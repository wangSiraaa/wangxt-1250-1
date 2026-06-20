using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Application.Services;

public class RectificationService : IRectificationService
{
    private readonly AppDbContext _context;
    private readonly ITowerCraneService _towerCraneService;

    public RectificationService(AppDbContext context, ITowerCraneService towerCraneService)
    {
        _context = context;
        _towerCraneService = towerCraneService;
    }

    public async Task<IEnumerable<Rectification>> GetAllAsync()
    {
        return await _context.Rectifications
            .Include(r => r.TowerCrane)
            .Include(r => r.Alarm)
            .Include(r => r.CreatedBy)
            .Include(r => r.ReviewedBy)
            .Include(r => r.AssignedTo)
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Rectification?> GetByIdAsync(int id)
    {
        return await _context.Rectifications
            .Include(r => r.TowerCrane)
            .Include(r => r.Alarm)
            .Include(r => r.CreatedBy)
            .Include(r => r.ReviewedBy)
            .Include(r => r.AssignedTo)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Rectification>> GetByTowerCraneIdAsync(int towerCraneId)
    {
        return await _context.Rectifications
            .Include(r => r.Alarm)
            .Include(r => r.CreatedBy)
            .Include(r => r.ReviewedBy)
            .Include(r => r.AssignedTo)
            .Where(r => r.TowerCraneId == towerCraneId)
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Rectification>> GetByStatusAsync(RectificationStatus status)
    {
        return await _context.Rectifications
            .Include(r => r.TowerCrane)
            .Include(r => r.Alarm)
            .Include(r => r.CreatedBy)
            .Include(r => r.AssignedTo)
            .Where(r => r.Status == status)
            .AsNoTracking()
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Deadline)
            .ToListAsync();
    }

    public async Task<IEnumerable<Rectification>> GetOpenRectificationsAsync()
    {
        return await _context.Rectifications
            .Include(r => r.TowerCrane)
            .Include(r => r.Alarm)
            .Include(r => r.CreatedBy)
            .Include(r => r.AssignedTo)
            .Where(r => r.Status != RectificationStatus.Closed)
            .AsNoTracking()
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Deadline)
            .ToListAsync();
    }

    public async Task<Rectification> CreateAsync(Rectification rectification)
    {
        rectification.RectificationNo = GenerateRectificationNo();
        rectification.Status = RectificationStatus.Open;
        rectification.CreatedAt = DateTime.Now;
        if (rectification.DueDate == default)
            rectification.DueDate = rectification.Deadline == default ? DateTime.Now.AddDays(2) : rectification.Deadline;
        if (rectification.Deadline == default)
            rectification.Deadline = rectification.DueDate;
        rectification.Remarks = string.IsNullOrWhiteSpace(rectification.Remarks)
            ? "整改期间该塔吊仅允许执行低风险任务"
            : rectification.Remarks;

        _context.Rectifications.Add(rectification);
        await _context.SaveChangesAsync();

        return rectification;
    }

    public async Task<Rectification> UpdateAsync(Rectification rectification)
    {
        var existing = await _context.Rectifications.FindAsync(rectification.Id);
        if (existing == null) throw new KeyNotFoundException($"整改单不存在");

        if (existing.Status == RectificationStatus.Closed)
            throw new InvalidOperationException("已关闭的整改单不能修改");

        _context.Entry(existing).CurrentValues.SetValues(rectification);
        existing.Status = existing.Status;
        existing.CreatedAt = existing.CreatedAt;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) return false;
        if (rectification.Status != RectificationStatus.Open && rectification.Status != RectificationStatus.Rejected)
            throw new InvalidOperationException("只能删除草稿或已驳回状态的整改单");

        _context.Rectifications.Remove(rectification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Rectification> StartRectificationAsync(int id)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) throw new KeyNotFoundException($"整改单不存在");
        if (rectification.Status != RectificationStatus.Open)
            throw new InvalidOperationException("只有待整改状态的整改单可以开始执行");

        rectification.Status = RectificationStatus.InProgress;
        rectification.StartTime = DateTime.Now;
        await _context.SaveChangesAsync();
        return rectification;
    }

    public async Task<Rectification> SubmitForReviewAsync(int id, string actions)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) throw new KeyNotFoundException($"整改单不存在");
        if (rectification.Status != RectificationStatus.InProgress)
            throw new InvalidOperationException("只有进行中的整改单可以提交审核");

        rectification.Status = RectificationStatus.PendingReview;
        rectification.RectificationActions = actions;
        await _context.SaveChangesAsync();
        return rectification;
    }

    public async Task<Rectification> ReviewAsync(int id, int reviewerId, bool approved, string reviewResult)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) throw new KeyNotFoundException($"整改单不存在");
        if (rectification.Status != RectificationStatus.PendingReview)
            throw new InvalidOperationException("只有待审核状态的整改单可以审核");

        rectification.ReviewedById = reviewerId;
        rectification.ReviewResult = reviewResult;
        rectification.ReviewComments = reviewResult;
        rectification.ReviewTime = DateTime.Now;
        rectification.UpdatedAt = DateTime.Now;

        if (approved)
        {
            rectification.Status = RectificationStatus.Closed;
            rectification.ClosedTime = DateTime.Now;
            await _context.SaveChangesAsync();

            var craneId = rectification.TowerCraneId;
            var hasOtherOpenRectifications = await _context.Rectifications.AnyAsync(r =>
                r.TowerCraneId == craneId && r.Status != RectificationStatus.Closed && r.Id != id);
            if (!hasOtherOpenRectifications)
            {
                var hasPendingCriticalAlarms = await _towerCraneService.HasPendingCriticalAlarmsAsync(craneId);
                var hasActiveTasks = await _context.LiftingTasks.AnyAsync(t =>
                    t.TowerCraneId == craneId && t.Status == TaskStatus.InProgress);

                if (hasPendingCriticalAlarms)
                {
                    await _towerCraneService.UpdateStatusAsync(craneId, TowerCraneStatus.Warning);
                }
                else if (hasActiveTasks)
                {
                    await _towerCraneService.UpdateStatusAsync(craneId, TowerCraneStatus.Working);
                }
                else
                {
                    await _towerCraneService.UpdateStatusAsync(craneId, TowerCraneStatus.Idle);
                }
            }

            var suspendedTasks = await _context.LiftingTasks
                .Where(t => t.TowerCraneId == craneId && t.Status == TaskStatus.Suspended)
                .ToListAsync();
            foreach (var task in suspendedTasks)
            {
                var blockingAlarms = await _context.Alarms.AnyAsync(a =>
                    a.LiftingTaskId == task.Id && a.Status != AlarmStatus.Resolved && a.Status != AlarmStatus.Ignored &&
                    (a.AlarmType == AlarmType.Overload || a.AlarmType == AlarmType.RangeLimit || a.AlarmType == AlarmType.HeightLimit || a.AlarmLevel == AlarmLevel.Critical));
                if (!blockingAlarms)
                {
                    task.Status = TaskStatus.InProgress;
                    task.UpdatedAt = DateTime.Now;
                }
            }
            if (suspendedTasks.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            rectification.Status = RectificationStatus.Rejected;
            await _context.SaveChangesAsync();
        }

        return rectification;
    }

    public async Task<Rectification> CloseAsync(int id)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) throw new KeyNotFoundException($"整改单不存在");
        if (rectification.Status != RectificationStatus.PendingReview)
            throw new InvalidOperationException("只有待审核状态的整改单可以直接关闭（管理员操作）");

        rectification.Status = RectificationStatus.Closed;
        rectification.ClosedTime = DateTime.Now;
        await _context.SaveChangesAsync();

        var craneId = rectification.TowerCraneId;
        var hasOtherOpenRectifications = await _context.Rectifications.AnyAsync(r =>
            r.TowerCraneId == craneId && r.Status != RectificationStatus.Closed && r.Id != id);
        if (!hasOtherOpenRectifications)
        {
            var hasPendingCriticalAlarms = await _towerCraneService.HasPendingCriticalAlarmsAsync(craneId);
            var hasActiveTasks = await _context.LiftingTasks.AnyAsync(t =>
                t.TowerCraneId == craneId && t.Status == TaskStatus.InProgress);

            if (hasPendingCriticalAlarms)
                await _towerCraneService.UpdateStatusAsync(craneId, TowerCraneStatus.Warning);
            else if (hasActiveTasks)
                await _towerCraneService.UpdateStatusAsync(craneId, TowerCraneStatus.Working);
            else
                await _towerCraneService.UpdateStatusAsync(craneId, TowerCraneStatus.Idle);
        }

        return rectification;
    }

    public async Task<Rectification> RejectAsync(int id, int reviewerId, string reviewResult)
    {
        return await ReviewAsync(id, reviewerId, false, reviewResult);
    }

    public async Task<Rectification> AssignAsync(int id, int assignedToId)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) throw new KeyNotFoundException($"整改单不存在");
        if (rectification.Status == RectificationStatus.Closed)
            throw new InvalidOperationException("整改单已关闭，无法分配");

        rectification.AssignedToId = assignedToId;
        if (rectification.Status == RectificationStatus.Created)
        {
            rectification.Status = RectificationStatus.InProgress;
            rectification.StartTime = DateTime.Now;
        }
        rectification.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return rectification;
    }

    public async Task<Rectification> ExecuteAsync(int id, string actionsTaken, string results)
    {
        var rectification = await _context.Rectifications.FindAsync(id);
        if (rectification == null) throw new KeyNotFoundException($"整改单不存在");
        if (rectification.Status != RectificationStatus.InProgress && rectification.Status != RectificationStatus.Created)
            throw new InvalidOperationException("只有待执行或执行中状态的整改单可以执行并提交审核");

        rectification.ActionsTaken = actionsTaken;
        rectification.Results = results;
        rectification.Status = RectificationStatus.PendingReview;
        rectification.SubmittedTime = DateTime.Now;
        rectification.ExecutedTime = DateTime.Now;
        rectification.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return rectification;
    }

    private string GenerateRectificationNo()
    {
        return $"RC{DateTime.Now:yyyyMMdd}{new Random().Next(1, 9999):D4}";
    }
}

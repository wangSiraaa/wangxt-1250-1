using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Application.Services;

public class LiftingTaskService : ILiftingTaskService
{
    private readonly AppDbContext _context;
    private readonly IPersonService _personService;
    private readonly ITowerCraneService _towerCraneService;
    private readonly IAlarmService _alarmService;

    public LiftingTaskService(AppDbContext context, IPersonService personService, ITowerCraneService towerCraneService, IAlarmService alarmService)
    {
        _context = context;
        _personService = personService;
        _towerCraneService = towerCraneService;
        _alarmService = alarmService;
    }

    public async Task<IEnumerable<LiftingTask>> GetAllAsync()
    {
        return await _context.LiftingTasks
            .Include(t => t.TowerCrane)
            .Include(t => t.SafetyOfficer)
            .Include(t => t.Driver)
            .Include(t => t.Alarms)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<LiftingTask?> GetByIdAsync(int id)
    {
        return await _context.LiftingTasks
            .Include(t => t.TowerCrane)
            .Include(t => t.SafetyOfficer)
            .Include(t => t.Driver)
            .Include(t => t.Alarms)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<LiftingTask>> GetByTowerCraneIdAsync(int towerCraneId)
    {
        return await _context.LiftingTasks
            .Include(t => t.SafetyOfficer)
            .Include(t => t.Driver)
            .Where(t => t.TowerCraneId == towerCraneId)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LiftingTask>> GetByDriverIdAsync(int driverId)
    {
        return await _context.LiftingTasks
            .Include(t => t.TowerCrane)
            .Include(t => t.SafetyOfficer)
            .Where(t => t.DriverId == driverId)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LiftingTask>> GetByStatusAsync(TaskStatus status)
    {
        return await _context.LiftingTasks
            .Include(t => t.TowerCrane)
            .Include(t => t.SafetyOfficer)
            .Include(t => t.Driver)
            .Where(t => t.Status == status)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<LiftingTask> CreateAsync(LiftingTask task)
    {
        var issues = await ValidateTaskCreationAsync(task);
        if (issues.Any())
            throw new InvalidOperationException($"创建任务校验失败: {string.Join("; ", issues)}");

        task.TaskNo = GenerateTaskNo();
        task.Status = TaskStatus.Draft;
        task.CreatedAt = DateTime.Now;

        if (await _towerCraneService.HasOpenRectificationAsync(task.TowerCraneId))
        {
            task.IsLowRiskOnly = true;
        }

        _context.LiftingTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<LiftingTask> UpdateAsync(LiftingTask task)
    {
        if (task.Status != TaskStatus.Draft && task.Status != TaskStatus.PendingDriverConfirm)
            throw new InvalidOperationException("只能修改草稿或待司机确认状态的任务");

        task.UpdatedAt = DateTime.Now;
        _context.Entry(task).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _context.LiftingTasks.FindAsync(id);
        if (task == null) return false;
        if (task.Status == TaskStatus.InProgress)
            throw new InvalidOperationException("进行中的任务不能删除");

        _context.LiftingTasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<LiftingTask> SubmitTaskAsync(int taskId)
    {
        var task = await _context.LiftingTasks.FindAsync(taskId);
        if (task == null) throw new KeyNotFoundException($"任务不存在");
        if (task.Status != TaskStatus.Draft) throw new InvalidOperationException("只有草稿状态的任务可以提交");

        task.Status = TaskStatus.PendingDriverConfirm;
        task.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<LiftingTask> DriverConfirmAsync(int taskId, int driverId)
    {
        var task = await _context.LiftingTasks.FindAsync(taskId);
        if (task == null) throw new KeyNotFoundException($"任务不存在");
        if (task.Status != TaskStatus.PendingDriverConfirm)
            throw new InvalidOperationException("任务状态不正确，无法确认上机");

        var qualIssues = await _personService.GetDriverQualificationIssuesAsync(driverId);
        var expiredIssue = qualIssues.FirstOrDefault(i => i.Contains("已过期"));
        if (expiredIssue != null)
            throw new InvalidOperationException($"司机证件不通过，无法确认上机: {expiredIssue}");

        task.DriverId = driverId;
        task.DriverConfirmTime = DateTime.Now;
        task.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<LiftingTask> StartTaskAsync(int taskId)
    {
        var issues = await ValidateTaskStartAsync(taskId);
        if (issues.Any())
            throw new InvalidOperationException($"启动任务校验失败: {string.Join("; ", issues)}");

        var task = await _context.LiftingTasks.FindAsync(taskId);
        if (task == null) throw new KeyNotFoundException($"任务不存在");

        task.Status = TaskStatus.InProgress;
        task.ActualStartTime = DateTime.Now;
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        await _towerCraneService.UpdateStatusAsync(task.TowerCraneId, TowerCraneStatus.Working);

        if (task.DriverId.HasValue)
        {
            var driver = await _context.Persons.FindAsync(task.DriverId.Value);
            if (driver != null)
            {
                driver.DriverStatus = DriverStatus.Working;
                await _context.SaveChangesAsync();
            }
        }

        return task;
    }

    public async Task<LiftingTask> CompleteTaskAsync(int taskId)
    {
        var task = await _context.LiftingTasks.FindAsync(taskId);
        if (task == null) throw new KeyNotFoundException($"任务不存在");
        if (task.Status != TaskStatus.InProgress)
            throw new InvalidOperationException("只有进行中的任务可以完成");

        var blockingAlarms = await _alarmService.GetBlockingAlarmsForTaskAsync(taskId);
        if (blockingAlarms.Any())
            throw new InvalidOperationException($"存在未处理的阻断性报警，无法完成任务: {string.Join(", ", blockingAlarms.Select(a => a.Description))}");

        task.Status = TaskStatus.Completed;
        task.ActualEndTime = DateTime.Now;
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var craneId = task.TowerCraneId;
        var hasOtherActiveTasks = await _context.LiftingTasks.AnyAsync(t =>
            t.TowerCraneId == craneId && t.Status == TaskStatus.InProgress && t.Id != taskId);
        if (!hasOtherActiveTasks)
        {
            var hasPendingAlarms = await _towerCraneService.HasPendingCriticalAlarmsAsync(craneId);
            var targetStatus = hasPendingAlarms ? TowerCraneStatus.Warning : TowerCraneStatus.Idle;
            await _towerCraneService.UpdateStatusAsync(craneId, targetStatus);
        }

        if (task.DriverId.HasValue)
        {
            var hasOtherActiveTasksForDriver = await _context.LiftingTasks.AnyAsync(t =>
                t.DriverId == task.DriverId.Value && t.Status == TaskStatus.InProgress && t.Id != taskId);
            if (!hasOtherActiveTasksForDriver)
            {
                var driver = await _context.Persons.FindAsync(task.DriverId.Value);
                if (driver != null)
                {
                    driver.DriverStatus = DriverStatus.Idle;
                    await _context.SaveChangesAsync();
                }
            }
        }

        return task;
    }

    public async Task<LiftingTask> CancelTaskAsync(int taskId, string reason)
    {
        var task = await _context.LiftingTasks.FindAsync(taskId);
        if (task == null) throw new KeyNotFoundException($"任务不存在");
        if (task.Status == TaskStatus.Completed)
            throw new InvalidOperationException("已完成的任务不能取消");

        var craneId = task.TowerCraneId;
        var driverId = task.DriverId;

        task.Status = TaskStatus.Cancelled;
        task.Remarks = reason;
        task.UpdatedAt = DateTime.Now;

        if (task.Status == TaskStatus.InProgress)
            task.ActualEndTime = DateTime.Now;

        await _context.SaveChangesAsync();

        var hasOtherActiveTasks = await _context.LiftingTasks.AnyAsync(t =>
            t.TowerCraneId == craneId && t.Status == TaskStatus.InProgress && t.Id != taskId);
        if (!hasOtherActiveTasks)
        {
            var hasPendingAlarms = await _towerCraneService.HasPendingCriticalAlarmsAsync(craneId);
            var targetStatus = hasPendingAlarms ? TowerCraneStatus.Warning : TowerCraneStatus.Idle;
            await _towerCraneService.UpdateStatusAsync(craneId, targetStatus);
        }

        if (driverId.HasValue)
        {
            var hasOtherActiveTasksForDriver = await _context.LiftingTasks.AnyAsync(t =>
                t.DriverId == driverId.Value && t.Status == TaskStatus.InProgress && t.Id != taskId);
            if (!hasOtherActiveTasksForDriver)
            {
                var driver = await _context.Persons.FindAsync(driverId.Value);
                if (driver != null)
                {
                    driver.DriverStatus = DriverStatus.Idle;
                    await _context.SaveChangesAsync();
                }
            }
        }

        return task;
    }

    public async Task<List<string>> ValidateTaskCreationAsync(LiftingTask task)
    {
        var issues = new List<string>();

        var crane = await _context.TowerCranes.FindAsync(task.TowerCraneId);
        if (crane == null || !crane.IsActive)
        {
            issues.Add("塔吊不存在或已停用");
            return issues;
        }

        if (crane.Status == TowerCraneStatus.Maintenance || crane.Status == TowerCraneStatus.Suspended)
        {
            issues.Add("塔吊当前处于维护或停用状态，无法创建任务");
        }

        if (task.PlannedLoad > crane.RatedLoadCapacity)
        {
            issues.Add($"计划载荷{task.PlannedLoad}吨超过塔吊额定载荷{crane.RatedLoadCapacity}吨");
        }

        if (task.Radius > crane.MaxRadius)
        {
            issues.Add($"工作半径{task.Radius}m超过塔吊最大半径{crane.MaxRadius}m");
        }

        if (task.LiftHeight > crane.MaxHeight)
        {
            issues.Add($"起升高度{task.LiftHeight}m超过塔吊最大高度{crane.MaxHeight}m");
        }

        if (crane.NextInspectionDate.HasValue && crane.NextInspectionDate.Value < DateTime.Now)
        {
            issues.Add("塔吊已超过下次检验日期，请先完成检验");
        }

        if (task.SafetyOfficerId.HasValue)
        {
            var safetyOfficer = await _context.Persons.FindAsync(task.SafetyOfficerId.Value);
            if (safetyOfficer == null || safetyOfficer.Role != UserRole.SafetyOfficer || !safetyOfficer.IsActive)
            {
                issues.Add("指定的安全员无效");
            }
        }

        var hasOpenRectification = await _towerCraneService.HasOpenRectificationAsync(task.TowerCraneId);
        if (hasOpenRectification && task.RiskLevel != TaskRiskLevel.Low)
        {
            issues.Add("该塔吊存在未关闭的整改记录，仅允许执行低风险任务");
        }

        var hasBlockingAlarms = await _alarmService.GetBlockingAlarmsForTowerCraneAsync(task.TowerCraneId);
        if (hasBlockingAlarms.Any())
        {
            issues.Add($"该塔吊存在未处理的阻断性报警，请先处理后再创建任务: {string.Join(", ", hasBlockingAlarms.Select(a => a.Description))}");
        }

        return issues;
    }

    public async Task<List<string>> ValidateTaskStartAsync(int taskId)
    {
        var issues = new List<string>();
        var task = await _context.LiftingTasks.FindAsync(taskId);

        if (task == null)
        {
            issues.Add("任务不存在");
            return issues;
        }

        if (task.Status != TaskStatus.PendingDriverConfirm && task.Status != TaskStatus.InProgress)
        {
            issues.Add("任务状态不正确，需要先由安全员提交并经司机确认");
        }

        if (!task.DriverId.HasValue)
        {
            issues.Add("未指定司机，请先安排司机并确认上机");
        }
        else
        {
            var qualIssues = await _personService.GetDriverQualificationIssuesAsync(task.DriverId.Value);
            foreach (var issue in qualIssues)
            {
                if (issue.Contains("已过期"))
                {
                    issues.Add(issue);
                }
            }
        }

        var blockingAlarms = await _alarmService.GetBlockingAlarmsForTowerCraneAsync(task.TowerCraneId);
        if (blockingAlarms.Any())
        {
            issues.Add($"存在未处理的阻断性报警，请先处理: {string.Join(", ", blockingAlarms.Select(a => a.Description))}");
        }

        var hasOpenRectification = await _towerCraneService.HasOpenRectificationAsync(task.TowerCraneId);
        if (hasOpenRectification && task.RiskLevel != TaskRiskLevel.Low)
        {
            issues.Add("该塔吊存在未关闭的整改记录，仅允许执行低风险任务");
        }

        return issues;
    }

    private string GenerateTaskNo()
    {
        return $"LT{DateTime.Now:yyyyMMdd}{new Random().Next(1, 9999):D4}";
    }
}

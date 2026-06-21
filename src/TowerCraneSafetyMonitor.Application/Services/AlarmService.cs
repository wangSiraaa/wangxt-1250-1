using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Application.Services;

public class AlarmService : IAlarmService
{
    private readonly AppDbContext _context;
    private readonly ITowerCraneService _towerCraneService;

    public AlarmService(AppDbContext context, ITowerCraneService towerCraneService)
    {
        _context = context;
        _towerCraneService = towerCraneService;
    }

    public async Task<IEnumerable<Alarm>> GetAllAsync()
    {
        return await _context.Alarms
            .Include(a => a.TowerCrane)
            .Include(a => a.LiftingTask)
            .Include(a => a.HandledBy)
            .AsNoTracking()
            .OrderByDescending(a => a.AlarmTime)
            .ToListAsync();
    }

    public async Task<Alarm?> GetByIdAsync(int id)
    {
        return await _context.Alarms
            .Include(a => a.TowerCrane)
            .Include(a => a.LiftingTask)
            .Include(a => a.HandledBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Alarm>> GetByTowerCraneIdAsync(int towerCraneId)
    {
        return await _context.Alarms
            .Include(a => a.LiftingTask)
            .Include(a => a.HandledBy)
            .Where(a => a.TowerCraneId == towerCraneId)
            .AsNoTracking()
            .OrderByDescending(a => a.AlarmTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Alarm>> GetByStatusAsync(AlarmStatus status)
    {
        return await _context.Alarms
            .Include(a => a.TowerCrane)
            .Include(a => a.LiftingTask)
            .Where(a => a.Status == status)
            .AsNoTracking()
            .OrderByDescending(a => a.AlarmTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Alarm>> GetPendingAlarmsAsync()
    {
        return await _context.Alarms
            .Include(a => a.TowerCrane)
            .Include(a => a.LiftingTask)
            .Where(a => a.Status == AlarmStatus.Pending || a.Status == AlarmStatus.Processing)
            .AsNoTracking()
            .OrderByDescending(a => a.AlarmLevel)
            .ThenByDescending(a => a.AlarmTime)
            .ToListAsync();
    }

    public async Task<Alarm> CreateAsync(Alarm alarm)
    {
        alarm.AlarmTime = DateTime.Now;
        alarm.Status = AlarmStatus.Pending;
        _context.Alarms.Add(alarm);
        await _context.SaveChangesAsync();

        if (alarm.AlarmLevel == AlarmLevel.Critical || 
            alarm.AlarmType == AlarmType.Overload || 
            alarm.AlarmType == AlarmType.RangeLimit ||
            alarm.AlarmType == AlarmType.HeightLimit)
        {
            await _towerCraneService.UpdateStatusAsync(alarm.TowerCraneId, TowerCraneStatus.Warning);
        }

        if (alarm.LiftingTaskId.HasValue && alarm.BlocksLiftingOperation)
        {
            var task = await _context.LiftingTasks.FindAsync(alarm.LiftingTaskId.Value);
            if (task != null && task.Status == TaskStatus.InProgress)
            {
                task.Status = TaskStatus.Suspended;
                task.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        return alarm;
    }

    public async Task<Alarm> StartProcessingAsync(int alarmId, int handledById)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null) throw new KeyNotFoundException($"报警不存在");
        if (alarm.Status != AlarmStatus.Pending)
            throw new InvalidOperationException("只有待处理状态的报警可以开始处理");

        alarm.Status = AlarmStatus.Processing;
        alarm.ProcessStartTime = DateTime.Now;
        alarm.HandledById = handledById;
        await _context.SaveChangesAsync();
        return alarm;
    }

    public async Task<Alarm> ResolveAsync(int alarmId, string action, string remarks, bool requiresRectification, DateTime? expectedRectificationTime)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null) throw new KeyNotFoundException($"报警不存在");
        if (alarm.Status == AlarmStatus.Resolved)
            throw new InvalidOperationException("该报警已处理完成");

        alarm.Status = AlarmStatus.Resolved;
        alarm.ResolvedTime = DateTime.Now;
        alarm.HandleAction = action;
        alarm.HandleRemarks = remarks;
        alarm.RequiresRectification = requiresRectification;
        alarm.ExpectedRectificationTime = expectedRectificationTime;
        if (!alarm.ProcessStartTime.HasValue)
            alarm.ProcessStartTime = DateTime.Now;

        await _context.SaveChangesAsync();

        if (alarm.LiftingTaskId.HasValue)
        {
            var task = await _context.LiftingTasks.FindAsync(alarm.LiftingTaskId.Value);
            if (task != null && task.Status == TaskStatus.Suspended)
            {
                var blockingAlarms = await GetBlockingAlarmsForTaskAsync(task.Id);
                if (!blockingAlarms.Any(a => a.Id != alarm.Id))
                {
                    task.Status = TaskStatus.InProgress;
                    task.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
        }

        var craneId = alarm.TowerCraneId;
        var hasOtherBlockingAlarms = await GetBlockingAlarmsForTowerCraneAsync(craneId);
        if (!hasOtherBlockingAlarms.Any(a => a.Id != alarm.Id))
        {
            var hasActiveTasks = await _context.LiftingTasks.AnyAsync(t =>
                t.TowerCraneId == craneId && t.Status == TaskStatus.InProgress);
            var targetStatus = hasActiveTasks ? TowerCraneStatus.Working : TowerCraneStatus.Idle;
            await _towerCraneService.UpdateStatusAsync(craneId, targetStatus);
        }

        if (requiresRectification)
        {
            var dueHours = alarm.AlarmLevel == AlarmLevel.Critical ? 24 :
                           alarm.AlarmLevel == AlarmLevel.Warning ? 48 : 72;
            var defaultDueDate = DateTime.Now.AddHours(dueHours);
            var finalDueDate = expectedRectificationTime ?? defaultDueDate;
            var rectification = new Rectification
            {
                RectificationNo = GenerateRectificationNo(),
                TowerCraneId = alarm.TowerCraneId,
                AlarmId = alarm.Id,
                SourceAlarmId = alarm.Id,
                CreatedById = alarm.HandledById ?? throw new InvalidOperationException("需要指定处理人以创建整改单"),
                Title = $"{GetAlarmTypeName(alarm.AlarmType)}整改",
                RectificationCategory = GetAlarmTypeName(alarm.AlarmType),
                ActionRequired = $"处理{GetAlarmTypeName(alarm.AlarmType)}问题，检查相关安全装置",
                Description = $"因报警触发整改：{alarm.Description}。报警编号：{alarm.Id}，发生时间：{alarm.AlarmTime:yyyy-MM-dd HH:mm:ss}",
                Priority = alarm.AlarmLevel == AlarmLevel.Critical ? RectificationPriority.Urgent :
                           alarm.AlarmLevel == AlarmLevel.Warning ? RectificationPriority.High :
                           RectificationPriority.Medium,
                Status = RectificationStatus.Open,
                Deadline = finalDueDate,
                DueDate = finalDueDate,
                Remarks = "整改期间该塔吊仅允许执行低风险任务"
            };
            _context.Rectifications.Add(rectification);
            await _context.SaveChangesAsync();
        }

        return alarm;
    }

    public async Task<Alarm> IgnoreAsync(int alarmId, int handledById, string reason)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null) throw new KeyNotFoundException($"报警不存在");
        if (alarm.Status == AlarmStatus.Resolved)
            throw new InvalidOperationException("该报警已处理完成");

        alarm.Status = AlarmStatus.Ignored;
        alarm.ResolvedTime = DateTime.Now;
        alarm.HandledById = handledById;
        alarm.HandleAction = "忽略报警";
        alarm.HandleRemarks = $"忽略原因：{reason}";
        if (!alarm.ProcessStartTime.HasValue)
            alarm.ProcessStartTime = DateTime.Now;
        alarm.RequiresRectification = false;

        await _context.SaveChangesAsync();

        if (alarm.LiftingTaskId.HasValue)
        {
            var task = await _context.LiftingTasks.FindAsync(alarm.LiftingTaskId.Value);
            if (task != null && task.Status == TaskStatus.Suspended)
            {
                var blockingAlarms = await GetBlockingAlarmsForTaskAsync(task.Id);
                if (!blockingAlarms.Any(a => a.Id != alarm.Id))
                {
                    task.Status = TaskStatus.InProgress;
                    task.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
        }

        var craneId = alarm.TowerCraneId;
        var hasOtherBlockingAlarms = await GetBlockingAlarmsForTowerCraneAsync(craneId);
        if (!hasOtherBlockingAlarms.Any(a => a.Id != alarm.Id))
        {
            var hasActiveTasks = await _context.LiftingTasks.AnyAsync(t =>
                t.TowerCraneId == craneId && t.Status == TaskStatus.InProgress);
            var targetStatus = hasActiveTasks ? TowerCraneStatus.Working : TowerCraneStatus.Idle;
            await _towerCraneService.UpdateStatusAsync(craneId, targetStatus);
        }

        return alarm;
    }

    public async Task<List<Alarm>> GetBlockingAlarmsForTaskAsync(int liftingTaskId)
    {
        var task = await _context.LiftingTasks.FindAsync(liftingTaskId);
        if (task == null) return new List<Alarm>();

        return await GetBlockingAlarmsForTowerCraneAsync(task.TowerCraneId);
    }

    public async Task<List<Alarm>> GetBlockingAlarmsForTowerCraneAsync(int towerCraneId)
    {
        return await _context.Alarms
            .Where(a =>
                a.TowerCraneId == towerCraneId &&
                a.Status != AlarmStatus.Resolved &&
                a.Status != AlarmStatus.Ignored &&
                (a.AlarmType == AlarmType.Overload ||
                 a.AlarmType == AlarmType.RangeLimit ||
                 a.AlarmType == AlarmType.HeightLimit ||
                 a.AlarmLevel == AlarmLevel.Critical))
            .AsNoTracking()
            .ToListAsync();
    }

    private string GenerateRectificationNo()
    {
        return $"RC{DateTime.Now:yyyyMMdd}{new Random().Next(1, 9999):D4}";
    }

    private string GetAlarmTypeName(AlarmType type)
    {
        return type switch
        {
            AlarmType.Overload => "超载",
            AlarmType.HeightLimit => "高度限位",
            AlarmType.RangeLimit => "幅度限位",
            AlarmType.RotationLimit => "回转限位",
            AlarmType.TiltWarning => "倾翻预警",
            AlarmType.WindSpeedWarning => "风速预警",
            AlarmType.CollisionWarning => "碰撞预警",
            _ => "报警"
        };
    }
}

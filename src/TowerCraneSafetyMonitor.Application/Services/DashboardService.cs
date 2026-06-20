using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var totalCranes = await _context.TowerCranes.CountAsync(c => c.IsActive);
        var workingCranes = await _context.TowerCranes.CountAsync(c => c.IsActive && c.Status == TowerCraneStatus.Working);
        var warningCranes = await _context.TowerCranes.CountAsync(c => c.IsActive && c.Status == TowerCraneStatus.Warning);
        var maintenanceCranes = await _context.TowerCranes.CountAsync(c => c.IsActive && c.Status == TowerCraneStatus.Maintenance);

        var allDrivers = await _context.Persons
            .Include(p => p.Certificates)
            .Where(p => p.Role == UserRole.Driver && p.IsActive)
            .ToListAsync();

        var totalDrivers = allDrivers.Count;
        var qualifiedDrivers = allDrivers.Count(d =>
            d.DriverStatus != DriverStatus.Suspended &&
            d.Certificates.Any(c =>
                c.CertificateType == CertificateType.TowerCraneOperator &&
                c.IsActive && !c.IsExpired));
        var driversWithExpiringCertificates = allDrivers.Count(d =>
            d.Certificates.Any(c =>
                c.CertificateType == CertificateType.TowerCraneOperator &&
                c.IsActive && !c.IsExpired && c.DaysUntilExpiry <= 30));

        var totalTasksToday = await _context.LiftingTasks.CountAsync(t =>
            t.PlannedStartTime >= today && t.PlannedStartTime < tomorrow ||
            t.ActualStartTime.HasValue && t.ActualStartTime.Value >= today);
        var inProgressTasks = await _context.LiftingTasks.CountAsync(t => t.Status == TaskStatus.InProgress);
        var completedTasksToday = await _context.LiftingTasks.CountAsync(t =>
            t.Status == TaskStatus.Completed &&
            t.ActualEndTime.HasValue && t.ActualEndTime.Value >= today && t.ActualEndTime.Value < tomorrow);

        var pendingAlarms = await _context.Alarms.CountAsync(a =>
            a.Status == AlarmStatus.Pending || a.Status == AlarmStatus.Processing);
        var criticalAlarms = await _context.Alarms.CountAsync(a =>
            a.AlarmLevel == AlarmLevel.Critical && a.Status != AlarmStatus.Resolved && a.Status != AlarmStatus.Ignored);

        var openRectifications = await _context.Rectifications.CountAsync(r => r.Status != RectificationStatus.Closed);
        var urgentRectifications = await _context.Rectifications.CountAsync(r =>
            r.Priority == RectificationPriority.Urgent && r.Status != RectificationStatus.Closed);

        return new DashboardStats(
            totalCranes,
            workingCranes,
            warningCranes,
            maintenanceCranes,
            totalDrivers,
            qualifiedDrivers,
            driversWithExpiringCertificates,
            totalTasksToday,
            inProgressTasks,
            completedTasksToday,
            pendingAlarms,
            criticalAlarms,
            openRectifications,
            urgentRectifications
        );
    }

    public async Task<IEnumerable<AlarmTrendData>> GetAlarmTrendAsync(int days = 7)
    {
        var startDate = DateTime.Today.AddDays(-days + 1);
        var result = new List<AlarmTrendData>();

        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var nextDate = date.AddDays(1);

            var alarms = await _context.Alarms
                .Where(a => a.AlarmTime >= date && a.AlarmTime < nextDate)
                .ToListAsync();

            result.Add(new AlarmTrendData(
                date,
                alarms.Count,
                alarms.Count(a => a.AlarmLevel == AlarmLevel.Critical),
                alarms.Count(a => a.AlarmLevel == AlarmLevel.Warning),
                alarms.Count(a => a.Status == AlarmStatus.Resolved)
            ));
        }

        return result;
    }

    public async Task<IEnumerable<TaskStatusDistribution>> GetTaskStatusDistributionAsync()
    {
        var statuses = Enum.GetValues<TaskStatus>();
        var result = new List<TaskStatusDistribution>();

        foreach (var status in statuses)
        {
            var count = await _context.LiftingTasks.CountAsync(t => t.Status == status);
            result.Add(new TaskStatusDistribution(status, count));
        }

        return result;
    }

    public async Task<IEnumerable<CraneWorkloadData>> GetCraneWorkloadAsync()
    {
        var cranes = await _context.TowerCranes
            .Where(c => c.IsActive)
            .Include(c => c.Tasks)
            .Include(c => c.Alarms)
            .ToListAsync();

        var startDate = DateTime.Today.AddDays(-7);

        return cranes.Select(c => new CraneWorkloadData(
            c.Id,
            c.CraneNo,
            c.Tasks.Count(t => t.Status == TaskStatus.Completed && t.ActualEndTime >= startDate),
            c.Tasks.Count(t => t.Status == TaskStatus.InProgress),
            c.Alarms.Count(a => a.AlarmTime >= startDate)
        )).ToList();
    }

    public async Task<IEnumerable<AlarmTypeDistribution>> GetAlarmTypeDistributionAsync()
    {
        var types = Enum.GetValues<AlarmType>();
        var result = new List<AlarmTypeDistribution>();

        foreach (var type in types)
        {
            var alarms = await _context.Alarms
                .Where(a => a.AlarmType == type)
                .OrderByDescending(a => a.AlarmLevel)
                .ToListAsync();

            if (alarms.Any())
            {
                result.Add(new AlarmTypeDistribution(
                    type,
                    alarms.Count,
                    alarms.MaxBy(a => a.AlarmLevel)!.AlarmLevel
                ));
            }
        }

        return result.OrderByDescending(d => d.Count);
    }
}

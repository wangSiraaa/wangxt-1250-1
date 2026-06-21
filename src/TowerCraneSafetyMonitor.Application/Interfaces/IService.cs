using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;

namespace TowerCraneSafetyMonitor.Application.Interfaces;

public interface IPersonService
{
    Task<IEnumerable<Person>> GetAllAsync();
    Task<Person?> GetByIdAsync(int id);
    Task<Person> CreateAsync(Person person);
    Task<Person> UpdateAsync(Person person);
    Task<bool> DeleteAsync(int id);
    Task<bool> CheckDriverQualificationAsync(int driverId);
    Task<List<string>> GetDriverQualificationIssuesAsync(int driverId);
    Task<IEnumerable<Person>> GetDriversAsync();
    Task<IEnumerable<Person>> GetSafetyOfficersAsync();
    Task<IEnumerable<Person>> GetSupervisorsAsync();
}

public interface ITowerCraneService
{
    Task<IEnumerable<TowerCrane>> GetAllAsync();
    Task<TowerCrane?> GetByIdAsync(int id);
    Task<TowerCrane> CreateAsync(TowerCrane towerCrane);
    Task<TowerCrane> UpdateAsync(TowerCrane towerCrane);
    Task<bool> DeleteAsync(int id);
    Task<bool> CanExecuteRiskLevelTaskAsync(int towerCraneId, TaskRiskLevel riskLevel);
    Task<bool> HasPendingCriticalAlarmsAsync(int towerCraneId);
    Task<bool> HasOpenRectificationAsync(int towerCraneId);
    Task UpdateStatusAsync(int id, TowerCraneStatus status);
}

public interface ILiftingTaskService
{
    Task<IEnumerable<LiftingTask>> GetAllAsync();
    Task<LiftingTask?> GetByIdAsync(int id);
    Task<IEnumerable<LiftingTask>> GetByTowerCraneIdAsync(int towerCraneId);
    Task<IEnumerable<LiftingTask>> GetByDriverIdAsync(int driverId);
    Task<IEnumerable<LiftingTask>> GetByStatusAsync(TaskStatus status);
    Task<LiftingTask> CreateAsync(LiftingTask task);
    Task<LiftingTask> UpdateAsync(LiftingTask task);
    Task<bool> DeleteAsync(int id);
    Task<LiftingTask> SubmitTaskAsync(int taskId);
    Task<LiftingTask> DriverConfirmAsync(int taskId, int driverId);
    Task<LiftingTask> StartTaskAsync(int taskId);
    Task<LiftingTask> CompleteTaskAsync(int taskId);
    Task<LiftingTask> CancelTaskAsync(int taskId, string reason);
    Task<List<string>> ValidateTaskCreationAsync(LiftingTask task);
    Task<List<string>> ValidateTaskStartAsync(int taskId);
}

public interface IAlarmService
{
    Task<IEnumerable<Alarm>> GetAllAsync();
    Task<Alarm?> GetByIdAsync(int id);
    Task<IEnumerable<Alarm>> GetByTowerCraneIdAsync(int towerCraneId);
    Task<IEnumerable<Alarm>> GetByStatusAsync(AlarmStatus status);
    Task<IEnumerable<Alarm>> GetPendingAlarmsAsync();
    Task<Alarm> CreateAsync(Alarm alarm);
    Task<Alarm> StartProcessingAsync(int alarmId, int handledById);
    Task<Alarm> ResolveAsync(int alarmId, string action, string remarks, bool requiresRectification, DateTime? expectedRectificationTime);
    Task<Alarm> IgnoreAsync(int alarmId, int handledById, string reason);
    Task<List<Alarm>> GetBlockingAlarmsForTaskAsync(int liftingTaskId);
    Task<List<Alarm>> GetBlockingAlarmsForTowerCraneAsync(int towerCraneId);
}

public interface IRectificationService
{
    Task<IEnumerable<Rectification>> GetAllAsync();
    Task<Rectification?> GetByIdAsync(int id);
    Task<IEnumerable<Rectification>> GetByTowerCraneIdAsync(int towerCraneId);
    Task<IEnumerable<Rectification>> GetByStatusAsync(RectificationStatus status);
    Task<IEnumerable<Rectification>> GetOpenRectificationsAsync();
    Task<Rectification> CreateAsync(Rectification rectification);
    Task<Rectification> UpdateAsync(Rectification rectification);
    Task<bool> DeleteAsync(int id);
    Task<Rectification> StartRectificationAsync(int id);
    Task<Rectification> SubmitForReviewAsync(int id, string actions);
    Task<Rectification> ReviewAsync(int id, int reviewerId, bool approved, string reviewResult);
    Task<Rectification> CloseAsync(int id);
    Task<Rectification> RejectAsync(int id, int reviewerId, string reviewResult);
    Task<Rectification> AssignAsync(int id, int assignedToId);
    Task<Rectification> ExecuteAsync(int id, string actionsTaken, string results);
}

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<IEnumerable<AlarmTrendData>> GetAlarmTrendAsync(int days = 7);
    Task<IEnumerable<TaskStatusDistribution>> GetTaskStatusDistributionAsync();
    Task<IEnumerable<CraneWorkloadData>> GetCraneWorkloadAsync();
    Task<IEnumerable<AlarmTypeDistribution>> GetAlarmTypeDistributionAsync();
}

public record DashboardStats(
    int TotalCranes,
    int WorkingCranes,
    int WarningCranes,
    int MaintenanceCranes,
    int TotalDrivers,
    int QualifiedDrivers,
    int DriversWithExpiringCertificates,
    int TotalTasksToday,
    int InProgressTasks,
    int CompletedTasksToday,
    int PendingAlarms,
    int CriticalAlarms,
    int OpenRectifications,
    int UrgentRectifications
);

public record AlarmTrendData(
    DateTime Date,
    int TotalAlarms,
    int CriticalAlarms,
    int WarningAlarms,
    int ResolvedAlarms
);

public record TaskStatusDistribution(
    TaskStatus Status,
    int Count
);

public record CraneWorkloadData(
    int CraneId,
    string CraneNo,
    int CompletedTasks,
    int InProgressTasks,
    int TotalAlarms
);

public record AlarmTypeDistribution(
    AlarmType Type,
    int Count,
    AlarmLevel HighestLevel
);

using TowerCraneSafetyMonitor.Domain.Enums;

namespace TowerCraneSafetyMonitor.Domain.Entities;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string IdCard { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public DateTime? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<LiftingTask> TasksAsSafetyOfficer { get; set; } = new List<LiftingTask>();
    public ICollection<LiftingTask> TasksAsDriver { get; set; } = new List<LiftingTask>();
    public ICollection<Alarm> AlarmsHandled { get; set; } = new List<Alarm>();
    public ICollection<Rectification> RectificationsCreated { get; set; } = new List<Rectification>();
    public ICollection<Rectification> RectificationsReviewed { get; set; } = new List<Rectification>();
}

public class Certificate
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public CertificateType CertificateType { get; set; }
    public string CertificateNo { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExpired => ExpiryDate <= DateTime.Now;
    public int DaysUntilExpiry => (ExpiryDate - DateTime.Now).Days;
    public string? FilePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Person? Person { get; set; }
}

public class TowerCrane
{
    public int Id { get; set; }
    public string CraneNo { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public TowerCraneStatus Status { get; set; } = TowerCraneStatus.Idle;
    public decimal RatedLoadCapacity { get; set; }
    public decimal MaxRadius { get; set; }
    public decimal MaxHeight { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDate { get; set; }
    public string? BlackBoxDeviceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<LiftingTask> Tasks { get; set; } = new List<LiftingTask>();
    public ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
    public ICollection<Rectification> Rectifications { get; set; } = new List<Rectification>();
}

public class LiftingTask
{
    public int Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public int TowerCraneId { get; set; }
    public int? SafetyOfficerId { get; set; }
    public int? DriverId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal PlannedLoad { get; set; }
    public string LoadType { get; set; } = string.Empty;
    public decimal Radius { get; set; }
    public decimal LiftHeight { get; set; }
    public TaskRiskLevel RiskLevel { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Draft;
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public DateTime? DriverConfirmTime { get; set; }
    public int? AssignedTaskId { get; set; }
    public bool IsLowRiskOnly { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public TowerCrane? TowerCrane { get; set; }
    public Person? SafetyOfficer { get; set; }
    public Person? Driver { get; set; }
    public ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
}

public class Alarm
{
    public int Id { get; set; }
    public int TowerCraneId { get; set; }
    public int? LiftingTaskId { get; set; }
    public AlarmType AlarmType { get; set; }
    public AlarmLevel AlarmLevel { get; set; }
    public AlarmStatus Status { get; set; } = AlarmStatus.Pending;
    public string Description { get; set; } = string.Empty;
    public decimal? LoadValue { get; set; }
    public decimal? LoadPercentage { get; set; }
    public decimal? HeightValue { get; set; }
    public decimal? RadiusValue { get; set; }
    public decimal? RotationValue { get; set; }
    public decimal? WindSpeed { get; set; }
    public DateTime AlarmTime { get; set; } = DateTime.Now;
    public DateTime? ProcessStartTime { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public int? HandledById { get; set; }
    public string? HandleAction { get; set; }
    public string? HandleRemarks { get; set; }
    public bool RequiresRectification { get; set; }
    public DateTime? ExpectedRectificationTime { get; set; }
    public bool BlocksLiftingOperation => 
        (AlarmType == AlarmType.Overload || AlarmType == AlarmType.RangeLimit || AlarmType == AlarmType.HeightLimit)
        && Status != AlarmStatus.Resolved;

    public TowerCrane? TowerCrane { get; set; }
    public LiftingTask? LiftingTask { get; set; }
    public Person? HandledBy { get; set; }
}

public class Rectification
{
    public int Id { get; set; }
    public string RectificationNo { get; set; } = string.Empty;
    public int TowerCraneId { get; set; }
    public int? AlarmId { get; set; }
    public int? SourceAlarmId { get; set; }
    public int CreatedById { get; set; }
    public int? ReviewedById { get; set; }
    public int? AssignedToId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RectificationCategory { get; set; }
    public string? ActionRequired { get; set; }
    public string? ActionsTaken { get; set; }
    public string? Results { get; set; }
    public string? ReviewComments { get; set; }
    public RectificationPriority Priority { get; set; }
    public RectificationStatus Status { get; set; } = RectificationStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime Deadline { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? ExecutedTime { get; set; }
    public DateTime? SubmittedTime { get; set; }
    public DateTime? ReviewTime { get; set; }
    public DateTime? ClosedTime { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RectificationActions { get; set; }
    public string? ReviewResult { get; set; }
    public string? Remarks { get; set; }
    public bool RestrictsHighRiskTasks => Status != RectificationStatus.Closed;
    public bool IsOverdue => Status != RectificationStatus.Closed && DueDate < DateTime.Now;

    public TowerCrane? TowerCrane { get; set; }
    public Alarm? Alarm { get; set; }
    public Person? CreatedBy { get; set; }
    public Person? ReviewedBy { get; set; }
    public Person? AssignedTo { get; set; }
}

namespace TowerCraneSafetyMonitor.Domain.Enums;

public enum UserRole
{
    SafetyOfficer = 1,
    Driver = 2,
    Supervisor = 3,
    Admin = 4
}

public enum DriverStatus
{
    Idle = 1,
    Working = 2,
    Suspended = 3
}

public enum TowerCraneStatus
{
    Idle = 1,
    Working = 2,
    Warning = 3,
    Maintenance = 4,
    Suspended = 5
}

public enum TaskRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum TaskStatus
{
    Draft = 1,
    PendingDriverConfirm = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Suspended = 6
}

public enum AlarmType
{
    Overload = 1,
    HeightLimit = 2,
    RangeLimit = 3,
    RotationLimit = 4,
    TiltWarning = 5,
    WindSpeedWarning = 6,
    CollisionWarning = 7
}

public enum AlarmLevel
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public enum AlarmStatus
{
    Pending = 1,
    Processing = 2,
    Resolved = 3,
    Ignored = 4
}

public enum RectificationStatus
{
    Open = 1,
    InProgress = 2,
    PendingReview = 3,
    Closed = 4,
    Rejected = 5
}

public enum RectificationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum CertificateType
{
    TowerCraneOperator = 1,
    SafetyOfficer = 2,
    Supervisor = 3,
    SpecialEquipment = 4
}

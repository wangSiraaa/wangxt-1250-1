using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Domain.Entities;

namespace TowerCraneSafetyMonitor.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<TowerCrane> TowerCranes { get; set; }
    public DbSet<LiftingTask> LiftingTasks { get; set; }
    public DbSet<Alarm> Alarms { get; set; }
    public DbSet<Rectification> Rectifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.EmployeeNo).IsRequired().HasMaxLength(50);
            b.Property(p => p.IdCard).IsRequired().HasMaxLength(18);
            b.Property(p => p.Phone).HasMaxLength(20);
            b.HasIndex(p => p.EmployeeNo).IsUnique();
        });

        modelBuilder.Entity<Certificate>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasOne(c => c.Person)
             .WithMany(p => p.Certificates)
             .HasForeignKey(c => c.PersonId)
             .OnDelete(DeleteBehavior.Cascade);
            b.Property(c => c.CertificateNo).IsRequired().HasMaxLength(100);
            b.Property(c => c.IssuingAuthority).HasMaxLength(200);
            b.Property(c => c.FilePath).HasMaxLength(500);
        });

        modelBuilder.Entity<TowerCrane>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.CraneNo).IsRequired().HasMaxLength(50);
            b.Property(t => t.Model).IsRequired().HasMaxLength(100);
            b.Property(t => t.SerialNo).IsRequired().HasMaxLength(100);
            b.Property(t => t.Location).IsRequired().HasMaxLength(200);
            b.Property(t => t.RatedLoadCapacity).HasPrecision(18, 2);
            b.Property(t => t.MaxRadius).HasPrecision(18, 2);
            b.Property(t => t.MaxHeight).HasPrecision(18, 2);
            b.Property(t => t.BlackBoxDeviceId).HasMaxLength(100);
            b.HasIndex(t => t.CraneNo).IsUnique();
        });

        modelBuilder.Entity<LiftingTask>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.TaskNo).IsRequired().HasMaxLength(50);
            b.HasIndex(t => t.TaskNo).IsUnique();
            b.HasOne(t => t.TowerCrane)
             .WithMany(tc => tc.Tasks)
             .HasForeignKey(t => t.TowerCraneId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(t => t.SafetyOfficer)
             .WithMany(p => p.TasksAsSafetyOfficer)
             .HasForeignKey(t => t.SafetyOfficerId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(t => t.Driver)
             .WithMany(p => p.TasksAsDriver)
             .HasForeignKey(t => t.DriverId)
             .OnDelete(DeleteBehavior.Restrict);
            b.Property(t => t.Description).IsRequired().HasMaxLength(500);
            b.Property(t => t.Location).IsRequired().HasMaxLength(200);
            b.Property(t => t.PlannedLoad).HasPrecision(18, 2);
            b.Property(t => t.Radius).HasPrecision(18, 2);
            b.Property(t => t.LiftHeight).HasPrecision(18, 2);
            b.Property(t => t.LoadType).HasMaxLength(100);
            b.Property(t => t.Remarks).HasMaxLength(1000);
        });

        modelBuilder.Entity<Alarm>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasOne(a => a.TowerCrane)
             .WithMany(tc => tc.Alarms)
             .HasForeignKey(a => a.TowerCraneId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(a => a.LiftingTask)
             .WithMany(t => t.Alarms)
             .HasForeignKey(a => a.LiftingTaskId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.HandledBy)
             .WithMany(p => p.AlarmsHandled)
             .HasForeignKey(a => a.HandledById)
             .OnDelete(DeleteBehavior.Restrict);
            b.Property(a => a.Description).IsRequired().HasMaxLength(500);
            b.Property(a => a.LoadValue).HasPrecision(18, 2);
            b.Property(a => a.LoadPercentage).HasPrecision(18, 2);
            b.Property(a => a.HeightValue).HasPrecision(18, 2);
            b.Property(a => a.RadiusValue).HasPrecision(18, 2);
            b.Property(a => a.RotationValue).HasPrecision(18, 2);
            b.Property(a => a.WindSpeed).HasPrecision(18, 2);
            b.Property(a => a.HandleAction).HasMaxLength(500);
            b.Property(a => a.HandleRemarks).HasMaxLength(1000);
        });

        modelBuilder.Entity<Rectification>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.RectificationNo).IsRequired().HasMaxLength(50);
            b.HasIndex(r => r.RectificationNo).IsUnique();
            b.HasOne(r => r.TowerCrane)
             .WithMany(tc => tc.Rectifications)
             .HasForeignKey(r => r.TowerCraneId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(r => r.Alarm)
             .WithMany()
             .HasForeignKey(r => r.AlarmId)
             .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(r => r.CreatedBy)
             .WithMany(p => p.RectificationsCreated)
             .HasForeignKey(r => r.CreatedById)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(r => r.ReviewedBy)
             .WithMany(p => p.RectificationsReviewed)
             .HasForeignKey(r => r.ReviewedById)
             .OnDelete(DeleteBehavior.Restrict);
            b.Property(r => r.Title).IsRequired().HasMaxLength(200);
            b.Property(r => r.Description).IsRequired().HasMaxLength(1000);
            b.Property(r => r.RectificationActions).HasMaxLength(2000);
            b.Property(r => r.ReviewResult).HasMaxLength(1000);
            b.Property(r => r.Remarks).HasMaxLength(1000);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.Now;
        var future = now.AddYears(1);
        var past = now.AddYears(-1);
        var expired = now.AddMonths(-1);

        modelBuilder.Entity<Person>().HasData(
            new Person { Id = 1, Name = "张安全", EmployeeNo = "SA001", IdCard = "110101199001011234", Phone = "13800138001", Role = Domain.Enums.UserRole.SafetyOfficer, HireDate = past, IsActive = true, CreatedAt = now },
            new Person { Id = 2, Name = "李安全", EmployeeNo = "SA002", IdCard = "110101199002022345", Phone = "13800138002", Role = Domain.Enums.UserRole.SafetyOfficer, HireDate = past, IsActive = true, CreatedAt = now },
            new Person { Id = 3, Name = "王司机", EmployeeNo = "DR001", IdCard = "110101198503033456", Phone = "13800138003", Role = Domain.Enums.UserRole.Driver, DriverStatus = Domain.Enums.DriverStatus.Idle, HireDate = past.AddYears(-2), IsActive = true, CreatedAt = now },
            new Person { Id = 4, Name = "赵司机", EmployeeNo = "DR002", IdCard = "110101198604044567", Phone = "13800138004", Role = Domain.Enums.UserRole.Driver, DriverStatus = Domain.Enums.DriverStatus.Idle, HireDate = past.AddYears(-3), IsActive = true, CreatedAt = now },
            new Person { Id = 5, Name = "钱司机", EmployeeNo = "DR003", IdCard = "110101198705055678", Phone = "13800138005", Role = Domain.Enums.UserRole.Driver, DriverStatus = Domain.Enums.DriverStatus.Idle, HireDate = past.AddYears(-1), IsActive = true, CreatedAt = now },
            new Person { Id = 6, Name = "孙监理", EmployeeNo = "SV001", IdCard = "110101198006066789", Phone = "13800138006", Role = Domain.Enums.UserRole.Supervisor, HireDate = past.AddYears(-5), IsActive = true, CreatedAt = now },
            new Person { Id = 7, Name = "周监理", EmployeeNo = "SV002", IdCard = "110101198107077890", Phone = "13800138007", Role = Domain.Enums.UserRole.Supervisor, HireDate = past.AddYears(-4), IsActive = true, CreatedAt = now },
            new Person { Id = 8, Name = "吴管理", EmployeeNo = "AD001", IdCard = "110101197808088901", Phone = "13800138008", Role = Domain.Enums.UserRole.Admin, HireDate = past.AddYears(-6), IsActive = true, CreatedAt = now }
        );

        modelBuilder.Entity<Certificate>().HasData(
            new Certificate { Id = 1, PersonId = 3, CertificateType = Domain.Enums.CertificateType.TowerCraneOperator, CertificateNo = "TC202301001", IssuingAuthority = "市建设工程安全监督站", IssueDate = past.AddMonths(-6), ExpiryDate = future, IsActive = true, CreatedAt = now },
            new Certificate { Id = 2, PersonId = 3, CertificateType = Domain.Enums.CertificateType.SpecialEquipment, CertificateNo = "SE202205001", IssuingAuthority = "特种设备检验研究院", IssueDate = past.AddMonths(-12), ExpiryDate = future, IsActive = true, CreatedAt = now },
            new Certificate { Id = 3, PersonId = 4, CertificateType = Domain.Enums.CertificateType.TowerCraneOperator, CertificateNo = "TC202302001", IssuingAuthority = "市建设工程安全监督站", IssueDate = past.AddMonths(-3), ExpiryDate = future, IsActive = true, CreatedAt = now },
            new Certificate { Id = 4, PersonId = 5, CertificateType = Domain.Enums.CertificateType.TowerCraneOperator, CertificateNo = "TC202012001", IssuingAuthority = "市建设工程安全监督站", IssueDate = past.AddMonths(-36), ExpiryDate = expired, IsActive = true, CreatedAt = now },
            new Certificate { Id = 5, PersonId = 1, CertificateType = Domain.Enums.CertificateType.SafetyOfficer, CertificateNo = "SO202301001", IssuingAuthority = "市安全生产监督管理局", IssueDate = past.AddMonths(-8), ExpiryDate = future, IsActive = true, CreatedAt = now },
            new Certificate { Id = 6, PersonId = 2, CertificateType = Domain.Enums.CertificateType.SafetyOfficer, CertificateNo = "SO202302001", IssuingAuthority = "市安全生产监督管理局", IssueDate = past.AddMonths(-5), ExpiryDate = future, IsActive = true, CreatedAt = now },
            new Certificate { Id = 7, PersonId = 6, CertificateType = Domain.Enums.CertificateType.Supervisor, CertificateNo = "SV202201001", IssuingAuthority = "市建设监理协会", IssueDate = past.AddMonths(-10), ExpiryDate = future, IsActive = true, CreatedAt = now },
            new Certificate { Id = 8, PersonId = 7, CertificateType = Domain.Enums.CertificateType.Supervisor, CertificateNo = "SV202301001", IssuingAuthority = "市建设监理协会", IssueDate = past.AddMonths(-2), ExpiryDate = future, IsActive = true, CreatedAt = now }
        );

        modelBuilder.Entity<TowerCrane>().HasData(
            new TowerCrane { Id = 1, CraneNo = "TC-001", Model = "QTZ80", SerialNo = "TC2022010001", Location = "A区1号楼南侧", Status = Domain.Enums.TowerCraneStatus.Idle, RatedLoadCapacity = 8.0m, MaxRadius = 55.0m, MaxHeight = 120.0m, InstallDate = past.AddMonths(-18), LastInspectionDate = past.AddMonths(-2), NextInspectionDate = future.AddMonths(-2), BlackBoxDeviceId = "BB-TC001-2024", IsActive = true, CreatedAt = now },
            new TowerCrane { Id = 2, CraneNo = "TC-002", Model = "QTZ100", SerialNo = "TC2022060005", Location = "B区2号楼北侧", Status = Domain.Enums.TowerCraneStatus.Idle, RatedLoadCapacity = 10.0m, MaxRadius = 60.0m, MaxHeight = 150.0m, InstallDate = past.AddMonths(-12), LastInspectionDate = past.AddMonths(-1), NextInspectionDate = future.AddMonths(-3), BlackBoxDeviceId = "BB-TC002-2024", IsActive = true, CreatedAt = now },
            new TowerCrane { Id = 3, CraneNo = "TC-003", Model = "QTZ63", SerialNo = "TC2023030002", Location = "C区3号楼东侧", Status = Domain.Enums.TowerCraneStatus.Idle, RatedLoadCapacity = 6.3m, MaxRadius = 50.0m, MaxHeight = 100.0m, InstallDate = past.AddMonths(-8), LastInspectionDate = past, NextInspectionDate = future, BlackBoxDeviceId = "BB-TC003-2024", IsActive = true, CreatedAt = now },
            new TowerCrane { Id = 4, CraneNo = "TC-004", Model = "QTZ125", SerialNo = "TC2021090012", Location = "D区4号楼西侧", Status = Domain.Enums.TowerCraneStatus.Warning, RatedLoadCapacity = 12.5m, MaxRadius = 65.0m, MaxHeight = 180.0m, InstallDate = past.AddMonths(-24), LastInspectionDate = past.AddMonths(-4), NextInspectionDate = past.AddDays(15), BlackBoxDeviceId = "BB-TC004-2024", IsActive = true, CreatedAt = now }
        );

        modelBuilder.Entity<LiftingTask>().HasData(
            new LiftingTask { Id = 1, TaskNo = "LT20240115001", TowerCraneId = 1, SafetyOfficerId = 1, DriverId = 3, Description = "A1号楼三层钢筋吊装", Location = "A区1号楼", PlannedLoad = 3.5m, LoadType = "钢筋", Radius = 35.0m, LiftHeight = 15.0m, RiskLevel = Domain.Enums.TaskRiskLevel.Medium, Status = Domain.Enums.TaskStatus.Completed, PlannedStartTime = now.AddDays(-2), PlannedEndTime = now.AddDays(-2).AddHours(8), ActualStartTime = now.AddDays(-2).AddMinutes(30), ActualEndTime = now.AddDays(-2).AddHours(7), DriverConfirmTime = now.AddDays(-2).AddMinutes(15), IsLowRiskOnly = false, CreatedAt = now.AddDays(-3) },
            new LiftingTask { Id = 2, TaskNo = "LT20240116001", TowerCraneId = 2, SafetyOfficerId = 2, DriverId = 4, Description = "B2号楼五层模板吊装", Location = "B区2号楼", PlannedLoad = 4.0m, LoadType = "钢模板", Radius = 40.0m, LiftHeight = 25.0m, RiskLevel = Domain.Enums.TaskRiskLevel.Medium, Status = Domain.Enums.TaskStatus.Completed, PlannedStartTime = now.AddDays(-1), PlannedEndTime = now.AddDays(-1).AddHours(6), ActualStartTime = now.AddDays(-1).AddMinutes(20), ActualEndTime = now.AddDays(-1).AddHours(5), DriverConfirmTime = now.AddDays(-1).AddMinutes(10), IsLowRiskOnly = false, CreatedAt = now.AddDays(-2) },
            new LiftingTask { Id = 3, TaskNo = "LT20240117001", TowerCraneId = 1, SafetyOfficerId = 1, DriverId = 3, Description = "A1号楼四层混凝土构件吊装", Location = "A区1号楼", PlannedLoad = 2.8m, LoadType = "预制构件", Radius = 32.0m, LiftHeight = 18.0m, RiskLevel = Domain.Enums.TaskRiskLevel.Low, Status = Domain.Enums.TaskStatus.InProgress, PlannedStartTime = now, PlannedEndTime = now.AddHours(5), ActualStartTime = now.AddMinutes(10), DriverConfirmTime = now.AddMinutes(5), IsLowRiskOnly = false, CreatedAt = now.AddDays(-1) }
        );

        modelBuilder.Entity<Alarm>().HasData(
            new Alarm { Id = 1, TowerCraneId = 4, LiftingTaskId = 2, AlarmType = Domain.Enums.AlarmType.Overload, AlarmLevel = Domain.Enums.AlarmLevel.Critical, Status = Domain.Enums.AlarmStatus.Pending, Description = "超载报警：实际载荷5.2吨超过额定载荷5.0吨（104%）", LoadValue = 5.2m, LoadPercentage = 104.0m, AlarmTime = now.AddHours(-6), RequiresRectification = true },
            new Alarm { Id = 2, TowerCraneId = 4, AlarmType = Domain.Enums.AlarmType.WindSpeedWarning, AlarmLevel = Domain.Enums.AlarmLevel.Warning, Status = Domain.Enums.AlarmStatus.Resolved, Description = "风速超限报警：瞬时风速18m/s", WindSpeed = 18.0m, AlarmTime = now.AddDays(-1).AddHours(10), ProcessStartTime = now.AddDays(-1).AddHours(10).AddMinutes(5), ResolvedTime = now.AddDays(-1).AddHours(12), HandledById = 6, HandleAction = "暂停吊装作业，等待风速降低", HandleRemarks = "两小时后风速降至12m/s以下，恢复作业", RequiresRectification = false },
            new Alarm { Id = 3, TowerCraneId = 2, LiftingTaskId = 2, AlarmType = Domain.Enums.AlarmType.RangeLimit, AlarmLevel = Domain.Enums.AlarmLevel.Critical, Status = Domain.Enums.AlarmStatus.Processing, Description = "幅度限位报警：工作半径62m超过最大60m", RadiusValue = 62.0m, AlarmTime = now.AddHours(-2), ProcessStartTime = now.AddHours(-1), HandledById = 7, RequiresRectification = true },
            new Alarm { Id = 4, TowerCraneId = 1, AlarmType = Domain.Enums.AlarmType.HeightLimit, AlarmLevel = Domain.Enums.AlarmLevel.Warning, Status = Domain.Enums.AlarmStatus.Resolved, Description = "高度限位预警：接近最大高度115m", HeightValue = 115.0m, AlarmTime = now.AddDays(-2).AddHours(4), ProcessStartTime = now.AddDays(-2).AddHours(4).AddMinutes(3), ResolvedTime = now.AddDays(-2).AddHours(4).AddMinutes(15), HandledById = 6, HandleAction = "调整起升高度限制器参数", HandleRemarks = "已重新标定高度限位，复测正常", RequiresRectification = false }
        );

        modelBuilder.Entity<Rectification>().HasData(
            new Rectification { Id = 1, RectificationNo = "RC20240117001", TowerCraneId = 4, AlarmId = 1, SourceAlarmId = 1, CreatedById = 6, AssignedToId = 4, Priority = Domain.Enums.RectificationPriority.Urgent, Title = "TC-004超载问题整改", RectificationCategory = "载荷限制", ActionRequired = "检查力矩限制器参数，重新标定额定载荷", Description = "2024年1月17日08:30，TC-004塔吊在B2号楼吊装作业中发生超载报警，实际载荷5.2吨超过额定载荷5.0吨的104%。需立即停止高风险吊装，检查力矩限制器。", Status = Domain.Enums.RectificationStatus.InProgress, CreatedAt = now.AddHours(-5), Deadline = now.AddHours(48), DueDate = now.AddHours(48), StartTime = now.AddHours(-4), Remarks = "整改期间TC-004仅允许执行低风险任务" },
            new Rectification { Id = 2, RectificationNo = "RC20240117002", TowerCraneId = 2, AlarmId = 3, SourceAlarmId = 3, CreatedById = 7, Priority = Domain.Enums.RectificationPriority.High, Title = "TC-002幅度限位整改", RectificationCategory = "限位装置", ActionRequired = "检查幅度限位器和小车行程开关", Description = "2024年1月17日11:00，TC-002塔吊发生幅度限位报警，工作半径62m超过最大设计值60m。需检查幅度限位器和小车行程开关。", Status = Domain.Enums.RectificationStatus.Open, CreatedAt = now.AddHours(-1), Deadline = now.AddDays(2), DueDate = now.AddDays(2), Remarks = "整改期间TC-002仅允许执行低风险任务" }
        );
    }
}

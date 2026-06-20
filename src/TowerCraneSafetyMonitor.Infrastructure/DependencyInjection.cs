using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Application.Services;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=TowerCraneSafetyMonitor;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<ITowerCraneService, TowerCraneService>();
        services.AddScoped<ILiftingTaskService, LiftingTaskService>();
        services.AddScoped<IAlarmService, AlarmService>();
        services.AddScoped<IRectificationService, RectificationService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}

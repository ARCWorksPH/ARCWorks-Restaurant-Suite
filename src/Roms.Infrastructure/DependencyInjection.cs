using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roms.Application;
using Roms.Infrastructure.Persistence;
using Roms.Infrastructure.Services;

namespace Roms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRomsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        services.AddSingleton<MariaDbMigrationLockInterceptor>();
        services.AddDbContext<RomsDbContext>((provider, options) => options
            .UseMySQL(connectionString, mySql => mySql.EnableRetryOnFailure())
            .AddInterceptors(provider.GetRequiredService<MariaDbMigrationLockInterceptor>()));
        services.AddDbContextFactory<RomsDbContext>((provider, options) => options
            .UseMySQL(connectionString, mySql => mySql.EnableRetryOnFailure())
            .AddInterceptors(provider.GetRequiredService<MariaDbMigrationLockInterceptor>()), ServiceLifetime.Scoped);
        services.Configure<InventoryOptions>(configuration.GetSection("Features:Inventory"));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        return services;
    }
}

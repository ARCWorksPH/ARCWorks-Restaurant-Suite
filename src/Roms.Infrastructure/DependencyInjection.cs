using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roms.Application;
using Roms.Application.Ai;
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
        services.AddSingleton<IClock, SystemClock>();
        services.Configure<AiSecurityOptions>(configuration.GetSection("Ai"));
        services.AddSingleton<AiRequestGate>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IInventoryService, InventoryService>();
        // Keep the AI implementation available in source for a future version,
        // but do not activate it while the product hold is in force. This also
        // prevents a disabled app from resolving a gateway-backed service.
        var aiEnabled = configuration.GetValue<bool>("Ai:Enabled")
            && !configuration.GetValue<bool>("Ai:Hold");
        if (aiEnabled)
        {
            services.AddScoped<IAiFunctionService, AiFunctionService>();
            services.AddScoped<IAiAssistantService, AiAssistantService>();
        }
        services.AddScoped<IAttendanceService, AttendanceService>();
        return services;
    }
}

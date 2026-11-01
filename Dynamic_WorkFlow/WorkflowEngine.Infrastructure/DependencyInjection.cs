using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Infrastructure.BackgroundJobs;
using WorkflowEngine.Infrastructure.Identity;
using WorkflowEngine.Infrastructure.Notifications;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Register Background SLA and Queue Workers
        services.AddHostedService<EscalationWorker>();
        services.AddHostedService<NotificationQueueWorker>();

        return services;
    }
}

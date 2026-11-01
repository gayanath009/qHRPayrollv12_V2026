using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Common;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence;

public class WorkflowDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowConditionGroup> WorkflowConditionGroups => Set<WorkflowConditionGroup>();
    public DbSet<WorkflowCondition> WorkflowConditions => Set<WorkflowCondition>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowRole> WorkflowRoles => Set<WorkflowRole>();
    public DbSet<WorkflowApprover> WorkflowApprovers => Set<WorkflowApprover>();
    public DbSet<WorkflowEscalation> WorkflowEscalations => Set<WorkflowEscalation>();
    public DbSet<WorkflowDelegation> WorkflowDelegations => Set<WorkflowDelegation>();
    public DbSet<WorkflowSetting> WorkflowSettings => Set<WorkflowSetting>();
    public DbSet<WorkflowPermission> WorkflowPermissions => Set<WorkflowPermission>();
    public DbSet<WorkflowUserPermission> WorkflowUserPermissions => Set<WorkflowUserPermission>();
    public DbSet<WorkflowRolePermission> WorkflowRolePermissions => Set<WorkflowRolePermission>();
    
    public DbSet<WorkflowForm> WorkflowForms => Set<WorkflowForm>();
    public DbSet<WorkflowField> WorkflowFields => Set<WorkflowField>();
    public DbSet<WorkflowFieldOption> WorkflowFieldOptions => Set<WorkflowFieldOption>();
    public DbSet<WorkflowRule> WorkflowRules => Set<WorkflowRule>();
    public DbSet<WorkflowRuleCondition> WorkflowRuleConditions => Set<WorkflowRuleCondition>();
    public DbSet<WorkflowRuleAction> WorkflowRuleActions => Set<WorkflowRuleAction>();
    
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowInstanceData> WorkflowInstanceDatas => Set<WorkflowInstanceData>();
    public DbSet<WorkflowInstanceStep> WorkflowInstanceSteps => Set<WorkflowInstanceStep>();
    public DbSet<WorkflowApproval> WorkflowApprovals => Set<WorkflowApproval>();
    public DbSet<WorkflowHistory> WorkflowHistories => Set<WorkflowHistory>();
    public DbSet<WorkflowComment> WorkflowComments => Set<WorkflowComment>();
    public DbSet<WorkflowAttachment> WorkflowAttachments => Set<WorkflowAttachment>();
    public DbSet<WorkflowVariable> WorkflowVariables => Set<WorkflowVariable>();
    
    public DbSet<WorkflowAuditLog> WorkflowAuditLogs => Set<WorkflowAuditLog>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<EmailQueue> EmailQueues => Set<EmailQueue>();
    public DbSet<SMSQueue> SMSQueues => Set<SMSQueue>();
    public DbSet<WebhookQueue> WebhookQueues => Set<WebhookQueue>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<APIClient> APIClients => Set<APIClient>();
    public DbSet<APIAccessToken> APIAccessTokens => Set<APIAccessToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply global soft delete query filter and configure concurrency rowversion for all entity types inheriting BaseEntity
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Soft delete filter
                var method = typeof(WorkflowDbContext)
                    .GetMethod(nameof(ConfigureGlobalFilters), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);
                method?.Invoke(this, new object[] { builder });

                // Concurrency RowVersion configuration
                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.RowVersion))
                    .IsRowVersion();
            }
        }

        // Configure specific relationships to prevent cascading loop violations

        // User Manager hierarchy
        builder.Entity<User>()
            .HasOne(u => u.Manager)
            .WithMany(m => m.DirectReports)
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkflowTransition to FromStep / ToStep
        builder.Entity<WorkflowTransition>()
            .HasOne(t => t.FromStep)
            .WithMany(s => s.TransitionsFrom)
            .HasForeignKey(t => t.FromStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkflowTransition>()
            .HasOne(t => t.ToStep)
            .WithMany(s => s.TransitionsTo)
            .HasForeignKey(t => t.ToStepId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkflowDelegation
        builder.Entity<WorkflowDelegation>()
            .HasOne(d => d.Delegator)
            .WithMany()
            .HasForeignKey(d => d.DelegatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkflowDelegation>()
            .HasOne(d => d.Delegatee)
            .WithMany()
            .HasForeignKey(d => d.DelegateeId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkflowApproval
        builder.Entity<WorkflowApproval>()
            .HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkflowApproval>()
            .HasOne(a => a.DelegatedFromUser)
            .WithMany()
            .HasForeignKey(a => a.DelegatedFromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkflowHistory
        builder.Entity<WorkflowHistory>()
            .HasOne(h => h.PerformedBy)
            .WithMany()
            .HasForeignKey(h => h.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkflowComment
        builder.Entity<WorkflowComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkflowAttachment
        builder.Entity<WorkflowAttachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
            
        // APIAccessToken
        builder.Entity<APIAccessToken>()
            .HasOne(t => t.ApiClient)
            .WithMany()
            .HasForeignKey(t => t.ApiClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent cascade delete loop on WorkflowInstanceSteps
        builder.Entity<WorkflowInstanceStep>()
            .HasOne(s => s.WorkflowStep)
            .WithMany()
            .HasForeignKey(s => s.WorkflowStepId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent cascade delete loop on WorkflowInstance
        builder.Entity<WorkflowInstance>()
            .HasOne(i => i.WorkflowVersion)
            .WithMany()
            .HasForeignKey(i => i.WorkflowVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkflowInstance>()
            .HasOne(i => i.Requester)
            .WithMany()
            .HasForeignKey(i => i.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureGlobalFilters<TEntity>(ModelBuilder builder) where TEntity : BaseEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var username = _currentUserService.Username ?? "System";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = username;
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.IsActive = true;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedBy = username;
                    entry.Entity.ModifiedDate = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified; // Soft delete!
                    entry.Entity.DeletedBy = username;
                    entry.Entity.DeletedDate = DateTime.UtcNow;
                    entry.Entity.IsDeleted = true;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

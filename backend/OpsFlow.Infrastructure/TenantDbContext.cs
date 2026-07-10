using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure;

// Inherits IdentityDbContext so ASP.NET Core Identity tables live in the tenant DB (Azure path)
public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options) : IdentityDbContext(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<Region> Regions { get; set; } = default!;
    public DbSet<Store> Stores { get; set; } = default!;
    public DbSet<UserProfile> UserProfiles { get; set; } = default!;
    public DbSet<UserStoreAssignment> UserStoreAssignments { get; set; } = default!;
    public DbSet<TaskTemplate> TaskTemplates { get; set; } = default!;
    public DbSet<Checklist> Checklists { get; set; } = default!;
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems { get; set; } = default!;
    public DbSet<RecurringAssignment> RecurringAssignments { get; set; } = default!;
    public DbSet<RecurringAssignmentStore> RecurringAssignmentStores { get; set; } = default!;
    public DbSet<TaskInstance> TaskInstances { get; set; } = default!;
    public DbSet<TaskCompletion> TaskCompletions { get; set; } = default!;
    public DbSet<InventorySnapshot> InventorySnapshots { get; set; } = default!;
    public DbSet<StoreSettings> StoreSettings { get; set; } = default!;
    public DbSet<DepositLog> DepositLogs { get; set; } = default!;
    public DbSet<MissedDepositFlag> MissedDepositFlags { get; set; } = default!;
    public DbSet<FormTemplate> FormTemplates { get; set; } = default!;
    public DbSet<FormSubmission> FormSubmissions { get; set; } = default!;
    public DbSet<FormSubmissionApprovalStep> FormSubmissionApprovalSteps { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Region>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.TenantId, r.Name }).IsUnique();
        });

        builder.Entity<Store>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Region)
             .WithMany(r => r.Stores)
             .HasForeignKey(s => s.RegionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserProfile>(e =>
        {
            e.HasKey(u => u.UserId);
            e.HasOne(u => u.Store)
             .WithMany()
             .HasForeignKey(u => u.StoreId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(u => u.Region)
             .WithMany()
             .HasForeignKey(u => u.RegionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UserStoreAssignment>(e =>
        {
            e.HasKey(a => new { a.UserId, a.StoreId });
            e.HasOne(a => a.Store)
             .WithMany(s => s.UserStoreAssignments)
             .HasForeignKey(a => a.StoreId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.User)
             .WithMany(u => u.StoreAssignments)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.TenantId, t.Name, t.Scope }).IsUnique();
            e.HasOne(t => t.Region).WithMany().HasForeignKey(t => t.RegionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Store).WithMany().HasForeignKey(t => t.StoreId).OnDelete(DeleteBehavior.SetNull);
            e.Property(t => t.FieldsJson).HasColumnName("Fields").HasColumnType("jsonb");
        });

        builder.Entity<Checklist>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.TenantId, c.Name, c.Scope }).IsUnique();
            e.HasOne(c => c.Region).WithMany().HasForeignKey(c => c.RegionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Store).WithMany().HasForeignKey(c => c.StoreId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ChecklistTemplateItem>(e =>
        {
            e.HasKey(i => new { i.ChecklistId, i.TemplateId });
            e.HasOne(i => i.Checklist).WithMany(c => c.Items).HasForeignKey(i => i.ChecklistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Template).WithMany().HasForeignKey(i => i.TemplateId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RecurringAssignment>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.Checklist).WithMany().HasForeignKey(r => r.ChecklistId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RecurringAssignmentStore>(e =>
        {
            e.HasKey(a => new { a.RecurringAssignmentId, a.StoreId });
            e.HasOne(a => a.RecurringAssignment)
             .WithMany(r => r.TargetStores)
             .HasForeignKey(a => a.RecurringAssignmentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Store)
             .WithMany()
             .HasForeignKey(a => a.StoreId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskInstance>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.RecurringAssignmentId, t.DueAt }); // uniqueness checked in code
            e.HasIndex(t => new { t.StoreId, t.Status, t.DueAt });
            e.HasOne(t => t.RecurringAssignment).WithMany(r => r.TaskInstances).HasForeignKey(t => t.RecurringAssignmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Checklist).WithMany().HasForeignKey(t => t.ChecklistId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Store).WithMany().HasForeignKey(t => t.StoreId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskCompletion>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.TaskInstance).WithMany(t => t.Completions).HasForeignKey(c => c.TaskInstanceId).OnDelete(DeleteBehavior.Cascade);
            e.Property(c => c.FieldValuesJson).HasColumnName("FieldValues").HasColumnType("jsonb");
            e.Property(c => c.CorrectiveActionsJson).HasColumnName("CorrectiveActions").HasColumnType("jsonb");
        });

        builder.Entity<InventorySnapshot>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.StoreId, s.Date, s.ItemKey }).IsUnique(); // one snapshot per item per day
            e.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StoreSettings>(e =>
        {
            e.HasKey(s => s.StoreId);
            e.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.DoughNeedTargetsJson).HasColumnName("DoughNeedTargets").HasColumnType("jsonb");
        });

        builder.Entity<DepositLog>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.StoreId, d.SubmittedAt });
            e.HasOne(d => d.Store).WithMany().HasForeignKey(d => d.StoreId).OnDelete(DeleteBehavior.Restrict);
            e.Property(d => d.Amount).HasPrecision(18, 2);
        });

        builder.Entity<MissedDepositFlag>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.StoreId, f.BusinessDate }).IsUnique(); // one flag per store per business day
            e.HasOne(f => f.Store).WithMany().HasForeignKey(f => f.StoreId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FormTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.TenantId, t.Name, t.Scope }).IsUnique();
            e.HasOne(t => t.Region).WithMany().HasForeignKey(t => t.RegionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Store).WithMany().HasForeignKey(t => t.StoreId).OnDelete(DeleteBehavior.SetNull);
            e.Property(t => t.FieldsJson).HasColumnName("Fields").HasColumnType("jsonb");
            e.Property(t => t.ApprovalStepsJson).HasColumnName("ApprovalSteps").HasColumnType("jsonb");
        });

        builder.Entity<FormSubmission>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.StoreId, s.Status });
            e.HasIndex(s => s.SubmittedByUserId);
            e.HasOne(s => s.FormTemplate).WithMany().HasForeignKey(s => s.FormTemplateId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
            e.Property(s => s.FieldValuesJson).HasColumnName("FieldValues").HasColumnType("jsonb");
        });

        builder.Entity<FormSubmissionApprovalStep>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.SubmissionId, a.StepOrder });
            e.HasOne(a => a.FormSubmission).WithMany(s => s.ApprovalSteps).HasForeignKey(a => a.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

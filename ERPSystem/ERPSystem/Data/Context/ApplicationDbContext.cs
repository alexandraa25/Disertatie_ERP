using ERPSystem.Data.Audit;
using ERPSystem.Data.Entities;
//using ERPSystem.Data.TypeConfigurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace ERPSystem.Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }

        public DbSet<EmailRecipientLog> EmailRecipientLogs { get; set; }
        public DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ActivityLog> ActivityLog { get; set; }
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Guardian> Guardians => Set<Guardian>();
        public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSession> CourseSessions { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<CompanySettings> CompanySettings { get; set; }
        public DbSet<ContractTemplate> ContractTemplates { get; set; }
        public DbSet<StudentContract> StudentContracts { get; set; }
        public DbSet<ContractCourse> ContractCourses { get; set; }
        public DbSet<ContractParty> ContractParties { get; set; }
        public DbSet<ContractDiscount> ContractDiscounts { get; set; }
        public DbSet<ContractInstallment> ContractInstallments { get; set; }
        public DbSet<SigningToken> SigningTokens { get; set; }
        public DbSet<ContractAdditionalAct> ContractAdditionalAct { get; set; }
        public DbSet<ContractAdditionalActItem> ContractAdditionalActItem { get; set; }

        public DbSet<MarketingCampaign> MarketingCampaigns { get; set; }
        public DbSet<MarketingCampaignCourseSessions> MarketingCampaignCourseSessions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Employee> Employees { get; set; }

        public DbSet<EmployeeContact> EmployeeContact { get; set; }

        public DbSet<EmployeeAddress> EmployeeAddress { get; set; }

        public DbSet<EmployeeBank> EmployeeBank { get; set; }

        public DbSet<EmployeeContract> EmployeeContracts { get; set; }
        public DbSet<EmployeeLeave> EmployeeLeaves { get; set; }
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }

        public DbSet<PublicHoliday> PublicHolidays { get; set; }

        public DbSet<FeedbackForm> FeedbackForms { get; set; }

        public DbSet<CourseReview> CourseReviews { get; set; }



        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserNotificationSetting>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.EventType, x.Channel })
                      .IsUnique();

                entity.Property(x => x.Channel)
                      .HasConversion<string>();

                entity.Property(x => x.Digest)
                      .HasConversion<string>();
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                      .IsRequired()
                      .HasMaxLength(450);

                entity.Property(x => x.EventType)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(x => x.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(x => x.Message)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(x => x.IsRead)
                      .HasDefaultValue(false);

                entity.Property(x => x.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.CreatedAt });

                entity.HasIndex(x => new { x.UserId, x.IsRead });

                entity.Property(x => x.Type)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("Info");

                entity.Property(x => x.Link)
                      .HasMaxLength(500);

                entity.Property(x => x.EntityType)
                      .HasMaxLength(100);

                entity.Property(x => x.EntityId)
                      .HasMaxLength(100);

                entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });

                entity.HasIndex(x => new { x.EntityType, x.EntityId });
            });

            modelBuilder.Entity<ActivityLog>()
               .HasIndex(x => new { x.EntityType, x.EntityId });

            modelBuilder.Entity<Student>()
                .HasIndex(x => x.FullName);

            modelBuilder.Entity<StudentGuardian>()
                .HasKey(sg => new { sg.StudentId, sg.GuardianId });

            modelBuilder.Entity<StudentGuardian>()
                .HasOne(sg => sg.Student)
                .WithMany(s => s.StudentGuardians)
                .HasForeignKey(sg => sg.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentGuardian>()
                .HasOne(sg => sg.Guardian)
                .WithMany(g => g.StudentGuardians)
                .HasForeignKey(sg => sg.GuardianId)
                .OnDelete(DeleteBehavior.Cascade);
           
            modelBuilder.Entity<CourseSession>()
                .HasOne(x => x.Course)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseSession>()
                .HasIndex(x => new { x.CourseId, x.DayOfWeek, x.StartTime });

            modelBuilder.Entity<CourseSession>()
                .HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseSession>()
                .Property(x => x.Fee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Session)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(e => e.CourseSessionId)
               .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<StudentContract>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ContractNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.ContractNumber)
                    .IsUnique();

                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.ContractBody)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.PdfPath)
                   .HasMaxLength(500);

                entity.HasMany(e => e.Parties)
                    .WithOne(p => p.Contract)
                    .HasForeignKey(p => p.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Courses)
                    .WithOne(c => c.Contract)
                    .HasForeignKey(c => c.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Discounts)
                    .WithOne(d => d.Contract)
                    .HasForeignKey(d => d.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


               modelBuilder.Entity<StudentContract>()
                   .Property(c => c.Status)
                   .HasConversion<string>();

            modelBuilder.Entity<StudentContract>()
                   .HasIndex(c => c.Status);

            modelBuilder.Entity<StudentContract>()
                .HasIndex(c => c.CreatedAtUtc);

            modelBuilder.Entity<StudentContract>()
                .HasIndex(c => c.StartDate);

            modelBuilder.Entity<ContractParty>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Guardian)
                    .WithMany()
                    .HasForeignKey(e => e.GuardianId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
           
            modelBuilder.Entity<ContractCourse>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PriceSnapshot)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CourseNameSnapshot)
                    .HasMaxLength(200);

                entity.Property(e => e.SessionNameSnapshot)
                    .HasMaxLength(200);

                entity.HasOne(e => e.CourseSession)
                    .WithMany()
                    .HasForeignKey(e => e.CourseSessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContractDiscount>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Value)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Reason)
                    .HasMaxLength(300);
            });

            modelBuilder.Entity<ContractDiscount>()
               .HasOne(cd => cd.MarketingCampaign)
               .WithMany(mc => mc.ContractDiscounts)
               .HasForeignKey(cd => cd.MarketingCampaignId)
               .OnDelete(DeleteBehavior.NoAction);
            
            modelBuilder.Entity<MarketingCampaign>(entity =>
            {
                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

                entity.Property(x => x.DiscountValue)
                    .HasPrecision(18, 2);
            });

            modelBuilder.Entity<MarketingCampaignCourseSessions>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.MarketingCampaign)
                    .WithMany(x => x.CourseSessions)
                    .HasForeignKey(x => x.MarketingCampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.CourseSession)
                    .WithMany()
                    .HasForeignKey(x => x.CourseSessionId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ContractInstallment>()
                  .HasOne(i => i.Contract)
                  .WithMany(c => c.InstallmentsList)
                  .HasForeignKey(i => i.ContractId)
                  .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractInstallment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
                entity.Property(e => e.PaidAmount)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.ContractId);
            });

            modelBuilder.Entity<ContractAdditionalAct>()
               .HasOne(a => a.Contract)
               .WithMany(c => c.AdditionalActs)
               .HasForeignKey(a => a.ContractId)
               .OnDelete(DeleteBehavior.Cascade);
             

            modelBuilder.Entity<ContractAdditionalAct>(entity =>
            {
                entity.Property(x => x.ActNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.Body)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.Status)
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ContractAdditionalAct>()
               .HasIndex(a => a.ContractId);

            modelBuilder.Entity<ContractAdditionalActItem>()
                 .HasOne(i => i.Act)
                 .WithMany(a => a.Items)
                 .HasForeignKey(i => i.ActId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractAdditionalAct>()
                .Property(x => x.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ContractAdditionalActItem>()
                .Property(x => x.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Contract)
                    .WithMany()
                    .HasForeignKey(p => p.ContractId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Installment)
                    .WithMany()
                    .HasForeignKey(p => p.InstallmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.ContractId);
                entity.HasIndex(p => p.InstallmentId);

                entity.Property(p => p.Amount)
                    .HasPrecision(18, 2);

                entity.Property(p => p.Status)
                    .HasDefaultValue("Completed");
            });

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasIndex(x => new { x.UserId, x.EmploymentStatus });

            modelBuilder.Entity<Employee>()
               .Property(x => x.Salary)
               .HasPrecision(18, 2);
             
            modelBuilder.Entity<EmployeeContract>()
               .HasOne(x => x.Employee)
               .WithMany(x => x.Contracts)
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeLeave>()
               .HasOne(x => x.Employee)
               .WithMany(x => x.Leaves)
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeDocument>()
               .HasOne(x => x.Employee)
               .WithMany(x => x.Documents)
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
               .HasOne(e => e.Address)
               .WithOne(a => a.Employee)
               .HasForeignKey<EmployeeAddress>(a => a.EmployeeId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Bank)
                .WithOne(b => b.Employee)
                .HasForeignKey<EmployeeBank>(b => b.EmployeeId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Contact)
                .WithOne(c => c.Employee)
                .HasForeignKey<EmployeeContact>(c => c.EmployeeId);


            modelBuilder.Entity<EmailLog>()
    .HasMany(x => x.Recipients)
    .WithOne(x => x.EmailLog)
    .HasForeignKey(x => x.EmailLogId)
    .OnDelete(DeleteBehavior.Cascade);

        }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();

            var result = await base.SaveChangesAsync(cancellationToken);

            await OnAfterSaveChanges(auditEntries);

            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();

            var auditEntries = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is ActivityLog ||
                    entry.State == EntityState.Detached ||
                    entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    EntityType = entry.Entity.GetType().Name,
                    Action = entry.State switch
                    {
                        EntityState.Added => "Create",
                        EntityState.Deleted => "Delete",
                        EntityState.Modified => "Update",
                        _ => "Unknown"
                    }
                };

                foreach (var property in entry.Properties)
                {
                    var propName = property.Metadata.Name;

                    if (propName == "UpdatedAtUtc")
                        continue;

                    if (property.Metadata.IsForeignKey())
                        continue;

                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propName] = property.CurrentValue!;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.OldValues[propName] = property.OriginalValue;
                                auditEntry.NewValues[propName] = property.CurrentValue;
                            }
                            break;
                    }
                }

                if (auditEntry.HasChanges)
                    auditEntries.Add(auditEntry);
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries.Count == 0)
                return;

            foreach (var entry in auditEntries)
            {
                entry.UpdateKeyValues();

                var log = new ActivityLog
                {
                    EntityType = entry.EntityType,
                    EntityId = entry.GetEntityId().ToString(),
                    Action = entry.Action,
                    Changes = JsonSerializer.Serialize(new
                    {
                        Old = entry.OldValues,
                        New = entry.NewValues
                    }),
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = "system" 
                };

                Set<ActivityLog>().Add(log);
            }

            await base.SaveChangesAsync();
        }
    }
}
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Student.Models;
//using ERPSystem.Data.TypeConfigurations;
using ERPSystem.Modules.UserProfile.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace ERPSystem.Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Guardian> Guardians => Set<Guardian>();
        public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSession> CourseSessions { get; set; }

        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

        public DbSet<StudentContract> StudentContracts { get; set; }
        public DbSet<ContractCourse> ContractCourses { get; set; }
        public DbSet<ContractParty> ContractParties { get; set; }
        public DbSet<ContractDiscount> ContractDiscounts { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Config minim (chei + indexuri)
            modelBuilder.Entity<UserProfile>()
                .HasKey(x => x.UserId);

            modelBuilder.Entity<UserNotificationSetting>()
                .HasIndex(x => new { x.UserId, x.EventType, x.Channel })
                .IsUnique();

            // (opțional) index pentru audit
            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => new { x.UserId, x.TimestampUtc });

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

            modelBuilder.Entity<Course>()
                .Property(x => x.Price)
                 .HasPrecision(18, 2);

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

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.CourseSessionId)
                .OnDelete(DeleteBehavior.NoAction); // sau Restrict

            // StudentContract
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
            // ContractCourse
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

        }

    }
}
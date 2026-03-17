
﻿using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Model.Appointment;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Model.Logging;
using Smart_ward_management_system.Model.Notice;
using Smart_ward_management_system.Model.Polls;
using Smart_ward_management_system.Model.Services;
using Smart_ward_management_system.Model.Services.Complaints;
using Smart_ward_management_system.Model.Services.ProbableServices;
using Smart_ward_management_system.Model.WasteManagement_And_Scheduling;

namespace Smart_ward_management_system.Data

{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext()
        { } 
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        public DbSet<ActivityLog>ActivityLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<CitizenVerificationRequest> CitizenVerificationRequests { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<ComplaintEscalation> ComplaintEscalations { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ServiceApprovalFlow>ServiceApprovalFlows { get; set; }
        public DbSet<ServiceRequest>ServiceRequests { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }
        public DbSet<StatusMaster> StatusMasters { get; set; }

        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Queue>Queues { get; set; }
        public DbSet<Token> Tokens { get; set; }

        public DbSet<Notice> Notices { get; set; }
        public DbSet<NoticeCategory> NoticeCategories { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<PollCategory> PollCategories { get; set; }

        public DbSet<AddressVerificationRequest> AddressVerificationRequests { get; set; }
        public DbSet<BirthCertificateRequest> BirthCertificateRequests { get; set; }
        public DbSet<DeathCertificateRequest> DeathCertificateRequests { get; set; }
        public DbSet<MarriageRegistrationRequest> MarriageRegistrationRequests { get; set; }
        public DbSet<MigrationCertificateRequest> MigrationCertificateRequests { get; set; }
        public DbSet<PropertyDocumentRequest> PropertyDocumentRequests { get; set; }
        public DbSet<RecommendationLetterRequest> RecommendationLetterRequests { get; set; }

        public DbSet<WasteCollectionRoute> WasteCollectionRoutes { get; set; }
        public DbSet<WasteVehicle> WasteVehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<CollectionPoint> CollectionPoints { get; set; }
        public DbSet<RouteSchedule> RouteSchedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<User>().HasData(new User
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "admin",
                Role = "Staff",
                PasswordHash = "AQAAAAIAAYagAAAAEAB9zLigadl2431aHLhlcKzzUiGBjUWRmnwFIDF3CT94M3BkfYp/3J7pS66wz7oj2w==", // ideally hashed
                AccountStatus = "Active",
                IsEmailConfirmed = true,

                FullNameNepali = "प्रशासक",
                FullNameEnglish = "System Admin",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "9800000000",
                Email = "admin@ward.gov.np",

                CitizenshipNumber = "123456789",
                CitizenshipIssuedDistrict = "Kathmandu",
                CitizenshipIssuedDate = new DateTime(2010, 5, 5),

                PermanentAddress = "Kathmandu",
                TemporaryAddress = "Kathmandu",
                WardNumber = "1",
                Municipality = "Kathmandu Metropolitan",
                District = "Kathmandu",
                Province = "Bagmati",

                IsVerified = true,
                VerificationStatus = VerificationStatusEnum.Approved,
                VerifiedBy = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                VerifiedAt = new DateTime(2024, 1, 1),

                CreatedAt = new DateTime(2024, 1, 1)
            }
            );
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);


            modelBuilder.Entity<Token>()
             .Property(t => t.TokenSequence)
             .ValueGeneratedOnAdd();
                    modelBuilder.Entity<PollVote>()
                .HasOne<Poll>()
                .WithMany()
                .HasForeignKey(v => v.PollId)
                .OnDelete(DeleteBehavior.Restrict);

                    modelBuilder.Entity<PollVote>()
                        .HasOne<PollOption>()
                        .WithMany(o => o.Votes)
                        .HasForeignKey(v => v.OptionId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WasteCollectionRoute>()
                .HasOne(r => r.AssignedVehicle)
                .WithMany(v => v.Routes)
                .HasForeignKey(r => r.AssignedVehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WasteCollectionRoute>()
                .HasOne(r => r.AssignedDriver)
                .WithMany(d => d.Routes)
                .HasForeignKey(r => r.AssignedDriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes
            modelBuilder.Entity<WasteCollectionRoute>()
                .HasIndex(r => r.ScheduledDate)
                .HasDatabaseName("IX_Routes_ScheduledDate");

            modelBuilder.Entity<WasteCollectionRoute>()
                .HasIndex(r => r.Status)
                .HasDatabaseName("IX_Routes_Status");

            modelBuilder.Entity<WasteVehicle>()
                .HasIndex(v => v.VehicleNumber)
                .IsUnique();

            modelBuilder.Entity<Driver>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique();

            // Seed initial data
            modelBuilder.Entity<WasteVehicle>().HasData(
                new WasteVehicle { Id = Guid.NewGuid(), VehicleNumber = "VH-001", VehicleName = "Truck 1", Status = VehicleStatus.Available, Capacity = 5, VehicleType = "Compactor", IsActive = true, Latitude = 0, Longitude = 0, LastUpdatedLocation = DateTime.Now },
                new WasteVehicle { Id = Guid.NewGuid(), VehicleNumber = "VH-002", VehicleName = "Truck 2", Status = VehicleStatus.Available, Capacity = 3, VehicleType = "Dumper", IsActive = true, Latitude = 0, Longitude = 0, LastUpdatedLocation = DateTime.Now }
            );

            modelBuilder.Entity<Driver>().HasData(
                new Driver { Id = Guid.NewGuid(), Name = "John Doe", LicenseNumber = "DL-001", PhoneNumber = "1234567890", Email = "john@example.com", IsAvailable = true },
                new Driver { Id = Guid.NewGuid(), Name = "Jane Smith", LicenseNumber = "DL-002", PhoneNumber = "0987654321", Email = "jane@example.com", IsAvailable = true }
            );

        }

    }
}

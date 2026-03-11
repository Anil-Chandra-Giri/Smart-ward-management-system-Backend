using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Model.Appointment;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Model.Logging;
using Smart_ward_management_system.Model.Services;
using Smart_ward_management_system.Model.Services.Complaints;
using Smart_ward_management_system.Model.Services.ProbableServices;

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
        public DbSet<Notice> Notices { get; set; }
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
        public DbSet<AddressVerificationRequest> AddressVerificationRequests { get; set; }
        public DbSet<BirthCertificateRequest> BirthCertificateRequests { get; set; }
        public DbSet<DeathCertificateRequest> DeathCertificateRequests { get; set; }
        public DbSet<MarriageRegistrationRequest> MarriageRegistrationRequests { get; set; }
        public DbSet<MigrationCertificateRequest> MigrationCertificateRequests { get; set; }
        public DbSet<PropertyDocumentRequest> PropertyDocumentRequests { get; set; }
        public DbSet<RecommendationLetterRequest> RecommendationLetterRequests { get; set; }

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
        }

    }
}

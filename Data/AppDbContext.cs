using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<OTP> OTPs { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardType> CardTypes { get; set; }
        public DbSet<CardApplication> CardApplications { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        // Note: Notification and Transaction entities are empty, so not included yet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // USER ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<User>(entity =>
            {
                // Indexes for performance
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");

                entity.HasIndex(u => u.PhoneNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_PhoneNumber");

                entity.HasIndex(u => u.CreatedAt)
                    .HasDatabaseName("IX_Users_CreatedAt");

                entity.HasIndex(u => new { u.IsActive, u.IsEmailVerified })
                    .HasDatabaseName("IX_Users_Status");

                // Column constraints
                entity.Property(u => u.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(u => u.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(u => u.PinHash)
                    .HasMaxLength(500);

                entity.Property(u => u.CitizenImgFront)
                    .HasMaxLength(500);

                entity.Property(u => u.CitizenImgBack)
                    .HasMaxLength(500);

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationship configuration
                entity.HasOne(u => u.Role)
                    .WithMany()
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Users_Roles");
            });

            // =========================================================
            // ROLE ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Roles_Name");

                entity.Property(r => r.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // =========================================================
            // OTP ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<OTP>(entity =>
            {
                // Composite index for OTP lookup performance
                entity.HasIndex(o => new { o.UserId, o.OTPType, o.IsUsed })
                    .HasDatabaseName("IX_OTPs_UserType_Used");

                entity.HasIndex(o => new { o.OTPCode, o.OTPType, o.ExpiryTime })
                    .HasDatabaseName("IX_OTPs_Code_Type_Expiry");

                entity.HasIndex(o => o.ExpiryTime)
                    .HasDatabaseName("IX_OTPs_ExpiryTime");

                // Column constraints
                entity.Property(o => o.OTPCode)
                    .IsRequired()
                    .HasMaxLength(6)
                    .IsFixedLength();

                entity.Property(o => o.OTPType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(o => o.IsUsed)
                    .HasDefaultValue(false);

                // Relationship configuration
                entity.HasOne(o => o.User)
                    .WithMany()
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_OTPs_Users");
            });

            // =========================================================
            // CARD TYPE ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<CardType>(entity =>
            {
                entity.HasIndex(ct => ct.CardName)
                    .IsUnique()
                    .HasDatabaseName("IX_CardTypes_CardName");

                entity.Property(ct => ct.CardName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(ct => ct.CardNetwork)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(ct => ct.Description)
                    .HasMaxLength(500);

                entity.Property(ct => ct.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(ct => ct.CreditLimit)
                    .HasPrecision(18, 2);

                entity.Property(ct => ct.AnnualFee)
                    .HasPrecision(18, 2);

                entity.Property(ct => ct.CashbackRate)
                    .HasPrecision(5, 4); // For percentage rates like 0.015 (1.5%)
            });

            // =========================================================
            // CARD ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasIndex(c => c.CardNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Cards_CardNumber");

                entity.HasIndex(c => c.UserId)
                    .HasDatabaseName("IX_Cards_UserId");

                entity.HasIndex(c => new { c.UserId, c.CardStatus })
                    .HasDatabaseName("IX_Cards_User_Status");

                entity.Property(c => c.CardNumber)
                    .IsRequired()
                    .HasMaxLength(19); // For credit card numbers

                entity.Property(c => c.CVV)
                    .IsRequired()
                    .HasMaxLength(4);

                entity.Property(c => c.CardStatus)
                    .IsRequired()
                    .HasMaxLength(20);

                // Relationships
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Cards_Users");

                entity.HasOne(c => c.CardType)
                    .WithMany()
                    .HasForeignKey(c => c.CardTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Cards_CardTypes");
            });

            // =========================================================
            // CARD APPLICATION ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<CardApplication>(entity =>
            {
                entity.HasIndex(ca => ca.UserId)
                    .HasDatabaseName("IX_CardApplications_UserId");

                entity.HasIndex(ca => new { ca.UserId, ca.Status })
                    .HasDatabaseName("IX_CardApplications_User_Status");

                entity.HasIndex(ca => ca.ApplicationDate)
                    .HasDatabaseName("IX_CardApplications_ApplicationDate");

                entity.Property(ca => ca.GrossAnnualIncome)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.Property(ca => ca.Occupation)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(ca => ca.IncomeSource)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(ca => ca.CompanyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(ca => ca.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");

                entity.Property(ca => ca.ApplicationDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasOne(ca => ca.User)
                    .WithMany()
                    .HasForeignKey(ca => ca.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_CardApplications_Users");

                entity.HasOne(ca => ca.CardType)
                    .WithMany()
                    .HasForeignKey(ca => ca.CardTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_CardApplications_CardTypes");
            });

            // =========================================================
            // CHAT CONVERSATION ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<ChatConversation>(entity =>
            {
                entity.HasIndex(cc => cc.UserId)
                    .HasDatabaseName("IX_ChatConversations_UserId");

                entity.HasIndex(cc => new { cc.UserId, cc.Status })
                    .HasDatabaseName("IX_ChatConversations_User_Status");

                entity.HasIndex(cc => cc.CreatedAt)
                    .HasDatabaseName("IX_ChatConversations_CreatedAt");

                entity.HasIndex(cc => cc.LastMessageAt)
                    .HasDatabaseName("IX_ChatConversations_LastMessageAt");

                entity.Property(cc => cc.Subject)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(cc => cc.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.Property(cc => cc.AssignedAgentName)
                    .HasMaxLength(100);

                entity.Property(cc => cc.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasOne(cc => cc.User)
                    .WithMany()
                    .HasForeignKey(cc => cc.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ChatConversations_Users");

                entity.HasMany(cc => cc.Messages)
                    .WithOne(cm => cm.Conversation)
                    .HasForeignKey(cm => cm.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ChatMessages_ChatConversations");
            });

            // =========================================================
            // CHAT MESSAGE ENTITY CONFIGURATION
            // =========================================================
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasIndex(cm => cm.ConversationId)
                    .HasDatabaseName("IX_ChatMessages_ConversationId");

                entity.HasIndex(cm => new { cm.ConversationId, cm.SentAt })
                    .HasDatabaseName("IX_ChatMessages_Conversation_SentAt");

                entity.HasIndex(cm => new { cm.ConversationId, cm.IsRead })
                    .HasDatabaseName("IX_ChatMessages_Conversation_IsRead");

                entity.Property(cm => cm.Content)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(cm => cm.SenderType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(cm => cm.SenderName)
                    .HasMaxLength(100);

                entity.Property(cm => cm.AttachmentUrl)
                    .HasMaxLength(500);

                entity.Property(cm => cm.MessageType)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Text");

                entity.Property(cm => cm.IsRead)
                    .HasDefaultValue(false);

                entity.Property(cm => cm.SentAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // =========================================================
            // SEED DATA
            // =========================================================

            // Seed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Admin" }
            );

            // Seed default admin user (password will be hashed at runtime)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "System Administrator",
                    Email = "admin@mastercredit.com",
                    PhoneNumber = "0901000000",
                    PasswordHash = "$2a$11$8F8VnW7VQ9d.qjrYO4hZ8.OG4CYpVcGE3yF/7yVR0JIL5Xd1yjFZq", // Admin123!@# (pre-hashed)
                    RoleId = 2, // Admin role
                    IsEmailVerified = true,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed default card types
            modelBuilder.Entity<CardType>().HasData(
                new CardType
                {
                    Id = 1,
                    CardName = "Classic",
                    CardNetwork = "MasterCard",
                    CreditLimit = 10000000, // 10 triệu
                    AnnualFee = 0,
                    CashbackRate = 0.005m, // 0.5%
                    Description = "Thẻ tín dụng cơ bản với các tính năng thiết yếu",
                    ImageUrl = "/images/cards/classic.png"
                },
                new CardType
                {
                    Id = 2,
                    CardName = "Gold",
                    CardNetwork = "MasterCard",
                    CreditLimit = 50000000, // 50 triệu
                    AnnualFee = 500000, // 500k/năm
                    CashbackRate = 0.01m, // 1%
                    Description = "Thẻ tín dụng cao cấp với nhiều ưu đãi hấp dẫn",
                    ImageUrl = "/images/cards/gold.png"
                },
                new CardType
                {
                    Id = 3,
                    CardName = "Platinum",
                    CardNetwork = "MasterCard",
                    CreditLimit = 200000000, // 200 triệu
                    AnnualFee = 2000000, // 2 triệu/năm
                    CashbackRate = 0.015m, // 1.5%
                    Description = "Thẻ tín dụng cao cấp nhất với đầy đủ tiện ích",
                    ImageUrl = "/images/cards/platinum.png"
                }
            );
        }
    }
}

using FusionComms.Entities;
using FusionComms.Entities.WhatsApp;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FusionComms.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {

        //public DbSet<User> Users { get; set; }
        public DbSet<RegisteredMontyUser> RegisteredMontyUsers { get; set; }
        public DbSet<SMSNotification> SMSNotifications { get; set; }

        public DbSet<RegisteredSesUser> RegisteredSesUsers { get; set; }
        public DbSet<EmailNotification> EmailNotifications { get; set; }
        public DbSet<WhatsAppBusiness> WhatsAppBusinesses { get; set; }
        public DbSet<WhatsAppAppConfig> WhatsAppAppConfigs { get; set; }
        public DbSet<WhatsAppMedia> WhatsAppMedia { get; set; }
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; }
        public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; }
        public DbSet <SentEmail> SentEmails { get; set; }
        public DbSet <WhatsAppProduct> WhatsAppProducts { get; set; }
        public DbSet <WhatsAppProductSet> WhatsAppProductSets { get; set; }
        public DbSet <WhatsAppProductSetGrouping> WhatsAppProductSetGroupings { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet <OrderSession> OrderSessions { get; set; }
        public DbSet <Order> Orders { get; set; }
        public DbSet <OrderItem> OrderItems { get; set; }

        public DbSet<OTP> OTPs { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options)
                : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>().HasIndex(c => c.Email).IsUnique(true);
            builder.Entity<User>().HasIndex(c => c.PhoneNumber).IsUnique(true);
            builder.Entity<WhatsAppProduct>()
                .HasKey(p => new { p.ProductId, p.RevenueCenterId });

            base.OnModelCreating(builder);
        }
    }
}

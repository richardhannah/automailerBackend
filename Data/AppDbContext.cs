using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Login> Logins => Set<Login>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Login>(entity =>
        {
            entity.HasKey(l => l.UserId);
            entity.HasIndex(l => l.Username).IsUnique();
            entity.HasOne(l => l.User)
                  .WithOne(u => u.Login)
                  .HasForeignKey<Login>(l => l.UserId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.CustomerId);
            entity.HasIndex(c => c.CustomerGuid).IsUnique();
            entity.HasIndex(c => c.Email);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(t => t.EmailTemplateId);
            entity.HasIndex(t => t.EmailTemplateGuid).IsUnique();
        });

        // Seed default admin user (password: admin)
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<User>().HasData(new User
        {
            UserId = adminId,
            Role = UserRole.Admin
        });
        modelBuilder.Entity<Login>().HasData(new Login
        {
            UserId = adminId,
            Username = "admin",
            Salt = "VHdVilQslnHUabg98c3aMcgINnenn4amr4uSfNZBMfE=",
            Password = "y3iapE4AqVl2GXGj+vfpk2KO9FCAcar+HCjPlusU7A4=",
            Token = Guid.Empty
        });
    }
}

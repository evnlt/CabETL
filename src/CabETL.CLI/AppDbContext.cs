using CabETL.CLI.Entities;
using Microsoft.EntityFrameworkCore;

namespace CabETL.CLI;

// EF Core is only use for schema configuration and migrations
// TODO - add docker config in readme 
public class AppDbContext : DbContext
{
    public DbSet<CabDataEntity> CabData { get; set; } = default!;
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // TODO - add reading connection string from appsettings file
        optionsBuilder.UseSqlServer("Data Source=(local);Initial Catalog=CabData;User Id=sa;Password=Qwerty123$%;TrustServerCertificate=true");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CabDataEntity>(entity =>
        {
            // TODO - configure for optimization
            // TODO - test how to update migrations on compilation

            entity.HasNoKey();
            
            entity.Property(e => e.StoreAndFwd)
                .HasConversion<string>()
                .HasMaxLength(3);

            /*entity.Property(e => e.FareAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TipAmount).HasColumnType("decimal(10,2)");*/
        });
    }
}
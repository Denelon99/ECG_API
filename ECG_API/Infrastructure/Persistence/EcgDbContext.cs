using ECG_API.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECG_API.Infrastructure.Persistence;

public class EcgDbContext : DbContext
{
    public EcgDbContext(DbContextOptions<EcgDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<AnalysisRecord>().HasOne<User>().WithMany().HasForeignKey(r => r.UserId);
    }
    public DbSet<AnalysisRecord> AnalysisRecords =>  Set<AnalysisRecord>();
    
    public DbSet<User> Users =>  Set<User>();
}

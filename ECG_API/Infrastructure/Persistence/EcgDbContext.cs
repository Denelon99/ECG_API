using ECG_API.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECG_API.Infrastructure.Persistence;

public class EcgDbContext : DbContext
{
    public EcgDbContext(DbContextOptions<EcgDbContext> options) : base(options)
    {
    }
    
    public DbSet<AnalysisRecord> AnalysisRecords =>  Set<AnalysisRecord>();
}

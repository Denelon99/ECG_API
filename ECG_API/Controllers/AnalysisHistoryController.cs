using ECG_API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECG_API.Controllers;

[ApiController]
[Route("api/analysis/history")]
public class AnalysisHistoryController : ControllerBase
{
    private readonly EcgDbContext _dbContext;

    public AnalysisHistoryController(EcgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var records = await _dbContext.AnalysisRecords.OrderByDescending(r => r.CreatedAtUtc).ToListAsync();
        return Ok(records);
    }
}

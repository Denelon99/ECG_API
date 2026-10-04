using ECG_API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECG_API.Infrastructure.Extensions;

namespace ECG_API.Controllers;

[ApiController]
[Authorize]
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
        var userId = User.GetUserId();
        var records = await _dbContext.AnalysisRecords.Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc).ToListAsync();
        return Ok(records);
    }
}

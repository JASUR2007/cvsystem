using backend.DTOs.Home;
using backend.DTOs.Positions;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HomeController(IHomeService homeService) : ControllerBase
{
    [HttpGet("statistics")]
    public async Task<ActionResult<HomeStatisticsResponse>> GetStatistics(CancellationToken cancellationToken)
    {
        var result = await homeService.GetStatisticsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("latest-positions")]
    public async Task<ActionResult<List<PositionListItem>>> GetLatestPositions(CancellationToken cancellationToken)
    {
        var result = await homeService.GetLatestPositionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("popular-positions")]
    public async Task<ActionResult<List<PopularPositionResponse>>> GetPopularPositions(CancellationToken cancellationToken)
    {
        var result = await homeService.GetPopularPositionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("tags")]
    public async Task<ActionResult<List<TagResponse>>> GetTags(CancellationToken cancellationToken)
    {
        var result = await homeService.GetPopularTagsAsync(cancellationToken);
        return Ok(result);
    }
}

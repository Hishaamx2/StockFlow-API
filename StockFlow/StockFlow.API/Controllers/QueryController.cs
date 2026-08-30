using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.API.Dtos;
using StockFlow.API.Interfaces;

namespace StockFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController(IAiQueryService aiQueryService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<QueryResponseDto>> Ask(QueryRequestDto dto)
    {
        var result = await aiQueryService.AskAsync(dto.Question, dto.History);
        return Ok(result);
    }
}

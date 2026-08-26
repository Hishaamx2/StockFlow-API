using Microsoft.AspNetCore.Mvc;
using StockFlow.API.Dtos;
using StockFlow.API.Interfaces;

namespace StockFlow.API.Controllers;

//check result of postgress if alr taken or inval

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        if (result is null)
            return Conflict("Username is already taken.");

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        if (result is null)
            return Unauthorized("Invalid username or password.");

        return Ok(result);
    }
}

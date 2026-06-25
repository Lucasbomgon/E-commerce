using Ecommerce.Api.Dtos.Auth;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var response = await authService.RegisterAsync(request);

        if (response is null)
        {
            return Conflict(new { message = "E-mail ja cadastrado." });
        }

        return Created(string.Empty, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);

        if (response is null)
        {
            return Unauthorized(new { message = "E-mail ou senha invalidos." });
        }

        return Ok(response);
    }
}

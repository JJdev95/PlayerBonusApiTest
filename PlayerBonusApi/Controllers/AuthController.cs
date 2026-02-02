using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Application.Dtos;

namespace PlayerBonusApi.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    // This is made this way solely for the purpose of the task, its not secure in real case scenario
    [HttpPost("dev-token")]
    public ActionResult<DevTokenResponse> CreateDevToken([FromBody] DevTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest("UserId and UserName are required.");

        var result = _authService.CreateDevToken(request);
        return Ok(result);
    }
}

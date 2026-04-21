using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService          _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth   = auth;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with email and password. Returns JWT with embedded user + license claims.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new LoginResponse(false, null, null,
                "Email and password are required."));
        }

        var result = await _auth.LoginAsync(request, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            return Unauthorized(result);
        }

        _logger.LogInformation("Successful login for email: {Email}", request.Email);
        return Ok(result);
    }
}

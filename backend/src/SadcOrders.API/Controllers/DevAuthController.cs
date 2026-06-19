using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SadcOrders.API.Auth;

namespace SadcOrders.API.Controllers;

[ApiController]
[Route("api/dev")]
[AllowAnonymous]
public class DevAuthController(IConfiguration configuration, IOptions<DevAuthOptions> devAuthOptions) : ControllerBase
{
    /// <summary>
    /// Development login — validates username/password and returns a JWT with role claims.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(DevLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Login([FromBody] DevLoginRequest request)
    {
        if (!devAuthOptions.Value.Enabled)
            return NotFound();

        var user = devAuthOptions.Value.Users.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, request.Username, StringComparison.OrdinalIgnoreCase)
            && candidate.Password == request.Password);

        if (user is null)
            return Unauthorized(new { error = "Invalid username or password." });

        return Ok(BuildLoginResponse(user));
    }

    private DevLoginResponse BuildLoginResponse(DevAuthUser user) =>
        new(
            JwtTokenIssuer.IssueToken(configuration, user.Username, user.Roles),
            user.Username,
            user.Roles);
}

public record DevLoginRequest(string Username, string Password);

public record DevLoginResponse(string Token, string Username, IReadOnlyList<string> Roles);

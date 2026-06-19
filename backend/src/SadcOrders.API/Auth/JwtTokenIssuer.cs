using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SadcOrders.API.Auth;

public static class JwtTokenIssuer
{
    public static string IssueToken(IConfiguration configuration, string username, IEnumerable<string> roles)
    {
        var key = configuration["Jwt:Key"] ?? "SadcOrdersDevSigningKeyMustBeAtLeast32Chars!";
        var issuer = configuration["Jwt:Issuer"] ?? "sadc-orders-dev";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.UniqueName, username)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            issuer,
            claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OAuthJWT.Api.Security;

public sealed class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public TokenResult? Authenticate(string username, string password)
    {
        var user = _options.Users.SingleOrDefault(candidate =>
            string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase) && candidate.Password == password);
        if (user is null) return null;

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var token = new JwtSecurityToken(
            _options.Issuer, _options.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Username), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.Role)],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256));
        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, user.Role);
    }
}

public sealed record TokenResult(string Token, DateTime ExpiresAt, string Role);

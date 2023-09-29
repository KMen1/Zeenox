using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Zeenox.Models;

namespace Zeenox.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiVersion("1.0")]
public class IdentityController : ControllerBase
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private readonly string _key;

    public IdentityController(IConfiguration configuration)
    {
        _key = configuration["JwtSettings:Key"]!;
    }

    [Authorize]
    [HttpGet]
    public IActionResult VerifyToken(ulong? id)
    {
        if (id is null)
            return Ok();
        
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);
        
        if (guildId != id)
            return Unauthorized();
        
        return Ok();
    }
    
    [HttpPost]
    public IActionResult GenerateToken([FromBody] TokenGenerationRequest request)
    {
        var _ = HttpContext.Request.Host.Value;
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new("userId", request.UserId.ToString()),
            new("guildId", request.GuildId.ToString())
        };

        foreach (var claimPair in request.CustomClaims)
        {
            var jsonElement = (JsonElement)claimPair.Value;
            var valueType = jsonElement.ValueKind switch
            {
                JsonValueKind.True => ClaimValueTypes.Boolean,
                JsonValueKind.False => ClaimValueTypes.Boolean,
                JsonValueKind.Number => ClaimValueTypes.Double,
                _ => ClaimValueTypes.String
            };

            var claim = new Claim(claimPair.Key, claimPair.Value.ToString()!, valueType);
            claims.Add(claim);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            Issuer = "https://zeenox.gg",
            Audience = "https://zeenox.gg",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha512Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var jwt = tokenHandler.WriteToken(token);
        return Ok(jwt);
    }
}
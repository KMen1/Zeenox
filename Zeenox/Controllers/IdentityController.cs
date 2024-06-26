﻿using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Zeenox.Models;

namespace Zeenox.Controllers;

[EnableRateLimiting("global")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class IdentityController(IConfiguration configuration) : ControllerBase
{
    private readonly string _key = configuration["JwtKey"]!;

    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost]
    public IActionResult GetAuthToken([FromBody] TokenGenerationRequest request)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("USER_ID", request.UserId.ToString()),
            new("GUILD_ID", request.GuildId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = "Zeenox-API",
            Audience = "Zeenox-Client",
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
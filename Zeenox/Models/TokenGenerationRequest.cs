namespace Zeenox.Models;

public class TokenGenerationRequest
{
    public string Username { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public Dictionary<string, object> CustomClaims { get; set; }
}
namespace Bot.Models;

public class UserPermissionDto
{
    public required string Username { get; init; }
    public required long UserId { get; init; }
    public required string Permissions { get; init; }
    public required long LastModified { get; init; }

    public bool IsWhitelisted => _whitelisted ??= this.Permissions.Contains("whitelisted");
    public bool IsBlacklisted => _blacklisted ??= this.Permissions.Contains("blacklisted");

    private bool? _blacklisted;
    private bool? _whitelisted;
}
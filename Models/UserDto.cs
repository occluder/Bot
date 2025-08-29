namespace Bot.Models;
public class UserDto
{
    public long Id { get; init; }
    public required string Username { get; init; }
    public long AddedAt { get; init; }
}

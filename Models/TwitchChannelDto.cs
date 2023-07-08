using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Models;
public class TwitchChannelDto
{
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public required long Id { get; set; }
    public required string AvatarUrl { get; set; }
    public required int Priority { get; set; }
    public required bool IsLogged { get; set; }
    public required DateTime DateJoined { get; set; }
}

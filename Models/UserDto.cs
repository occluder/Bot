using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Models;
public class UserDto
{
    public long Id { get; init; }
    public required string Username { get; init; }
}

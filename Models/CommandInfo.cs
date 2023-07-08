using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.Enums;

namespace Bot.Models;
internal record CommandInfo(string Name, string Description, TimeSpan Cooldown, CommandPermission Permission);

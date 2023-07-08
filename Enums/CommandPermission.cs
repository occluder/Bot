using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Enums;
internal enum CommandPermission
{
    None,
    Everyone,
    Subscribers,
    VIPs,
    Moderators,
    Whitelisted
}

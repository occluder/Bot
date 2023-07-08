using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Interfaces;

internal interface IModule
{
    public bool Enabled { get; }
    public ValueTask Enable();
    public ValueTask Disable();
}

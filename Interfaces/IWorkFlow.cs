using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Interfaces;

public interface IWorkflow
{
    public ValueTask<WorkflowState> Run();
}
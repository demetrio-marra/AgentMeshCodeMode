using AgentMesh.Models.AgentMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentMesh.Services
{
    public interface IAgentMemoryExecutor
    {

        Task<AgentMemoryExecutorSearchMemoryOutput> SearchMemoryAsync(AgentMemoryExecutorSearchMemoryInput input);


        Task AddInteractionAsync(AgentMemoryExecutorAddInteractionInput input);
    }
}

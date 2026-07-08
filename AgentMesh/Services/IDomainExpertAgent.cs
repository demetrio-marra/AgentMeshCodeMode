using AgentMesh.Models.DomainExpert;

namespace AgentMesh.Services
{
    public interface IDomainExpertAgent : IExecutor<DomainExpertAgentInput, DomainExpertAgentOutput>
    {
    }
}

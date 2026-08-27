using AgentMesh.Models;

namespace AgentMesh.Services
{
    public class OmittedValueEWParameterSerializer : IEWParameterSerializer
    {
        public string Serialize<T>(T? obj)
        {
            if (obj == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            if (obj is string strValue
                && string.IsNullOrWhiteSpace(strValue))
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return "(*** Omitted for brevity ***)";
        }
    }
}

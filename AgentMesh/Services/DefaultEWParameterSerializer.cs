using AgentMesh.Models.Workflows;
using AgentMesh.Utils;
using System.Globalization;
using System.Text.Json;

namespace AgentMesh.Services
{
    public class DefaultEWParameterSerializer : IEWParameterSerializer
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

            if (TypesUtils.IsBuiltInType(obj.GetType()))
            {
                return obj switch
                {
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => obj.ToString()! // string, bool, char, Guid, Uri — no culture-sensitive formatting
                };
            }

            // other structured types
            return JsonSerializer.Serialize(obj, SerializationUtils.DefaultSerializeOptions);
        }
    }
}

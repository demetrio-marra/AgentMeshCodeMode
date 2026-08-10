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
                    DateTime dateTime => dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"), // ISO 8601 format
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => obj.ToString()! // string, bool, char, Guid, Uri — no culture-sensitive formatting
                };
            }

            // if is any kind of list, collection, array, enumerable and it is empty, return the placeholder
            if (obj is System.Collections.IEnumerable enumerable
                && !enumerable.GetEnumerator().MoveNext())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            // other structured types
            return JsonSerializer.Serialize(obj, SerializationUtils.DefaultSerializeOptions);
        }
    }
}

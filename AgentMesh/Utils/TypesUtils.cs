namespace AgentMesh.Utils
{
    public class TypesUtils
    {
        public static bool IsBuiltInType(Type type)
        {
            // Handle Nullable<T>
            if (Nullable.GetUnderlyingType(type) is Type underlying)
                type = underlying;

            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || type == typeof(Uri);
        }
    }
}

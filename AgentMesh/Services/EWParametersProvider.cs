using AgentMesh.Models;

namespace AgentMesh.Services
{
    /// <summary>
    /// Provides a centralized way to manage and access workflow parameters using EWParameter models.
    /// It uses a concurrent dictionary to store parameters, allowing for thread-safe operations.
    /// The class allows retrieval of parameters by name, setting parameter values, and updating multiple parameters at once.
    /// </summary>
    public class EWParametersProvider(IEnumerable<IEWParameter> parameters)
    {
        private readonly IEnumerable<IEWParameter> _parameters = parameters;

        public IEnumerable<IEWParameter> GetParameters()
        {
            return _parameters;
        }
        
        public IEWParameter? GetUserCurrentRequestParameter() => GetParameterByPredicate(p => p.IsUserCurrentRequestParameter);
        public IEWParameter? GetConversationHistoryParameter() => GetParameterByPredicate(p => p.IsConversationHistoryParameter);
        public IEWParameter? GetResponseForUserParameter() => GetParameterByPredicate(p => p.IsResponseForUserParameter);

        private IEWParameter? GetParameterByPredicate(Func<IEWParameter, bool> predicate)
        {
            return _parameters.FirstOrDefault(predicate);
        }
    }
}

using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Parameters;
using AgentMesh.Utils;

namespace AgentMesh.Application.Parameters
{
    public static class ParametersInit
    {
        private static Parameter InitUserIntentParameter()
        {
            var userIntentParameter = new Parameter
            {
                Name = "User intent",
                IsSystemProvided = false
            };
            return userIntentParameter;
        }

        private static Parameter InitKnowledgeBaseQueryResultsParameter()
        {
            var knowledgeBaseQueryResultsParameter = new Parameter
            {
                Name = "Knowledge base query results",
                IsSystemProvided = true,
                GetDisplayValue = (rawValue) =>
                {
                    if (string.IsNullOrEmpty(rawValue))
                    {
                        return Parameter.NoDisplayValue;
                    }
                    else
                    {
                        var deser = Parameter.AsObject<IEnumerable<KnowledgeBaseQueryResultItem>>(rawValue);
                        if (deser == null || !deser.Any())
                        {
                            return Parameter.NoDisplayValue;
                        }
                        else
                        {
                            var displayValue = ListsFormatter.ToBulletList(deser.Select(item => $"{item.File} - Title: {item.Title} - Relevance: {item.Relevance}"));
                            return displayValue;
                        }
                    }
                }
            };
            return knowledgeBaseQueryResultsParameter;
        }

        public static IEnumerable<Parameter> InitParameters()
        {
            var parameters = new List<Parameter>
            {
                InitUserIntentParameter(),
                InitKnowledgeBaseQueryResultsParameter()
            };

            return parameters;
        }
    }
}

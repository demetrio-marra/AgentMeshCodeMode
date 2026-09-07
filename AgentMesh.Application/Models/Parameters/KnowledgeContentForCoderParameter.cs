using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
using AgentMesh.Services;
using AgentMesh.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeContentForCoderParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeContentItem>>
    {
        public override string Name => "Knowledge content for Coder Agent";

        private static readonly Regex FrontmatterRegex = new Regex(
    @"^\s*---\s*\r?\n.*?\r?\n---\s*\r?\n?",
    RegexOptions.Singleline | RegexOptions.Compiled
);

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
        public override IEWParameterSerializer ValueSerializer => _serializer;

        private readonly IEWParameterSerializer _serializer = new KnowledgeContentForCoderParameterSerializer();

        private class KnowledgeContentForCoderParameterSerializer : IEWParameterSerializer
        {
            public string Serialize<T>(T obj)
            {
                if (obj is null)
                {
                    return EWParameterConstants.NoDataPlaceholder;
                }

                if (!(obj is IEnumerable<KnowledgeContentItem> knowledgeContentItems))
                {
                    throw new ArgumentException($"Expected an object of type {typeof(IEnumerable<KnowledgeContentItem>).FullName}, but received an object of type {obj.GetType().FullName}.");
                }

                var ret = knowledgeContentItems.Select(c => new InternalDto
                {
                    Source = c.Source,
                    Content = FrontmatterRegex.Replace(c.Content, string.Empty)
                }).ToList();

                return JsonSerializer.Serialize(ret, SerializationUtils.DefaultSerializeOptions);
            }

            private class InternalDto
            {
                public string Source { get; set; } = string.Empty;
                public string Content { get; set; } = string.Empty;
            }
        }
    }
}

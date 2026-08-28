using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
using AgentMesh.Services;
using AgentMesh.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeQueryResultParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<KnowledgeQueryResult>
    {
        public override string Name => "Knowledge query result";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;

        public override IEWParameterSerializer ValueSerializer => valueSerializer;

        private static readonly IEWParameterSerializer valueSerializer = new KnowledgeQueryResultParameterSerializer();


        private class KnowledgeQueryResultParameterSerializer : IEWParameterSerializer
        {

            public string Serialize<T>(T obj)
            {
                if (obj is not KnowledgeQueryResult knowledgeQueryResult)
                {
                    throw new InvalidOperationException($"Cannot serialize object of type {typeof(T).FullName} as KnowledgeQueryResult.");
                }

                var ret = new
                {
                    Contents = knowledgeQueryResult.Contents.Select(c => new
                    {
                        c.Id,
                        c.Source,
                        c.Content
                    }),
                    Entities = knowledgeQueryResult.Entities.Select(e => new
                    {
                        e.Entity,
                        e.Type,                        
                        e.Description,
                        e.ContentItem.Source
                    }),
                    Relations = knowledgeQueryResult.Relations.Select(r => new
                    {
                        r.EntityRelationFrom,
                        r.Keywords,
                        r.EntityRelationTo,
                        r.ContentItem.Source
                    })
                };

                return JsonSerializer.Serialize(ret, SerializationUtils.DefaultSerializeOptions);
            }
        }
    }
}

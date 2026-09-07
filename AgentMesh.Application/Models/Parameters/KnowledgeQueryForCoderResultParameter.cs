using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
using AgentMesh.Services;
using AgentMesh.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeQueryForCoderResultParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<KnowledgeQueryResult>
    {
        public override string Name => "KnowledgeForCoder query result";

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
                        c.Content,
                        c.Id,
                        c.Source
                    }),
                    Entities = knowledgeQueryResult.Entities.Select(e => new
                    {
                        e.Id,
                        e.Entity,
                        e.Type,                        
                        e.Description,
                        e.ContentItem.Source
                    }).ToList(),
                    Relations = knowledgeQueryResult.Relations.Select(r => new
                    {
                        r.Id,
                        r.EntityRelationFrom,
                        r.Keywords,
                        r.EntityRelationTo,
                        r.ContentItem.Source
                    }).ToList()
                };

                return JsonSerializer.Serialize(ret, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
                });
            }
        }
    }
}

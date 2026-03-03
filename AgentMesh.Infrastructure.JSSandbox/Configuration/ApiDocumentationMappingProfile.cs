using AgentMesh.Infrastructure.JSSandbox.Entities;
using AgentMesh.Models;
using AutoMapper;

namespace AgentMesh.Infrastructure.JSSandbox.Configuration
{
    /// <summary>
    /// AutoMapper profile for mapping between ApiDocumentation domain model and ApiDocumentationEntity.
    /// </summary>
    public class ApiDocumentationMappingProfile : Profile
    {
        public ApiDocumentationMappingProfile()
        {
            CreateMap<ApiDocumentationEntity, ApiDocumentation>()
                .ReverseMap();
        }
    }
}

using AgentMesh.Application.Models;
using AgentMesh.Infrastructure.Persistence.Entities;
using AutoMapper;

namespace AgentMesh.Infrastructure.Persistence.Configuration
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

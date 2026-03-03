using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgentMesh.Infrastructure.JSSandbox.Entities
{
    /// <summary>
    /// MongoDB entity representing API documentation.
    /// </summary>
    public class ApiDocumentationEntity
    {
        /// <summary>
        /// MongoDB document identifier.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The unique name of the API for which this documentation fragment applies.
        /// </summary>
        [BsonElement("apiName")]
        public string ApiName { get; set; } = string.Empty;

        /// <summary>
        /// The technical Javascript documentation content for the specified API.
        /// </summary>
        [BsonElement("documentation")]
        public string Documentation { get; set; } = string.Empty;
    }
}

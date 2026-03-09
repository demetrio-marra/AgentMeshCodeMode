using AgentMesh.Application.Contracts;
using AgentMesh.Infrastructure.Persistence.Configuration;
using AgentMesh.Infrastructure.Persistence.Entities;
using AgentMesh.Models;
using AutoMapper;
using MongoDB.Driver;

namespace AgentMesh.Infrastructure.Persistence.Services
{
    /// <summary>
    /// MongoDB implementation of the IApiDocumentationService interface.
    /// </summary>
    public class MongoApiDocumentationRepository : IApiDocumentationService
    {
        private readonly IMongoCollection<ApiDocumentationEntity> _collection;
        private readonly IMapper _mapper;
        private static readonly SemaphoreSlim _initializationLock = new(1, 1);
        private static bool _isInitialized;

        public MongoApiDocumentationRepository(
            ApiDocumentationMongoRepositoryConfiguration configuration,
            IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
                throw new ArgumentException("ConnectionString cannot be null or empty.", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.DatabaseName))
                throw new ArgumentException("DatabaseName cannot be null or empty.", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.CollectionName))
                throw new ArgumentException("CollectionName cannot be null or empty.", nameof(configuration));

            var client = new MongoClient(configuration.ConnectionString);
            var database = client.GetDatabase(configuration.DatabaseName);
            _collection = database.GetCollection<ApiDocumentationEntity>(configuration.CollectionName);

            EnsureCollectionAndIndexAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures the collection exists and creates a unique index on the ApiName field.
        /// Uses a lock to prevent concurrent initialization.
        /// </summary>
        private async Task EnsureCollectionAndIndexAsync()
        {
            if (_isInitialized)
                return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                var database = _collection.Database;
                var collectionName = _collection.CollectionNamespace.CollectionName;

                var collectionsCursor = await database.ListCollectionNamesAsync();
                var collectionList = await collectionsCursor.ToListAsync();
                var collectionExists = collectionList.Contains(collectionName);

                if (!collectionExists)
                {
                    await database.CreateCollectionAsync(collectionName);
                }

                var indexKeys = Builders<ApiDocumentationEntity>.IndexKeys.Ascending(x => x.ApiName);
                var indexOptions = new CreateIndexOptions { Unique = true };
                var indexModel = new CreateIndexModel<ApiDocumentationEntity>(indexKeys, indexOptions);

                var existingIndexesCursor = await _collection.Indexes.ListAsync();
                var indexList = await existingIndexesCursor.ToListAsync();

                var apiNameIndexExists = indexList.Any(index =>
                {
                    var keysDoc = index["key"].AsBsonDocument;
                    return keysDoc.Contains("apiName");
                });

                if (!apiNameIndexExists)
                {
                    await _collection.Indexes.CreateOneAsync(indexModel);
                }

                _isInitialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// Retrieves the technical Javascript documentation for a specified API.
        /// </summary>
        /// <param name="apiName">The name of the API for which to retrieve documentation.</param>
        /// <returns>An ApiDocumentation object containing the technical Javascript documentation for the specified API.</returns>
        /// <exception cref="ArgumentException">Thrown when apiName is null or whitespace.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no documentation is found for the specified API name.</exception>
        public async Task<ApiDocumentation> GetApiDocumentationAsync(string apiName)
        {
            if (string.IsNullOrWhiteSpace(apiName))
                throw new ArgumentException("API name cannot be null or whitespace.", nameof(apiName));

            var filter = Builders<ApiDocumentationEntity>.Filter.Eq(x => x.ApiName, apiName);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync();

            if (entity == null)
                throw new KeyNotFoundException($"API documentation not found for API: {apiName}");

            return _mapper.Map<ApiDocumentation>(entity);
        }

        /// <summary>
        /// Retrieves multiple API documentation for the specified API names asynchronously.
        /// </summary>
        /// <param name="apiNames">A collection of API names for which to retrieve documentation.</param>
        /// <returns>A collection of ApiDocumentation objects for the specified API names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when apiNames is null.</exception>
        public async Task<IEnumerable<ApiDocumentation>> GetApiDocumentationAsync(IEnumerable<string> apiNames)
        {
            if (apiNames == null)
                throw new ArgumentNullException(nameof(apiNames));

            var apiNamesList = apiNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

            if (!apiNamesList.Any())
                return Enumerable.Empty<ApiDocumentation>();

            var filter = Builders<ApiDocumentationEntity>.Filter.In(x => x.ApiName, apiNamesList);
            var entities = await _collection.Find(filter).ToListAsync();

            return entities.Select(entity => _mapper.Map<ApiDocumentation>(entity));
        }
    }
}

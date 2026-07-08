using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentMesh.Application;
using AgentMesh.Infrastructure.QMD.Configuration;
using AgentMesh.Infrastructure.QMD.DTOs.Get;
using AgentMesh.Infrastructure.QMD.DTOs.JsonRpc;
using AgentMesh.Infrastructure.QMD.DTOs.MultiGet;
using AgentMesh.Infrastructure.QMD.DTOs.Query;
using AgentMesh.Infrastructure.QMD.DTOs.Status;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Infrastructure.QMD
{
    /// <summary>
    /// REST client that proxies the QMD MCP server tools (<c>query</c>, <c>get</c>,
    /// <c>multi_get</c>, <c>status</c>) over the Streamable HTTP transport (JSON-RPC 2.0).
    /// The MCP handshake (<c>initialize</c> + <c>notifications/initialized</c>) is performed
    /// lazily on the first tool call and any <c>Mcp-Session-Id</c> issued by the server is
    /// then echoed back on every subsequent request.
    /// </summary>
    public class QMDHttpProxy
    {
        private const string MimeJson = "application/json";
        private const string MimeEventStream = "text/event-stream";
        private const string SessionIdHeader = "Mcp-Session-Id";
        private const string ProtocolVersionHeader = "MCP-Protocol-Version";

        private readonly HttpClient _httpClient;
        private readonly QMDHttpProxyConfiguration _configuration;
        private readonly ILogger<QMDHttpProxy> _logger;
        private readonly Resilience _resilience;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _handshakeLock = new(1, 1);

        private string? _sessionId;
        private bool _initialized;
        private long _requestId;

        public QMDHttpProxy(
            HttpClient httpClient,
            QMDHttpProxyConfiguration configuration,
            ILogger<QMDHttpProxy> logger,
            Resilience resilience)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _resilience = resilience;

            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl, UriKind.Absolute);
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Invokes the MCP <c>query</c> tool.
        /// </summary>
        public async Task<QueryToolResponse> QueryAsync(QueryToolRequest request, CancellationToken cancellationToken = default)
        {
            request.Searches.ForEach(search =>
            {
                if (search.Type == "hyde")
                {
                    search.Query = ReplaceNewLinesWithSpaces(search.Query);
                }
            });

            return await CallToolAsync<QueryToolRequest, QueryToolResponse>("query", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes the MCP <c>get</c> tool.
        /// </summary>
        public async Task<GetToolResponse?> GetAsync(GetToolRequest request, CancellationToken cancellationToken = default)
        {
            var tol = await CallToolAsync<GetToolRequest, List<MultiGetToolResponseItem>>("get", request, cancellationToken);
            if (tol != null && tol.Count > 0)
            {
                var firstItem = tol[0];
                return new GetToolResponse
                {
                    Uri = firstItem.Uri.Replace("qmd://", string.Empty, StringComparison.OrdinalIgnoreCase),
                    MimeType = firstItem.MimeType,
                    Text = firstItem.Text
                };
            }
            return null;
        }

        /// <summary>
        /// Invokes the MCP <c>multi_get</c> tool.
        /// </summary>
        public async Task<MultiGetToolResponse> MultiGetAsync(MultiGetToolRequest request, CancellationToken cancellationToken = default)
        {
            var tol = await CallToolAsync<MultiGetToolRequest, List<MultiGetToolResponseItem>>("multi_get", request, cancellationToken);
            var retol = tol.Select(item => new MultiGetToolResponseItem
            {
                Uri = item.Uri.Replace("qmd://", string.Empty, StringComparison.OrdinalIgnoreCase),
                MimeType = item.MimeType,
                Text = item.Text
            }).ToList();
            return new MultiGetToolResponse
            {
                Files = retol
            };
        }

        /// <summary>
        /// Invokes the MCP <c>status</c> tool.
        /// </summary>
        public Task<StatusToolResponse> StatusAsync(CancellationToken cancellationToken = default)
            => CallToolAsync<StatusToolRequest, StatusToolResponse>("status", new StatusToolRequest(), cancellationToken);

        private async Task<TResponse> CallToolAsync<TRequest, TResponse>(
            string toolName,
            TRequest arguments,
            CancellationToken cancellationToken)
            where TResponse : new()
        {
            await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            var rpcRequest = new JsonRpcRequest<ToolCallParams<TRequest>>
            {
                Id = NextId(),
                Method = "tools/call",
                Params = new ToolCallParams<TRequest>
                {
                    Name = toolName,
                    Arguments = arguments
                }
            };

            var rpcResponse = await SendRpcAsync<ToolCallParams<TRequest>, ToolCallResult>(rpcRequest, cancellationToken)
                .ConfigureAwait(false);

            if (rpcResponse.Result is null)
            {
                throw new InvalidOperationException($"MCP tool '{toolName}' returned no result.");
            }

            if (rpcResponse.Result.IsError == true)
            {
                var errorText = ExtractFirstText(rpcResponse.Result) ?? "<no error text>";
                throw new InvalidOperationException($"MCP tool '{toolName}' reported an error: {errorText}");
            }

            return DeserializeToolPayload<TResponse>(rpcResponse.Result, toolName);
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_initialized)
            {
                return;
            }

            await _handshakeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                var initRequest = new JsonRpcRequest<InitializeParams>
                {
                    Id = NextId(),
                    Method = "initialize",
                    Params = new InitializeParams
                    {
                        ProtocolVersion = _configuration.ProtocolVersion,
                        Capabilities = JsonDocument.Parse("{}").RootElement.Clone(),
                        ClientInfo = new ClientInfo
                        {
                            Name = _configuration.ClientName,
                            Version = _configuration.ClientVersion
                        }
                    }
                };

                var initResponse = await SendRpcAsync<InitializeParams, InitializeResult>(initRequest, cancellationToken)
                    .ConfigureAwait(false);

                if (initResponse.Result is null)
                {
                    throw new InvalidOperationException("MCP 'initialize' returned no result.");
                }

                _logger.LogInformation(
                    "MCP handshake complete with {BaseUrl} (server protocol: {ProtocolVersion}).",
                    _configuration.BaseUrl,
                    initResponse.Result.ProtocolVersion ?? "<unknown>");

                // Per spec, follow up with the initialized notification (no id, no response expected).
                var initialized = new JsonRpcRequest<object>
                {
                    Id = null,
                    Method = "notifications/initialized",
                    Params = null
                };

                await SendNotificationAsync(initialized, cancellationToken).ConfigureAwait(false);
                _initialized = true;
            }
            finally
            {
                _handshakeLock.Release();
            }
        }

        private async Task<JsonRpcResponse<TResult>> SendRpcAsync<TParams, TResult>(
            JsonRpcRequest<TParams> request,
            CancellationToken cancellationToken)
        {
            using var httpResponse = await _resilience.SendWithRetryAsync(
                async () =>
                {
                    using var httpRequest = BuildHttpRequest(request);
                    return await _httpClient.SendAsync(
                        httpRequest,
                        HttpCompletionOption.ResponseContentRead,
                        cancellationToken).ConfigureAwait(false);
                },
                $"MCP request '{request.Method}'",
                _logger).ConfigureAwait(false);

            CaptureSessionId(httpResponse);

            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"MCP request '{request.Method}' failed with status {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
            }

            var json = ExtractJsonPayload(httpResponse, body);
            var rpcResponse = JsonSerializer.Deserialize<JsonRpcResponse<TResult>>(json, _jsonOptions)
                ?? throw new InvalidOperationException($"MCP request '{request.Method}' returned an empty body.");

            if (rpcResponse.Error is not null)
            {
                throw new InvalidOperationException(
                    $"MCP request '{request.Method}' returned JSON-RPC error {rpcResponse.Error.Code}: {rpcResponse.Error.Message}");
            }

            return rpcResponse;
        }

        private async Task SendNotificationAsync<TParams>(
            JsonRpcRequest<TParams> notification,
            CancellationToken cancellationToken)
        {
            using var httpResponse = await _resilience.SendWithRetryAsync(
                async () =>
                {
                    using var httpRequest = BuildHttpRequest(notification);
                    return await _httpClient.SendAsync(
                        httpRequest,
                        HttpCompletionOption.ResponseContentRead,
                        cancellationToken).ConfigureAwait(false);
                },
                $"MCP notification '{notification.Method}'",
                _logger).ConfigureAwait(false);

            CaptureSessionId(httpResponse);

            // Notifications may legitimately return 202 Accepted with empty body — only fail on hard errors.
            if (!httpResponse.IsSuccessStatusCode)
            {
                var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"MCP notification '{notification.Method}' failed with status {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
            }
        }

        private HttpRequestMessage BuildHttpRequest<TParams>(JsonRpcRequest<TParams> request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _httpClient.BaseAddress)
            {
                Content = new StringContent(json, Encoding.UTF8, MimeJson)
            };

            // Streamable HTTP transport: the server may reply with JSON or SSE — accept both.
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeJson));
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeEventStream));

            if (!string.IsNullOrEmpty(_sessionId))
            {
                httpRequest.Headers.TryAddWithoutValidation(SessionIdHeader, _sessionId);
            }

            if (_initialized && !string.IsNullOrEmpty(_configuration.ProtocolVersion))
            {
                httpRequest.Headers.TryAddWithoutValidation(ProtocolVersionHeader, _configuration.ProtocolVersion);
            }

            return httpRequest;
        }

        private void CaptureSessionId(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues(SessionIdHeader, out var values))
            {
                var value = values.FirstOrDefault();
                if (!string.IsNullOrEmpty(value))
                {
                    _sessionId = value;
                }
            }
        }

        private static string ExtractJsonPayload(HttpResponseMessage response, string body)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.Equals(contentType, MimeEventStream, StringComparison.OrdinalIgnoreCase))
            {
                // Streamable HTTP SSE frame format: lines starting with "data: " carry the JSON payload.
                var buffer = new StringBuilder();
                foreach (var line in body.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (trimmed.StartsWith("data:", StringComparison.Ordinal))
                    {
                        buffer.Append(trimmed.AsSpan(5).TrimStart());
                    }
                    else if (trimmed.Length == 0 && buffer.Length > 0)
                    {
                        // First complete event is enough — JSON-RPC responses are single-message.
                        break;
                    }
                }

                if (buffer.Length == 0)
                {
                    throw new InvalidOperationException("MCP SSE response did not contain a 'data:' frame.");
                }

                return buffer.ToString();
            }

            return body;
        }

        private TResponse DeserializeToolPayload<TResponse>(ToolCallResult toolResult, string toolName)
            where TResponse : new()
        {
            // Prefer structuredContent when the server provides it.
            if (toolResult.StructuredContent.HasValue
                && toolResult.StructuredContent.Value.ValueKind != JsonValueKind.Undefined
                && toolResult.StructuredContent.Value.ValueKind != JsonValueKind.Null)
            {
                var fromStructured = toolResult.StructuredContent.Value.Deserialize<TResponse>(_jsonOptions);
                if (fromStructured is not null)
                {
                    return fromStructured;
                }
            }

            if (toolResult.Content != null && toolResult.Content.Any())
            {
                var parsedContent = ParseToolContentItem(toolResult.Content ?? Enumerable.Empty<ToolContentItem>());
                if (!string.IsNullOrEmpty(parsedContent))
                {
                    try
                    {
                        var fromContent = JsonSerializer.Deserialize<TResponse>(parsedContent, _jsonOptions);
                        if (fromContent is not null)
                        {
                            return fromContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"MCP tool '{toolName}' returned content that could not be deserialized into the expected response type. Content: {parsedContent}", ex);
                    }
                }
            }

            throw new Exception($"MCP tool '{toolName}' returned content that could not be deserialized into the expected response type.");
        }

        private static string? ParseToolContentItem(IEnumerable<ToolContentItem> items)
        {
            var listOfItems = new List<string>();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Text))
                {
                    listOfItems.Add(item.Text);
                }
                else if (item.Extensions != null
                    && item.Extensions.ContainsKey("resource")
                    && item.Extensions["resource"].ValueKind == JsonValueKind.Object)
                {
                    var resource = item.Extensions["resource"];
                    listOfItems.Add(resource.GetRawText());
                }
            }

            if (listOfItems.Count == 0)
            {
                return null;
            }

            return "[" + string.Join(",", listOfItems) + "]";
        }


        private static string? ExtractFirstText(ToolCallResult result)
        {
            var listOfItems = new List<string>();
            if (result.Content is null)
            {
                return null;
            }

            foreach (var item in result.Content)
            {
                if (item.Type == "resource")
                {
                    if (item.Extensions != null
                        && item.Extensions.ContainsKey("resource")
                        && item.Extensions["resource"].ValueKind == JsonValueKind.Object)
                    {
                        var resource = item.Extensions["resource"];
                        if (resource.TryGetProperty("text", out var textProp) == true)
                        {
                            var val = textProp.GetString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                listOfItems.Add(val);
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(item.Text))
                {
                    listOfItems.Add(item.Text);
                }
            }

            return "[" + string.Join(", ", listOfItems) + "]";
        }

        private static string ReplaceNewLinesWithSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.Replace(Environment.NewLine, " ", StringComparison.OrdinalIgnoreCase)
                        .Replace("\n", " ", StringComparison.OrdinalIgnoreCase)
                        .Replace("\r", " ", StringComparison.OrdinalIgnoreCase);
        }

        private string NextId() => Interlocked.Increment(ref _requestId).ToString();
    }
}

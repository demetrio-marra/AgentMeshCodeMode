namespace AgentMesh.Infrastructure.SemanticSearch.Configuration
{
    /// <summary>
    /// Configuration for the <see cref="QMDHttpProxy"/> REST client that talks to the QMD MCP server
    /// over the Streamable HTTP transport (JSON-RPC 2.0).
    /// </summary>
    public class QMDHttpProxyConfiguration
    {
        public const string SectionName = "QMDHttpProxy";

        /// <summary>
        /// Absolute URL of the MCP endpoint (e.g. <c>http://dem-ubuntu-8g:8888/mcp</c>).
        /// Every JSON-RPC request is POSTed to this single URL.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// HTTP request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// MCP protocol version advertised during the <c>initialize</c> handshake.
        /// </summary>
        public string ProtocolVersion { get; set; } = "2025-03-26";

        /// <summary>
        /// Client name advertised to the MCP server during the <c>initialize</c> handshake.
        /// </summary>
        public string ClientName { get; set; } = "AgentMesh.QMDHttpProxy";

        /// <summary>
        /// Client version advertised to the MCP server during the <c>initialize</c> handshake.
        /// </summary>
        public string ClientVersion { get; set; } = "1.0.0";
    }
}

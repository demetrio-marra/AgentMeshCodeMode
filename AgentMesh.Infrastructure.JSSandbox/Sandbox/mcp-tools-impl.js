/**
 * MCP Tools Implementation File (JavaScript)
 * Generated: 2025-12-03T14:26:32.736Z
 * Server: http://localhost:59000
 * 
 * This file contains real implementations that call MCP tools via Streamable HTTP.
 * Import and use these functions to interact with the MCP server.
 */

const { Client } = require('@modelcontextprotocol/sdk/client/index.js');
const { StreamableHTTPClientTransport } = require('@modelcontextprotocol/sdk/client/streamableHttp.js');

/**
 * MCP Client Manager
 * Handles connection and tool execution
 */
class MCPClient {
    constructor(serverUrl, agentId = null) {
        this.serverUrl = serverUrl;
        this.agentId = agentId;
        this.client = null;
        this.connected = false;
    }

    /**
     * Set the agent ID for MCP requests
     * @param {string} agentId - The agent ID to use in x-agent-id header
     */
    setAgentId(agentId) {
        this.agentId = agentId;
        // Force reconnection with new agent ID
        this.connected = false;
    }

    /**
     * Connect to the MCP server
     */
    async connect() {
        if (this.connected) return;

        const options = {};
        if (this.agentId) {
            options.requestInit = {
                headers: {
                    'x-agent-id': this.agentId
                }
            };
        }

        const transport = new StreamableHTTPClientTransport(
            new URL(this.serverUrl),
            options
        );

        this.client = new Client({
            name: 'mcp-tool-client',
            version: '1.0.0'
        }, {
            capabilities: {}
        });

        await this.client.connect(transport);
        this.connected = true;
    }

    /**
     * Ensure connection before executing tools
     */
    async ensureConnected() {
        if (!this.connected) {
            await this.connect();
        }
    }

    /**
     * Call a tool on the MCP server
     * @param {string} toolName - Name of the tool to call
     * @param {Object} params - Parameters for the tool
     * @returns {Promise<Object>} Structured content from MCP response or error object
     */
    async callTool(toolName, params = {}) {
        await this.ensureConnected();

        if (!this.client) {
            throw new Error('Client not initialized');
        }

        const response = await this.client.callTool({
            name: toolName,
            arguments: params
        });

        // Check if the response indicates an error
        if (response.isError) {
            // Extract error message from content
            const errorMessage = response.content && response.content[0]
                ? response.content[0].text
                : 'Unknown error occurred';

            return {
                isError: true,
                error: errorMessage
            };
        }

        // Return the deserialized structured content
        return response.structuredContent || response.content;
    }

    /**
     * Close the connection
     */
    async close() {
        if (this.client && this.connected) {
            await this.client.close();
            this.connected = false;
        }
    }
}

// Create singleton instance
// You can set agent ID later using: mcpClient.setAgentId('your-agent-id')
const serverUrl = process.env.MCP_SERVER_URL || 'http://localhost:8080';
const mcpClient = new MCPClient(serverUrl);

/**
 * @tool MyPlatform_Statistics_GetRates
 * @description Get provisioning processes statistics rates for a specific product using Company, Family, Product and ProvisioningPhase filters. Supports data partitioning.
 * @inputSchema MyPlatformStatisticsGetRatesParams
 * @outputSchema MCPToolResponse<MyPlatformStatisticsGetRatesResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_Statistics_GetRates(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_Statistics_GetRates', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_Statistics_GetRates:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyCompany_CompanyInfo_GetAllProductNames
 * @description CompanyInfo - Retrieves all available product names.
 * @inputSchema MyCompanyCompanyInfoGetAllProductNamesParams
 * @outputSchema MCPToolResponse<MyCompanyCompanyInfoGetAllProductNamesResult>
 * @errorSchema MCPToolError
 */
async function MyCompany_CompanyInfo_GetAllProductNames(params = {}) {
    try {
        return await mcpClient.callTool('MyCompany_CompanyInfo_GetAllProductNames', params);
    } catch (error) {
        console.error('Error calling tool MyCompany_CompanyInfo_GetAllProductNames:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyPlatform_ProvisioningInfo
 * @description Get provisioning information. Returns a list of provisioning information matching the search criteria. If only one result is found, it includes detailed GsaContract and Queue information.
 * @inputSchema MyPlatformProvisioningInfoParams
 * @outputSchema MCPToolResponse<MyPlatformProvisioningInfoResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_ProvisioningInfo(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_ProvisioningInfo', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_ProvisioningInfo:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyPlatform_Chart_GenerateChart
 * @description Generates a visual chart (bar, line, or pie) from data, uploads it to an external file server service and returns a reference to the uploaded file for later use. Use this tool when you need to create visual representations of data for analysis or presentation. Bar charts are ideal for comparing discrete categories, line charts for showing trends or time series, and pie charts for displaying proportional distributions where all parts sum to a meaningful whole.
 * @inputSchema MyPlatformChartGenerateChartParams
 * @outputSchema MCPToolResponse<MyPlatformChartGenerateChartResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_Chart_GenerateChart(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_Chart_GenerateChart', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_Chart_GenerateChart:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyPlatform_ProvisioningInfo_GetById
 * @description Get provisioning information by RequestRegister ID. Returns detailed provisioning information including RequestRegister, GsaContract, and Queue data.
 * @inputSchema MyPlatformProvisioningInfoGetByIdParams
 * @outputSchema MCPToolResponse<MyPlatformProvisioningInfoGetByIdResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_ProvisioningInfo_GetById(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_ProvisioningInfo_GetById', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_ProvisioningInfo_GetById:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyPlatform_Statistics_Get
 * @description Get provisioning processes statistics for a specific product using Company, Family, Product and ProvisioningPhase filters. Supports data partitioning.
 * @inputSchema MyPlatformStatisticsGetParams
 * @outputSchema MCPToolResponse<MyPlatformStatisticsGetResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_Statistics_Get(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_Statistics_Get', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_Statistics_Get:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyPlatform_MyPermissions_Get
 * @description Get the current user's permissions (ACLs). Returns a list of access control permissions including Company, Family, and Product access rights.
 * @inputSchema MyPlatformMyPermissionsGetParams
 * @outputSchema MCPToolResponse<MyPlatformMyPermissionsGetResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_MyPermissions_Get(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_MyPermissions_Get', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_MyPermissions_Get:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyCompany_CompanyInfo_GetProductsHierarchy
 * @description CompanyInfo - Retrieves the Company/Family/Product flattened map. Omit filters to get the complete map.
 * @inputSchema MyCompanyCompanyInfoGetProductsHierarchyParams
 * @outputSchema MCPToolResponse<MyCompanyCompanyInfoGetProductsHierarchyResult>
 * @errorSchema MCPToolError
 */
async function MyCompany_CompanyInfo_GetProductsHierarchy(params = {}) {
    try {
        return await mcpClient.callTool('MyCompany_CompanyInfo_GetProductsHierarchy', params);
    } catch (error) {
        console.error('Error calling tool MyCompany_CompanyInfo_GetProductsHierarchy:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyCompany_CompanyInfo_FindProductHierarchy
 * @description CompanyInfo - Finds which Company and Family a specific Product belongs to.
 * @inputSchema MyCompanyCompanyInfoFindProductHierarchyParams
 * @outputSchema MCPToolResponse<MyCompanyCompanyInfoFindProductHierarchyResult>
 * @errorSchema MCPToolError
 */
async function MyCompany_CompanyInfo_FindProductHierarchy(params = {}) {
    try {
        return await mcpClient.callTool('MyCompany_CompanyInfo_FindProductHierarchy', params);
    } catch (error) {
        console.error('Error calling tool MyCompany_CompanyInfo_FindProductHierarchy:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

/**
 * @tool MyPlatform_Statistics_GetAverageDuration
 * @description Get provisioning processes average duration in seconds for a specific product using Company, Family, Product and ProvisioningPhase filters. Supports data partitioning.
 * @inputSchema MyPlatformStatisticsGetAverageDurationParams
 * @outputSchema MCPToolResponse<MyPlatformStatisticsGetAverageDurationResult>
 * @errorSchema MCPToolError
 */
async function MyPlatform_Statistics_GetAverageDuration(params = {}) {
    try {
        return await mcpClient.callTool('MyPlatform_Statistics_GetAverageDuration', params);
    } catch (error) {
        console.error('Error calling tool MyPlatform_Statistics_GetAverageDuration:', error.message);
        return {
            isError: true,
            error: error.message
        };
    }
}

// Export the client and all tool functions
module.exports = {
    mcpClient,
    MyPlatform_Statistics_GetRates,
    MyCompany_CompanyInfo_GetAllProductNames,
    MyPlatform_ProvisioningInfo,
    MyPlatform_Chart_GenerateChart,
    MyPlatform_ProvisioningInfo_GetById,
    MyPlatform_Statistics_Get,
    MyPlatform_MyPermissions_Get,
    MyCompany_CompanyInfo_GetProductsHierarchy,
    MyCompany_CompanyInfo_FindProductHierarchy,
    MyPlatform_Statistics_GetAverageDuration
};

/**
 * Usage Example:
 * 
 * const tools = require('./mcp-tools-impl.js');
 * 
 * // Example 1: Basic usage with error handling
 * async function example1() {
 *   const result = await tools.MyCompany_CompanyInfo_GetAllProductNames({});
 *   
 *   if (result.isError) {
 *     console.error('Tool error:', result.error);
 *     return;
 *   }
 *   
 *   // result is the deserialized structured content
 *   console.log('Product names:', result.result);
 * }
 * 
 * // Example 2: Multiple tool calls with error handling
 * async function example3() {
 *   const results = [];
 *   const products = ['Product1', 'Product2'];
 *   
 *   for (const product of products) {
 *     const result = await tools.MyPlatform_Statistics_Get({
 *       queryDateFrom: '2025-01-01',
 *       queryDateTo: '2025-12-31',
 *       product: product,
 *       dataPartitioning: 'Month'
 *     });
 *     
 *     if (result.isError) {
 *       console.warn('Skipping failed product:', product, result.error);
 *       continue;
 *     }
 *     
 *     results.push(result.result);
 *   }
 *   
 *   return results;
 * }
 */

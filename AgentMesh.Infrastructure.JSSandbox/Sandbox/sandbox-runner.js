/**
 * Hardened JavaScript (SES) Sandbox Runner for MCP Tools
 * 
 * This module provides a secure sandbox environment using Hardened JavaScript (SES)
 * to execute LLM-generated code with access to MCP tools.
 * 
 * Usage:
 *   const { SandboxRunner } = require('./sandbox-runner.js');
 *   const runner = new SandboxRunner();
 *   const result = await runner.execute('return await MyPlatform_Statistics_Get({...})');
 */

require('ses');
const mcpTools = require('./mcp-tools-impl.js');

// Global flag to track if SES has been initialized
let sesInitialized = false;

/**
 * Initialize the SES environment
 * This hardens JavaScript by freezing intrinsics and preventing prototype pollution
 */
function initializeSES() {
    if (sesInitialized) {
        return;
    }

    try {
        lockdown({
            errorTaming: 'unsafe', // Allow error stack traces for debugging
            stackFiltering: 'verbose', // Show full stack traces
            consoleTaming: 'unsafe', // Allow console.log
        });
        //console.debug('✓ SES lockdown initialized');
        sesInitialized = true;
    } catch (error) {
        if (error.message && error.message.includes('lockdown() already called')) {
            //console.debug('✓ SES already locked down');
            sesInitialized = true;
        } else {
            throw error;
        }
    }
}

/**
 * Sandbox Runner for executing LLM code with MCP tools
 */
class SandboxRunner {
    constructor(agentId = null, options = {}) {
        // Handle both old and new signature for backward compatibility
        if (typeof agentId === 'object' && agentId !== null) {
            // Old signature: new SandboxRunner(options)
            options = agentId;
            agentId = options.agentId || null;
        }

        this.agentId = agentId;
        this.options = {
            timeout: options.timeout || 30000, // 30 seconds default timeout
            enableConsole: options.enableConsole !== false,
            ...options
        };

        // Set agent ID for MCP client if provided
        if (this.agentId) {
            mcpTools.mcpClient.setAgentId(this.agentId);
        }

        // Initialize SES if not already done
        initializeSES();
    }

    /**
     * Create endowments (globals) available to sandboxed code
     */
    async createEndowments() {
        const endowments = {
            // Console for debugging (can be disabled)
            console: this.options.enableConsole ? console : {
                log: () => { },
                error: () => { },
                warn: () => { },
                info: () => { },
                debug: () => { }
            },

            // MCP tool functions - wrapped to ensure connection
            MyPlatform_Statistics_GetRates: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_Statistics_GetRates(params);
            },
            MyPlatform_CompanyInfo_GetAllProductNames: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_CompanyInfo_GetAllProductNames(params);
            },
            MyPlatform_ProvisioningInfo: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_ProvisioningInfo(params);
            },
            MyPlatform_Chart_GenerateChart: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_Chart_GenerateChart(params);
            },
            MyPlatform_ProvisioningInfo_GetById: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_ProvisioningInfo_GetById(params);
            },
            MyPlatform_Statistics_Get: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_Statistics_Get(params);
            },
            MyPlatform_MyPermissions_Get: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_MyPermissions_Get(params);
            },
            MyPlatform_CompanyInfo_GetProductsHierarchy: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_CompanyInfo_GetProductsHierarchy(params);
            },
            MyPlatform_CompanyInfo_FindProductHierarchy: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_CompanyInfo_FindProductHierarchy(params);
            },
            MyPlatform_Statistics_GetAverageDuration: async (params) => {
                await mcpTools.mcpClient.ensureConnected();
                return mcpTools.MyPlatform_Statistics_GetAverageDuration(params);
            },

            // Utility globals
            JSON,
            Math,
            Date,
            Promise,

            // Common utilities
            setTimeout,
            clearTimeout,
        };

        return endowments;
    }

    /**
     * Execute code in the sandbox
     * @param {string} code - The JavaScript code to execute
     * @returns {Promise<any>} The result of the code execution
     */
    async execute(code) {
        if (typeof code !== 'string') {
            throw new TypeError('Code must be a string');
        }

        const startTime = Date.now();
        let timeoutId;
        let abortController;

        try {
            // Create compartment with endowments
            const endowments = await this.createEndowments();

            // Add abort signal to endowments for cooperative cancellation
            abortController = new AbortController();
            endowments.signal = abortController.signal;

            const compartment = new Compartment(endowments);

            // Wrap code with cooperative timeout check
            const wrappedCode = `
        (async () => {
          // Helper to check if we should abort
          const checkTimeout = () => {
            if (signal && signal.aborted) {
              throw new Error('Execution timeout');
            }
          };
          
          ${code}

          return await main();
        })()
      `;

            // Create timeout that aborts execution
            const timeoutPromise = new Promise((_, reject) => {
                timeoutId = setTimeout(() => {
                    abortController.abort();
                    reject(new Error(`Execution timeout after ${this.options.timeout}ms`));
                }, this.options.timeout);
            });

            // Execute code with timeout race
            const resultPromise = compartment.evaluate(wrappedCode);
            const result = await Promise.race([resultPromise, timeoutPromise]);

            // Clear timeout
            clearTimeout(timeoutId);

            const executionTime = Date.now() - startTime;
            //console.debug(`✓ Code executed successfully in ${executionTime}ms`);

            return result;
        } catch (error) {
            if (timeoutId) clearTimeout(timeoutId);
            const executionTime = Date.now() - startTime;

            console.error(`✗ Execution failed after ${executionTime}ms:`, error.message);
            throw error;
        }
    }

    /**
     * Execute code and return both result and any errors
     * @param {string} code - The JavaScript code to execute
     * @returns {Promise<{success: boolean, result?: any, error?: Error, executionTime: number}>}
     */
    async executeSafe(code) {
        const startTime = Date.now();

        try {
            const result = await this.execute(code);
            return {
                success: true,
                result,
                executionTime: Date.now() - startTime
            };
        } catch (error) {
            return {
                success: false,
                error: error.message,
                errorStack: error.stack,
                executionTime: Date.now() - startTime
            };
        }
    }

    /**
     * Close MCP connection
     */
    async cleanup() {
        await mcpTools.mcpClient.close();
    }
}

/**
 * Main execution function for CLI usage
 */
async function main() {
    const args = process.argv.slice(2);

    if (args.length === 0) {
        console.log(`
Hardened JavaScript Sandbox Runner for MCP Tools

Usage:
  node sandbox-runner.js [agent-id] "<code>"
  node sandbox-runner.js [agent-id] --file <path-to-js-file>
  node sandbox-runner.js [agent-id] --json "<code>"

Arguments:
  agent-id    - (Optional) Agent ID for x-agent-id header

Examples:
  # Execute inline code
  node sandbox-runner.js "const result = await MyPlatform_MyPermissions_Get({}); return result;"

  # Execute with agent ID
  node sandbox-runner.js "my-agent-123" "const result = await MyPlatform_MyPermissions_Get({}); return result;"

  # Execute code from file
  node sandbox-runner.js --file my-script.js
  node sandbox-runner.js "agent-id" --file my-script.js

  # With JSON output
  node sandbox-runner.js --json "return await MyPlatform_CompanyInfo_GetAllProductNames({});"
  node sandbox-runner.js "agent-id" --json "return { test: true };"

Available MCP Tools:
  - MyPlatform_Statistics_GetRates
  - MyPlatform_CompanyInfo_GetAllProductNames
  - MyPlatform_ProvisioningInfo
  - MyPlatform_Chart_GenerateChart
  - MyPlatform_ProvisioningInfo_GetById
  - MyPlatform_Statistics_Get
  - MyPlatform_MyPermissions_Get
  - MyPlatform_CompanyInfo_GetProductsHierarchy
  - MyPlatform_CompanyInfo_FindProductHierarchy
  - MyPlatform_Statistics_GetAverageDuration

Note: Code runs in a hardened SES sandbox with limited access to Node.js APIs.
    `);
        process.exit(0);
    }

    let agentId = null;
    let code;
    let jsonOutput = false;
    let argIndex = 0;

    // Check if first argument is an agent ID (not a flag or code)
    if (args[0] && !args[0].startsWith('--') && (args.length > 1 || !args[0].includes('return'))) {
        // Likely an agent ID if there are more args or doesn't look like code
        if (args.length > 1 && (args[1].startsWith('--') || args[1].includes('return') || args[1].includes('await'))) {
            agentId = args[0];
            argIndex = 1;
        }
    }

    // Parse remaining arguments
    if (args[argIndex] === '--file') {
        const fs = require('fs');
        const filePath = args[argIndex + 1];
        if (!filePath) {
            console.error('Error: --file requires a file path');
            process.exit(1);
        }
        code = fs.readFileSync(filePath, 'utf-8');
    } else if (args[argIndex] === '--json') {
        jsonOutput = true;
        code = args[argIndex + 1];
    } else {
        code = args[argIndex];
    }

    if (!code) {
        console.error('Error: No code provided');
        process.exit(1);
    }

    const runner = new SandboxRunner(agentId, {
        timeout: 60000, // 60 second timeout for CLI
        enableConsole: !jsonOutput
    });

    try {
        const result = await runner.executeSafe(code);

        if (jsonOutput) {
            console.log(JSON.stringify(result, null, 2));
        } else {
            if (result.success) {
               // console.log('\n=== Execution Result ===');
               console.log(result.result);
            } else {
                //console.error('\n=== Execution Error ===');
                console.error(result.error);
                if (result.errorStack) {
                    console.error('\nStack trace:');
                    console.error(result.errorStack);
                }
            }
            //console.log(`\nExecution time: ${result.executionTime}ms`);
        }

        process.exit(result.success ? 0 : 1);
    } catch (error) {
        console.error('Fatal error:', error);
        process.exit(1);
    } finally {
        await runner.cleanup();
    }
}

// Run if executed directly
if (require.main === module) {
    main().catch(error => {
        console.error('Unhandled error:', error);
        process.exit(1);
    });
}

module.exports = { SandboxRunner, initializeSES };

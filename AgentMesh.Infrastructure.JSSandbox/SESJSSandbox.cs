using AgentMesh.Application.Services;

namespace AgentMesh.Infrastructure.JSSandbox
{
    public class SESJSSandbox : IJSSandbox
    {
        private static bool _dependenciesInstalled = false;
        private static readonly SemaphoreSlim _installSemaphore = new SemaphoreSlim(1, 1);

        private readonly SESJSSandboxConfiguration _configuration;

        public SESJSSandbox(SESJSSandboxConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task EnsureDependenciesInstalled()
        {
            if (_dependenciesInstalled)
            {
                return;
            }

            await _installSemaphore.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_dependenciesInstalled)
                {
                    return;
                }

                // Check if node_modules exists in Sandbox directory
                var sandboxDir = Path.GetFullPath("Sandbox");
                var nodeModulesPath = System.IO.Path.Combine(sandboxDir, "node_modules");

                if (!System.IO.Directory.Exists(nodeModulesPath))
                {
                    Console.WriteLine("Installing Node.js dependencies...");

                    var installProcess = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c npm install",
                        WorkingDirectory = sandboxDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    using (var process = System.Diagnostics.Process.Start(installProcess))
                    {
                        if (process == null)
                        {
                            throw new Exception("Failed to start npm install process");
                        }

                        var outputTask = process.StandardOutput.ReadToEndAsync();
                        var errorTask = process.StandardError.ReadToEndAsync();

                        await process.WaitForExitAsync();

                        var output = await outputTask;
                        var error = await errorTask;

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Failed to install Node.js dependencies: {error}");
                        }

                        Console.WriteLine("Node.js dependencies installed successfully.");
                    }
                }

                _dependenciesInstalled = true;
            }
            finally
            {
                _installSemaphore.Release();
            }
        }

        public async Task<string> RunCode(string agentId, string code)
        {
            // Ensure dependencies are installed (only runs once)
            await EnsureDependenciesInstalled();

            var fileToRunPath = Path.GetFullPath("Sandbox/sandbox-runner.js");

            // save code to a temp js file
            var codeFilePath = System.IO.Path.GetTempFileName() + ".js";
            await System.IO.File.WriteAllTextAsync(codeFilePath, code);
            Console.WriteLine("Saved code to temporary file: " + codeFilePath);

            // change it to node sandbox-runner.js [agent-id] --file <codeFilePath>

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("node", fileToRunPath + " " + agentId + " --file " + codeFilePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!string.IsNullOrWhiteSpace(_configuration.McpServerHost))
            {
                startInfo.EnvironmentVariables["MCP_SERVER_URL"] = _configuration.McpServerHost;
            }

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start Node.js process");
                }

                // log the complete command being run
                Console.WriteLine("Executing command:\nnode " + fileToRunPath + " " + agentId + " --file " + codeFilePath);

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Error executing JavaScript code: {error}");
                }

                return output;
            }
        }
    }
}

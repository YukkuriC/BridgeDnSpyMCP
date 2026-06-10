// 生成于 GLM-5V-Turbo

using System;
using DnSpyMCP.Server;
using DnSpyMCP.Services;

namespace DnSpyMCP
{
    // ---- 入口点 ----

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // 全局未捕获异常保护
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.Error.WriteLine("[DnSpyMCP] FATAL UnhandledException: " + e.ExceptionObject);
            };

            try
            {
                Console.Error.WriteLine("[DnSpyMCP] Starting server...");

                // 检查并确保所有 dnSpy 依赖 DLL 就位
                DllDependencyResolver.EnsureDependencies();

                var assemblyLoader = new AssemblyLoaderService();
                var metadataBrowser = new MetadataBrowserService(assemblyLoader);
                var decompilation = new DecompilationService(assemblyLoader);

                var toolRegistry = new McpToolRegistry(
                    assemblyLoader,
                    metadataBrowser,
                    decompilation
                );

                var server = new McpServer(toolRegistry);

                Console.Error.WriteLine("[DnSpyMCP] Server ready, waiting for stdio messages...");
                server.Run();

                Console.Error.WriteLine("[DnSpyMCP] Server shutdown normally.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[DnSpyMCP] FATAL: " + ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}

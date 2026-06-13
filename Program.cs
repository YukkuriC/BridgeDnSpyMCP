// 生成于 GLM-5V-Turbo

using System;
using BDSM.Server;
using BDSM.Services;

namespace BDSM
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
                Console.Error.WriteLine("[BDSM] FATAL UnhandledException: " + e.ExceptionObject);
            };

            try
            {
                Console.Error.WriteLine("[BDSM] Starting server...");

                // 1. 初始化配置管理器
                ConfigManager.Initialize();

                // 2. 初始化 DLL 依赖解析器（注册 AssemblyResolve + 自检）
                DnSpyDependencyResolver.Initialize();

                McpServer server;

                if (DnSpyDependencyResolver.SelfCheckPassed)
                {
                    // --- 正常模式：自检通过，加载全部服务 ---
                    Console.Error.WriteLine("[BDSM] Normal mode: full functionality available.");

                    var assemblyLoader = new AssemblyLoaderService();
                    var metadataBrowser = new MetadataBrowserService(assemblyLoader);
                    var decompilation = new DecompilationService(assemblyLoader);

                    var toolRegistry = new McpToolRegistry(
                        assemblyLoader,
                        metadataBrowser,
                        decompilation
                    );

                    server = new McpServer(toolRegistry);
                }
                else
                {
                    // --- 简化模式：自检失败，仅暴露配置工具 ---
                    Console.Error.WriteLine("[BDSM] Setup mode: dnSpy dependencies not resolved.");
                    Console.Error.WriteLine("[BDSM] Use the 'configure_dnspy_path' tool to set the correct path.");

                    var toolRegistry = new McpToolRegistry(); // setup mode
                    server = new McpServer(toolRegistry);
                }

                Console.Error.WriteLine("[BDSM] Server ready, waiting for stdio messages...");
                server.Run();

                Console.Error.WriteLine("[BDSM] Server shutdown normally.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[BDSM] FATAL: " + ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}

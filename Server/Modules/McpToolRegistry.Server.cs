// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>服务器管理模块 -- close_self</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterServerTools(List<Tool> tools)
        {
            tools.Add(MakeTool("close_self",
                "Gracefully shut down the MCP server process. The server will send a response confirming shutdown, then exit. This is a transport-level termination since MCP has no standard shutdown RPC method.",
                new Dictionary<string, PropertySchema>(),
                null));
            _dispatchers.Add(DispatchServer);
        }

        private bool DispatchServer(string toolName, Dictionary<string, object> args, out object result)
        {
            if (toolName == "close_self")
            {
                result = HandleShutdownServer();
                return true;
            }
            result = null;
            return false;
        }

        private object HandleShutdownServer()
        {
            var currentId = Process.GetCurrentProcess().Id;
            var processName = Process.GetCurrentProcess().ProcessName;
            var siblings = Process.GetProcessesByName(processName).Where(p => p.Id != currentId).ToList();

            int killed = 0;
            foreach (var p in siblings)
            {
                try
                {
                    p.Kill();
                    killed++;
                }
                catch { /* 进程可能已退出，忽略 */ }
            }

            var resultObj = new { status = "shutting_down", message = $"Server will exit after this response. Killed {killed} sibling process(es)." };
            _onShutdown?.Invoke();
            return resultObj;
        }
    }
}

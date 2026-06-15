// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>自检/配置模块 -- configure_dnspy_path</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterSetupTools(List<Tool> tools)
        {
            tools.Add(MakeTool("configure_dnspy_path",
                "Configure the dnSpy installation path. Required DLLs are loaded remotely from this path (not copied locally). After configuration, self-check runs automatically. If passed, restart the MCP server to activate full functionality.",
                new Dictionary<string, PropertySchema>
                {
                    {"path", new PropertySchema{ Type="string", Description="dnSpy installation directory (e.g. C:\\Program Files\\dnSpy)"}}
                },
                new List<string> {"path"}));
            _dispatchers.Add(DispatchSetup);
        }

        private bool DispatchSetup(string toolName, Dictionary<string, object> args, out object result)
        {
            if (toolName == "configure_dnspy_path")
            {
                result = HandleConfigureDnSpyPath(args);
                return true;
            }
            result = null;
            return false;
        }

        private object HandleConfigureDnSpyPath(Dictionary<string, object> args)
        {
            var path = GetRequiredArg<string>(args, "path");
            return DnSpyDependencyResolver.ConfigureAndRetry(path);
        }
    }
}

// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>程序集加载模块 -- load / list / unload / clear</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterAssemblyTools(List<Tool> tools)
        {
            tools.Add(MakeTool("load_assembly",
                "加载一个 .NET 程序集文件（.dll / .exe）以供后续分析。",
                new Dictionary<string, PropertySchema>
                {
                    {"path", new PropertySchema{ Type="string", Description="程序集文件的完整路径" }}
                },
                new List<string> {"path"}));

            tools.Add(MakeTool("list_assemblies",
                "列出当前已加载的所有程序集信息。",
                new Dictionary<string, PropertySchema>(),
                null));

            tools.Add(MakeTool("unload_assembly",
                "移除单个已加载的程序集，释放资源。",
                new Dictionary<string, PropertySchema>
                {
                    {"path", new PropertySchema{ Type="string", Description="要移除的程序集文件路径"}}
                },
                new List<string> {"path"}));

            tools.Add(MakeTool("clear_all_assemblies",
                "清空所有已加载的程序集，释放全部资源。",
                new Dictionary<string, PropertySchema>(),
                null));
            _dispatchers.Add(DispatchAssembly);
        }

        private bool DispatchAssembly(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "load_assembly":        result = HandleLoadAssembly(args); return true;
                case "list_assemblies":      result = _assemblyLoader.ListAssemblies(); return true;
                case "unload_assembly":      result = HandleUnloadAssembly(args); return true;
                case "clear_all_assemblies": result = HandleClearAllAssemblies(); return true;
                default: result = null; return false;
            }
        }

        private object HandleLoadAssembly(Dictionary<string, object> args)
        {
            var result = _assemblyLoader.LoadAssembly(GetRequiredArg<string>(args, "path"));
            NotifyToolsChanged();
            return result;
        }

        private object HandleUnloadAssembly(Dictionary<string, object> args)
        {
            var path = GetRequiredArg<string>(args, "path");
            var removed = _assemblyLoader.UnloadAssembly(path);
            NotifyToolsChanged();
            return new { success = removed, message = removed ? "Assembly unloaded." : "Assembly not found in loaded list." };
        }

        private object HandleClearAllAssemblies()
        {
            var count = _assemblyLoader.ClearAllAssemblies();
            NotifyToolsChanged();
            return new { cleared = count, message = string.Format("{0} assembly(es) cleared.", count) };
        }
    }
}

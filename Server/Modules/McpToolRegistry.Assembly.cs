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

            tools.Add(MakeTool("get_assembly_dependencies",
                "获取程序集的外部依赖清单（AssemblyRef 列表），包含每个依赖项的名称、版本、Culture、公钥令牌等信息。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}}
                },
                new List<string> {"assembly_path"}));

            tools.Add(MakeTool("list_probe_paths",
                "列出用户添加的探测路径（额外搜索路径，用于依赖程序集查找）。",
                new Dictionary<string, PropertySchema>(),
                null));

            tools.Add(MakeTool("add_probe_path",
                "添加探测路径到 AssemblyResolver 的优先搜索列表，用于依赖程序集查找。",
                new Dictionary<string, PropertySchema>
                {
                    {"path", new PropertySchema{ Type="string", Description="要添加的探测路径（必须是有效的目录）"}}
                },
                new List<string> {"path"}));

            tools.Add(MakeTool("remove_probe_path",
                "从 AssemblyResolver 的优先搜索列表移除探测路径。",
                new Dictionary<string, PropertySchema>
                {
                    {"path", new PropertySchema{ Type="string", Description="要移除的探测路径"}}
                },
                new List<string> {"path"}));

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
                case "get_assembly_dependencies": result = HandleGetAssemblyDependencies(args); return true;
                case "list_probe_paths":     result = _assemblyLoader.GetPreSearchPaths(); return true;
                case "add_probe_path":       result = HandleAddProbePath(args); return true;
                case "remove_probe_path":    result = HandleRemoveProbePath(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleLoadAssembly(Dictionary<string, object> args)
        {
            return _assemblyLoader.LoadAssembly(GetRequiredArg<string>(args, "path"));
        }

        private object HandleUnloadAssembly(Dictionary<string, object> args)
        {
            var path = GetRequiredArg<string>(args, "path");
            var removed = _assemblyLoader.UnloadAssembly(path);
            return new { success = removed, message = removed ? "Assembly unloaded." : "Assembly not found in loaded list." };
        }

        private object HandleClearAllAssemblies()
        {
            var count = _assemblyLoader.ClearAllAssemblies();
            return new { cleared = count, message = string.Format("{0} assembly(ies) cleared.", count) };
        }

        private object HandleGetAssemblyDependencies(Dictionary<string, object> args)
        {
            return _assemblyLoader.GetAssemblyDependencies(GetRequiredArg<string>(args, "assembly_path"));
        }

        private object HandleAddProbePath(Dictionary<string, object> args)
        {
            var path = GetRequiredArg<string>(args, "path");
            _assemblyLoader.AddProbePath(path);
            return new { success = true, message = "Probe path added: " + path };
        }

        private object HandleRemoveProbePath(Dictionary<string, object> args)
        {
            var path = GetRequiredArg<string>(args, "path");
            _assemblyLoader.RemoveProbePath(path);
            return new { success = true, message = "Probe path removed: " + path };
        }
    }
}

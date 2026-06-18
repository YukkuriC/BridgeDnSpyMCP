// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>类型查询模块 -- list_types / list_namespaces / find_type / get_type_info</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterTypeQueryTools(List<Tool> tools)
        {
            tools.Add(MakeTool("list_types",
                "列出已加载程序集中的所有顶层类型，支持按命名空间过滤。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"namespace_filter", new PropertySchema{ Type="string", Description="可选，仅列出指定命名空间下的类型"}}
                },
                new List<string> {"assembly_path"}));

            tools.Add(MakeTool("list_namespaces",
                "列出已加载程序集中的所有命名空间及其包含的类型数量。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}}
                },
                new List<string> {"assembly_path"}));

            tools.Add(MakeTool("find_type",
                "按名称或全限定名搜索类型（支持模糊匹配）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"query", new PropertySchema{ Type="string", Description="搜索关键词"}}
                },
                new List<string> {"assembly_path", "query"}));

            tools.Add(MakeTool("get_type_info",
                "获取指定类型的详细信息：基类、接口、成员统计、修饰符等。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                },
                new List<string> {"assembly_path", "full_type_name"}));
            _dispatchers.Add(DispatchTypeQuery);
        }

        private bool DispatchTypeQuery(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "list_types":      result = HandleListTypes(args); return true;
                case "list_namespaces": result = HandleListNamespaces(args); return true;
                case "find_type":       result = HandleFindType(args); return true;
                case "get_type_info":   result = HandleGetTypeInfo(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleListTypes(Dictionary<string, object> args)
        {
            return _metadataBrowser.ListTypes(
                GetRequiredArg<string>(args, "assembly_path"),
                GetOptionalArg<string>(args, "namespace_filter"));
        }

        private object HandleListNamespaces(Dictionary<string, object> args)
        {
            return _metadataBrowser.ListNamespaces(GetRequiredArg<string>(args, "assembly_path"));
        }

        private object HandleFindType(Dictionary<string, object> args)
        {
            return _metadataBrowser.FindType(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "query"));
        }

        private object HandleGetTypeInfo(Dictionary<string, object> args)
        {
            var info = _metadataBrowser.GetTypeInfo(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"));
            if (info == null) throw new UserException("Type not found.");
            return info;
        }
    }
}

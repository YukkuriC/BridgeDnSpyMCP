// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>路径查询模块 -- query_path</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterPathQueryTools(List<Tool> tools)
        {
            tools.Add(MakeTool("query_path",
                "按点分路径在已加载程序集中查找目标。支持从命名空间/类名开始，逐层解析到类型、字段、属性、事件或方法。" +
                "歧义时自动多路径并行追踪，一次调用返回所有完整匹配结果。",
                new Dictionary<string, PropertySchema>
                {
                    {"path", new PropertySchema{ Type="string", Description="点分路径，如 System.IO.File.Exists 或 MyClass.SomeField"}},
                    {"assembly_path", new PropertySchema{ Type="string", Description="可选，限定搜索范围到指定程序集路径"}},
                    {"search_inheritance", new PropertySchema{ Type="boolean", Description="成员未命中时是否沿基类/接口搜索，默认 false"}}
                },
                new List<string> {"path"}));
            _dispatchers.Add(DispatchPathQuery);
        }

        private bool DispatchPathQuery(string toolName, Dictionary<string, object> args, out object result)
        {
            if (toolName == "query_path")
            {
                result = HandleQueryPath(args);
                return true;
            }
            result = null;
            return false;
        }

        private object HandleQueryPath(Dictionary<string, object> args)
        {
            var path = GetRequiredArg<string>(args, "path");
            var assemblyPath = GetOptionalArg<string>(args, "assembly_path");
            var searchInheritance = GetOptionalArg<bool>(args, "search_inheritance");
            return _pathQuery.Resolve(path, assemblyPath, searchInheritance);
        }
    }
}

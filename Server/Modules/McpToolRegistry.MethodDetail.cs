// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>方法详情模块 -- get_method_info / get_method_il</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterMethodDetailTools(List<Tool> tools)
        {
            tools.Add(MakeTool("get_method_info",
                "获取单个方法的完整签名和元数据信息。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name"}));

            tools.Add(MakeTool("get_method_il",
                "获取方法的 IL 指令列表（偏移量、操作码、操作数）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name"}));
            _dispatchers.Add(DispatchMethodDetail);
        }

        private bool DispatchMethodDetail(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "get_method_info": result = HandleGetMethodInfo(args); return true;
                case "get_method_il":   result = HandleGetMethodIL(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleGetMethodInfo(Dictionary<string, object> args)
        {
            var info = _metadataBrowser.GetMethodInfo(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"));
            if (info == null) throw new UserException("Method not found.");
            return info;
        }

        private object HandleGetMethodIL(Dictionary<string, object> args)
        {
            var il = _metadataBrowser.GetMethodIL(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"));
            if (il == null) throw new UserException("Method has no body or not found.");
            return il;
        }
    }
}

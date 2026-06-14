// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>成员枚举模块 -- list_methods / list_fields / list_properties / list_events</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterMemberTools(List<Tool> tools)
        {
            tools.Add(MakeTool("list_methods",
                "列出指定类型的所有方法（含签名、返回值、可见性等）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                },
                new List<string> {"assembly_path", "full_type_name"}));

            tools.Add(MakeTool("list_fields",
                "列出指定类型的所有字段（含类型、常量值、可见性等）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                },
                new List<string> {"assembly_path", "full_type_name"}));

            tools.Add(MakeTool("list_properties",
                "列出指定类型的所有属性（含类型、getter/setter 可见性等）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                },
                new List<string> {"assembly_path", "full_type_name"}));

            tools.Add(MakeTool("list_events",
                "列出指定类型的所有事件。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                },
                new List<string> {"assembly_path", "full_type_name"}));
        }

        private bool TryDispatchMembers(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "list_methods":    result = HandleListMethods(args); return true;
                case "list_fields":     result = HandleListFields(args); return true;
                case "list_properties": result = HandleListProperties(args); return true;
                case "list_events":     result = HandleListEvents(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleListMethods(Dictionary<string, object> args)
        {
            return _metadataBrowser.ListMethods(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"));
        }

        private object HandleListFields(Dictionary<string, object> args)
        {
            return _metadataBrowser.ListFields(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"));
        }

        private object HandleListProperties(Dictionary<string, object> args)
        {
            return _metadataBrowser.ListProperties(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"));
        }

        private object HandleListEvents(Dictionary<string, object> args)
        {
            return _metadataBrowser.ListEvents(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"));
        }
    }
}

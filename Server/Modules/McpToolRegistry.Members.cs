// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>成员枚举模块 -- list_members(op) 统一入口</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterMemberTools(List<Tool> tools)
        {
            tools.Add(MakeTool("list_members",
                "列出指定类型的成员。通过 op 参数指定成员类型：methods（方法）、fields（字段）、properties（属性）、events（事件）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"op", new PropertySchema{ Type="string", Description="成员类型: methods / fields / properties / events"}}
                },
                new List<string> {"assembly_path", "full_type_name", "op"}));
            _dispatchers.Add(DispatchMembers);
        }

        private bool DispatchMembers(string toolName, Dictionary<string, object> args, out object result)
        {
            if (toolName == "list_members")
            {
                result = HandleListMembers(args);
                return true;
            }
            result = null;
            return false;
        }

        private object HandleListMembers(Dictionary<string, object> args)
        {
            var op = GetRequiredArg<string>(args, "op");
            var assemblyPath = GetRequiredArg<string>(args, "assembly_path");
            var fullTypeName = GetRequiredArg<string>(args, "full_type_name");

            switch (op)
            {
                case "methods":    return _metadataBrowser.ListMethods(assemblyPath, fullTypeName);
                case "fields":     return _metadataBrowser.ListFields(assemblyPath, fullTypeName);
                case "properties": return _metadataBrowser.ListProperties(assemblyPath, fullTypeName);
                case "events":     return _metadataBrowser.ListEvents(assemblyPath, fullTypeName);
                default: throw new UserException("Invalid op '" + op + "'. Must be one of: methods, fields, properties, events.");
            }
        }
    }
}

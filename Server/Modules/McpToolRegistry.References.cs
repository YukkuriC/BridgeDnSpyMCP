// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>引用分析模块 -- find_references / find_all_string_refs</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterReferenceTools(List<Tool> tools)
        {
            tools.Add(MakeTool("find_references",
                "查找某个成员（方法/字段/属性/事件）在程序集中的所有引用位置。返回包含类型、方法、IL偏移量等信息的列表。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="成员所属类型的全限定名"}},
                    {"member_name", new PropertySchema{ Type="string", Description="要查找的成员名称（方法名、字段名、属性名或事件名）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "member_name"}));

            tools.Add(MakeTool("find_all_string_refs",
                "查找程序集中包含指定字符串的 ldstr 指令位置。用于定位硬编码字符串的使用处。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"search_string", new PropertySchema{ Type="string", Description="要搜索的字符串（支持子串匹配）"}}
                },
                new List<string> {"assembly_path", "search_string"}));

            _dispatchers.Add(DispatchReferences);
        }

        private bool DispatchReferences(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "find_references":      result = HandleFindReferences(args); return true;
                case "find_all_string_refs": result = HandleFindAllStringRefs(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleFindReferences(Dictionary<string, object> args)
        {
            return _metadataBrowser.FindReferences(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "member_name"));
        }

        private object HandleFindAllStringRefs(Dictionary<string, object> args)
        {
            return _metadataBrowser.FindAllStringRefs(
                GetRequiredArg<string>(args, "search_string"));
        }
    }
}

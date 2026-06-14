// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>反编译输出模块 -- decompile_type / decompile_method / decompile_assembly / decompile_to_il</summary>
    public partial class McpToolRegistry
    {
        internal void RegisterDecompilationTools(List<Tool> tools)
        {
            tools.Add(MakeTool("decompile_type",
                "将指定类型反编译为 C# 源码文本。输出接近原始源码级别的 C# 代码。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="要反编译的类型的全限定名"}}
                },
                new List<string> {"assembly_path", "full_type_name"}));

            tools.Add(MakeTool("decompile_method",
                "将单个方法反编译为 C# 源码文本。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="要反编译的方法名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name"}));

            tools.Add(MakeTool("decompile_assembly",
                "反编译整个程序集中所有类型的 C# 源码。大型程序集建议配合 namespace_filter 使用。结果以 JSON 对象返回（key=类型全名, value=C#代码）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"namespace_filter", new PropertySchema{ Type="string", Description="可选，仅反编译指定命名空间下的类型"}}
                },
                new List<string> {"assembly_path"}));

            tools.Add(MakeTool("decompile_to_il",
                "将指定方法输出为 ILASM 格式的中间语言文本（含 .method 声明、.maxstack、指令列表）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="要输出的方法名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name"}));
        }

        private bool TryDispatchDecompilation(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "decompile_type":      result = HandleDecompileType(args); return true;
                case "decompile_method":    result = HandleDecompileMethod(args); return true;
                case "decompile_assembly":  result = HandleDecompileAssembly(args); return true;
                case "decompile_to_il":     result = HandleDecompileToIL(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleDecompileType(Dictionary<string, object> args)
        {
            return _decompilation.DecompileType(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"));
        }

        private object HandleDecompileMethod(Dictionary<string, object> args)
        {
            return _decompilation.DecompileMethod(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"));
        }

        private object HandleDecompileAssembly(Dictionary<string, object> args)
        {
            return _decompilation.DecompileAssembly(
                GetRequiredArg<string>(args, "assembly_path"),
                GetOptionalArg<string>(args, "namespace_filter"));
        }

        private object HandleDecompileToIL(Dictionary<string, object> args)
        {
            return _decompilation.DecompileMethodToIL(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"));
        }
    }
}

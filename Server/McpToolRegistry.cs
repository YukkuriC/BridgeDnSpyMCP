// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using DnSpyMCP.Services;
using DnSpyMCP.Server.Protocol;

namespace DnSpyMCP.Server
{
    /// <summary>
    /// MCP 工具注册表。
    /// 定义所有暴露给 AI 的工具及其参数 schema，并分发调用到对应的服务方法。
    /// </summary>
    public class McpToolRegistry
    {
        private readonly AssemblyLoaderService _assemblyLoader;
        private readonly MetadataBrowserService _metadataBrowser;
        private readonly DecompilationService _decompilation;
        private readonly JsonSerializerSettings _jsonSettings;

        public McpToolRegistry(
            AssemblyLoaderService assemblyLoader,
            MetadataBrowserService metadataBrowser,
            DecompilationService decompilation)
        {
            _assemblyLoader = assemblyLoader;
            _metadataBrowser = metadataBrowser;
            _decompilation = decompilation;
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        /// <summary>返回所有已注册的工具定义</summary>
        public ListToolsResult ListTools()
        {
            return new ListToolsResult
            {
                Tools = new List<Tool>
                {
                    // ===== 程序集加载与浏览 =====
                    MakeTool("load_assembly",
                        "加载一个 .NET 程序集文件（.dll / .exe）以供后续分析。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"path", new PropertySchema{ Type="string", Description="程序集文件的完整路径" }}
                        },
                        new List<string> {"path"}),

                    MakeTool("list_assemblies",
                        "列出当前已加载的所有程序集信息。",
                        new Dictionary<string, PropertySchema>(),
                        null),

                    // ===== 类型查询 =====
                    MakeTool("list_types",
                        "列出已加载程序集中的所有顶层类型，支持按命名空间过滤。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"namespace_filter", new PropertySchema{ Type="string", Description="可选，仅列出指定命名空间下的类型"}}
                        },
                        new List<string> {"assembly_path"}),

                    MakeTool("list_namespaces",
                        "列出已加载程序集中的所有命名空间及其包含的类型数量。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}}
                        },
                        new List<string> {"assembly_path"}),

                    MakeTool("find_type",
                        "按名称或全限定名搜索类型（支持模糊匹配）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"query", new PropertySchema{ Type="string", Description="搜索关键词"}}
                        },
                        new List<string> {"assembly_path", "query"}),

                    MakeTool("get_type_info",
                        "获取指定类型的详细信息：基类、接口、成员统计、修饰符等。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name"}),

                    // ---- 成员枚举 ----
                    MakeTool("list_methods",
                        "列出指定类型的所有方法（含签名、返回值、可见性等）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name"}),

                    MakeTool("list_fields",
                        "列出指定类型的所有字段（含类型、常量值、可见性等）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name"}),

                    MakeTool("list_properties",
                        "列出指定类型的所有属性（含类型、getter/setter 可见性等）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name"}),

                    MakeTool("list_events",
                        "列出指定类型的所有事件。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name"}),

                    MakeTool("get_method_info",
                        "获取单个方法的完整签名和元数据信息。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                            {"method_name", new PropertySchema{ Type="string", Description="方法名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name", "method_name"}),

                    MakeTool("get_method_il",
                        "获取方法的 IL 指令列表（偏移量、操作码、操作数）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                            {"method_name", new PropertySchema{ Type="string", Description="方法名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name", "method_name"}),

                    // ===== 反编译输出 =====
                    MakeTool("decompile_type",
                        "将指定类型反编译为 C# 源码文本。输出接近原始源码级别的 C# 代码。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="要反编译的类型的全限定名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name"}),

                    MakeTool("decompile_method",
                        "将单个方法反编译为 C# 源码文本。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                            {"method_name", new PropertySchema{ Type="string", Description="要反编译的方法名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name", "method_name"}),

                    MakeTool("decompile_assembly",
                        "反编译整个程序集中所有类型的 C# 源码。大型程序集建议配合 namespace_filter 使用。结果以 JSON 对象返回（key=类型全名, value=C#代码）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"namespace_filter", new PropertySchema{ Type="string", Description="可选，仅反编译指定命名空间下的类型"}}
                        },
                        new List<string> {"assembly_path"}),

                    MakeTool("decompile_to_il",
                        "将指定方法输出为 ILASM 格式的中间语言文本（含 .method 声明、.maxstack、指令列表）。",
                        new Dictionary<string, PropertySchema>
                        {
                            {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                            {"full_type_name", new PropertySchema{ Type="string", Description="所属类型的全限定名"}},
                            {"method_name", new PropertySchema{ Type="string", Description="要输出的方法名"}}
                        },
                        new List<string> {"assembly_path", "full_type_name", "method_name"}),
                }
            };
        }

        /// <summary>
        /// 分发工具调用到对应的处理方法。
        /// </summary>
        public CallToolResult CallTool(string toolName, Dictionary<string, object> arguments)
        {
            try
            {
                object result;
                switch (toolName)
                {
                    case "load_assembly":       result = HandleLoadAssembly(arguments); break;
                    case "list_assemblies":     result = HandleListAssemblies(); break;
                    case "list_types":          result = HandleListTypes(arguments); break;
                    case "list_namespaces":     result = HandleListNamespaces(arguments); break;
                    case "find_type":           result = HandleFindType(arguments); break;
                    case "get_type_info":       result = HandleGetTypeInfo(arguments); break;
                    case "list_methods":        result = HandleListMethods(arguments); break;
                    case "list_fields":         result = HandleListFields(arguments); break;
                    case "list_properties":     result = HandleListProperties(arguments); break;
                    case "list_events":         result = HandleListEvents(arguments); break;
                    case "get_method_info":     result = HandleGetMethodInfo(arguments); break;
                    case "get_method_il":       result = HandleGetMethodIL(arguments); break;
                    case "decompile_type":      result = HandleDecompileType(arguments); break;
                    case "decompile_method":    result = HandleDecompileMethod(arguments); break;
                    case "decompile_assembly":  result = HandleDecompileAssembly(arguments); break;
                    case "decompile_to_il":     result = HandleDecompileToIL(arguments); break;
                    default: throw new NotSupportedException("Unknown tool: " + toolName);
                }

                return new CallToolResult
                {
                    Content = new List<ContentBase> { new TextContent(Serialize(result)) },
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBase> { new TextContent("Error: " + ex.Message) },
                    IsError = true
                };
            }
        }

        // ---- 工具处理器 ----

        private object HandleLoadAssembly(Dictionary<string, object> args)
        {
            return _assemblyLoader.LoadAssembly(GetRequiredArg<string>(args, "path"));
        }

        private object HandleListAssemblies()
        {
            return _assemblyLoader.ListAssemblies();
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
            if (info == null) throw new Services.NotFoundException("Type not found.");
            return info;
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

        private object HandleGetMethodInfo(Dictionary<string, object> args)
        {
            var info = _metadataBrowser.GetMethodInfo(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"));
            if (info == null) throw new Services.NotFoundException("Method not found.");
            return info;
        }

        private object HandleGetMethodIL(Dictionary<string, object> args)
        {
            var il = _metadataBrowser.GetMethodIL(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"));
            if (il == null) throw new Services.NotFoundException("Method has no body or not found.");
            return il;
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

        // ---- 辅助 ----

        private static Tool MakeTool(string name, string desc, Dictionary<string, PropertySchema> props, List<string> required)
        {
            return new Tool
            {
                Name = name,
                Description = desc,
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = props,
                    Required = required
                }
            };
        }

        private T GetRequiredArg<T>(Dictionary<string, object> args, string key)
        {
            object value;
            if (args == null || !args.TryGetValue(key, out value))
                throw new ArgumentException("Missing required argument: '" + key + "'");
            return ConvertValue<T>(value);
        }

        private T GetOptionalArg<T>(Dictionary<string, object> args, string key)
        {
            object value;
            if (args == null || !args.TryGetValue(key, out value) || value == null)
                return default(T);
            return ConvertValue<T>(value);
        }

        private static T ConvertValue<T>(object value)
        {
            if (value is T) return (T)value;
            if (typeof(T) == typeof(string) && value != null)
                return (T)(object)value.ToString();
            return (T)Convert.ChangeType(value, typeof(T),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        private string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, _jsonSettings);
        }
    }
}

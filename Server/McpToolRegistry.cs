// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using BDSM;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>
    /// MCP 工具注册表。
    /// 支持两种模式：
    /// - 正常模式：暴露全部分析/反编译工具（需要服务实例）。
    /// - Setup 模式：仅暴露 configure_dnspy_path 工具（无服务依赖）。
    ///
    ///   Modules/McpToolRegistry.*.cs            -- 按功能模块拆分的 partial class
    /// </summary>
    public partial class McpToolRegistry
    {
        private readonly AssemblyLoaderService _assemblyLoader;
        private readonly MetadataBrowserService _metadataBrowser;
        private readonly DecompilationService _decompilation;
        private readonly AssemblyEditorService _editor;
        private readonly PathQueryService _pathQuery;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly bool _isSetupMode;
        private readonly Action _onShutdown;
        private readonly List<ToolDispatcher> _dispatchers = new List<ToolDispatcher>();

        /// <summary>工具分发委托：各模块注册自己的匹配+执行逻辑</summary>
        private delegate bool ToolDispatcher(string toolName, Dictionary<string, object> args, out object result);

        // ---- 正常模式构造函数 ----

        public McpToolRegistry(
            AssemblyLoaderService assemblyLoader,
            MetadataBrowserService metadataBrowser,
            DecompilationService decompilation,
            AssemblyEditorService editor,
            PathQueryService pathQuery,
            Action onShutdown = null)
        {
            _assemblyLoader = assemblyLoader;
            _metadataBrowser = metadataBrowser;
            _decompilation = decompilation;
            _editor = editor;
            _pathQuery = pathQuery;
            _isSetupMode = false;
            _onShutdown = onShutdown;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        // ---- Setup 模式构造函数 ----

        /// <summary>
        /// 创建 setup 模式的工具注册表，仅包含 configure_dnspy_path 工具。
        /// 用于自检失败时让 MCP agent 配置 dnSpy 安装路径。
        /// </summary>
        public McpToolRegistry()
        {
            _isSetupMode = true;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        /// <summary>是否处于 setup 模式（自检未通过）</summary>
        public bool IsSetupMode => _isSetupMode;

        /// <summary>返回所有已注册的工具定义</summary>
        public ListToolsResult ListTools()
        {
            if (_isSetupMode)
            {
                var tools = new List<Tool>();
                RegisterSetupTools(tools);
                return new ListToolsResult { Tools = tools };
            }

            var allTools = new List<Tool>();
            RegisterAssemblyTools(allTools);
            RegisterReferenceTools(allTools);
            RegisterTypeQueryTools(allTools);
            RegisterMemberTools(allTools);
            RegisterMethodDetailTools(allTools);
            RegisterDecompilationTools(allTools);
            RegisterEditorTools(allTools);
            RegisterServerTools(allTools);
            RegisterPathQueryTools(allTools);
            return new ListToolsResult { Tools = allTools };
        }

        /// <summary>
        /// 分发工具调用到对应的处理方法。
        /// 各模块通过 TryDispatch 方法自行匹配 toolName 并执行。
        /// </summary>
        public CallToolResult CallTool(string toolName, Dictionary<string, object> arguments)
        {
            try
            {
                object result = null;
                bool handled;

                if (_isSetupMode)
                {
                    handled = DispatchSetup(toolName, arguments, out result);
                    if (!handled)
                        throw new UserException(
                            "Unknown tool: " + toolName +
                            ". Currently in setup mode. Use 'configure_dnspy_path' to configure dnSpy first.");
                }
                else
                {
                    handled = _dispatchers.Any(d => d(toolName, arguments, out result));
                    if (!handled)
                        throw new UserException("Unknown tool: " + toolName);
                }

                return new CallToolResult
                {
                    Content = new List<ContentBase> { new TextContent(Serialize(result)) },
                    IsError = false
                };
            }
            catch (UserException userEx)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBase> { new TextContent(userEx.Message) },
                    IsError = true
                };
            }
            catch (Exception ex)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBase> { new TextContent(ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace) },
                    IsError = true
                };
            }
        }

        // ---- 辅助方法 ----

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
                throw new UserException("Missing required argument: '" + key + "'");
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

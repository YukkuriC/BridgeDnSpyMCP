// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>
    /// MCP 服务器核心实现。
    /// 基于 JSON-RPC 2.0 协议，通过 stdio 与客户端通信。
    /// 传输层使用换行分隔的 JSON（NDJSON），每行一个完整的 JSON-RPC 消息。
    /// </summary>
    public class McpServer
    {
        public McpToolRegistry toolRegistry;
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private bool _initialized;
        private volatile bool _shutdownRequested;

        // 调试日志开关 -- 出问题时设为 true，输出到 stderr 可在 Trae 终端中查看
        private const bool DebugLog = true;

        /// <summary>外部可获取的 shutdown 回调，传入 McpToolRegistry 以响应 shutdown_server 工具调用</summary>
        public Action ShutdownAction => () => _shutdownRequested = true;

        /// <summary>
        /// 启动 stdio 循环，逐行读取 JSON 请求并写入响应直到流结束。
        /// </summary>
        public void Run()
        {
            var stdin = Console.OpenStandardInput();
            var stdout = Console.OpenStandardOutput();

            Log("MCP server started, entering read loop...");

            // 使用 UTF-8 无 BOM 编码，确保输出纯净
            using (var reader = new StreamReader(stdin, new UTF8Encoding(false), false))
            using (var writer = new StreamWriter(stdout, new UTF8Encoding(false), 1024, true) { AutoFlush = false })
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        Log("Received: " + (line.Length > 200 ? line.Substring(0, 200) + "..." : line));

                        string responseJson = ProcessAndSerialize(line);
                        if (responseJson != null)
                        {
                            Log("Sending: " + responseJson);
                            writer.WriteLine(responseJson);
                            try
                            {
                                writer.Flush();
                            }
                            catch (Exception flushEx)
                            {
                                Log("FLUSH ERROR (stdout closed?): " + flushEx.Message);
                                break; // stdout 被关闭，退出循环
                            }
                            Log("Sent OK, waiting for next message...");
                        }

                        // 检查 shutdown 信号（在响应已发送后检查，确保客户端收到确认）
                        if (_shutdownRequested)
                        {
                            Log("Shutdown requested, exiting read loop.");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("ERROR: " + ex.Message + "\n" + ex.StackTrace);
                        try
                        {
                            var errorResp = new JsonRpcResponse
                            {
                                Id = null,
                                Error = new JsonRpcError { Code = -32700, Message = "Parse error: " + ex.Message }
                            };
                            writer.WriteLine(JsonConvert.SerializeObject(errorResp, _jsonSettings));
                            writer.Flush();
                        }
                        catch { /* 写入失败则忽略 */ }
                    }
                }

                Log("EOF on stdin, exiting read loop.");
            }

            Log("MCP server shutdown.");
        }

        /// <summary>
        /// 处理消息并返回序列化后的 JSON 响应字符串。
        /// 返回 null 表示无需回复（通知类消息）。
        /// </summary>
        private string ProcessAndSerialize(string rawMessage)
        {
            JsonRpcResponse response;
            try
            {
                response = ProcessMessage(rawMessage);
            }
            catch (Exception ex)
            {
                Log("ProcessMessage error: " + ex.Message);
                response = new JsonRpcResponse
                {
                    Id = null,
                    Error = new JsonRpcError { Code = -32700, Message = "Parse error: " + ex.Message }
                };
            }

            if (response == null) return null;
            return JsonConvert.SerializeObject(response, _jsonSettings);
        }

        /// <summary>
        /// 处理单条 JSON-RPC 消息，返回响应对象（通知类消息返回 null）。
        /// </summary>
        private JsonRpcResponse ProcessMessage(string rawMessage)
        {
            // 使用 JObject 先做原始解析，避免强类型反序列化丢失数据
            var jobj = JObject.Parse(rawMessage);
            var method = jobj["method"]?.ToString();

            // 正确提取 id：确保是原始类型（int/string）而非 JToken 包装
            var rawId = jobj["id"];
            object id = ExtractRawId(rawId);

            Log("Method: " + method ?? "(null)" + ", id=" + (id != null ? id.ToString() : "null"));

            switch (method)
            {
                case "initialize":
                    return HandleInitialize(id);

                case "notifications/initialized":
                    Log("Client confirmed initialization.");
                    return null; // 通知，不回复

                case "tools/list":
                    return HandleToolsList(id);

                case "tools/call":
                    return HandleToolsCall(id, jobj["params"]);

                case "ping":
                    return HandlePing(id);

                default:
                    return new JsonRpcResponse
                    {
                        Id = id,
                        Error = new JsonRpcError { Code = -32601, Message = "Method not found: " + method }
                    };
            }
        }

        private JsonRpcResponse HandleInitialize(object id)
        {
            _initialized = true;

            var serverInfo = new ServerInfoData
            {
                Name = "BridgeDnSpyMCP",
                Version = "1.0.0"
            };

            if (toolRegistry.IsSetupMode)
            {
                Log("Initialization complete (SETUP MODE - dnSpy dependencies not resolved).");
                return new JsonRpcResponse
                {
                    Id = id,
                    Result = new InitializeResult
                    {
                        ProtocolVersion = "2025-11-25",
                        Capabilities = new ServerCapabilities
                        {
                            Tools = new ToolsCapability()
                        },
                        ServerInfo = serverInfo
                    }
                };
            }

            Log("Initialization complete.");
            return new JsonRpcResponse
            {
                Id = id,
                Result = new InitializeResult
                {
                    ProtocolVersion = "2025-11-25",
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolsCapability()
                    },
                    ServerInfo = serverInfo
                }
            };
        }

        private JsonRpcResponse HandleToolsList(object id)
        {
            var tools = toolRegistry.ListTools();
            return new JsonRpcResponse { Id = id, Result = tools };
        }

        private JsonRpcResponse HandleToolsCall(object id, JToken paramsToken)
        {
            if (!_initialized)
            {
                return new JsonRpcResponse
                {
                    Id = id,
                    Error = new JsonRpcError { Code = -32002, Message = "Server not initialized" }
                };
            }

            try
            {
                var argsDict = ExtractArguments(paramsToken?["arguments"]);
                var toolName = ExtractToolName(paramsToken);
                Log("Tool call: " + toolName);
                var result = toolRegistry.CallTool(toolName, argsDict);
                return new JsonRpcResponse { Id = id, Result = result };
            }
            catch (Exception ex)
            {
                Log("Tool call error: " + ex.Message);
                return new JsonRpcResponse
                {
                    Id = id,
                    Error = new JsonRpcError { Code = -32603, Message = "Internal error: " + ex.Message }
                };
            }
        }

        private JsonRpcResponse HandlePing(object id)
        {
            return new JsonRpcResponse { Id = id, Result = new { status = "ok" } };
        }

        // ---- 参数提取辅助 ----

        /// <summary>
        /// 从 JToken 中提取原始类型的 id 值（int/long/string），避免 JToken 被序列化为复杂对象。
        /// </summary>
        private static object ExtractRawId(JToken idToken)
        {
            if (idToken == null) return null;
            switch (idToken.Type)
            {
                case JTokenType.Integer:
                    long lVal = (long)idToken;
                    if (lVal >= int.MinValue && lVal <= int.MaxValue) return (int)lVal;
                    return lVal;
                case JTokenType.String:
                    return (string)idToken;
                case JTokenType.Null:
                    return null;
                default:
                    return idToken.ToString();
            }
        }

        private static Dictionary<string, object> ExtractArguments(JToken paramsToken)
        {
            if (paramsToken == null || paramsToken.Type != JTokenType.Object)
                return new Dictionary<string, object>();

            var result = new Dictionary<string, object>();
            foreach (var prop in ((JObject)paramsToken).Properties())
            {
                result[prop.Name] = ConvertJToken(prop.Value);
            }
            return result;
        }

        private static string ExtractToolName(JToken paramsToken)
        {
            if (paramsToken?["name"] != null)
                return paramsToken["name"].ToString();
            return "";
        }

        private static object ConvertJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null:
                    return null;
                case JTokenType.Boolean:
                    return (bool)token;
                case JTokenType.Integer:
                    // 区分 int / long
                    long lVal = (long)token;
                    if (lVal >= int.MinValue && lVal <= int.MaxValue) return (int)lVal;
                    return lVal;
                case JTokenType.Float:
                    return (double)token;
                case JTokenType.String:
                    return (string)token;
                case JTokenType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((JObject)token).Properties())
                        dict[prop.Name] = ConvertJToken(prop.Value);
                    return dict;
                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                        list.Add(ConvertJToken(item));
                    return list;
                default:
                    return token.ToString();
            }
        }

        // ---- 日志 ----

        private static void Log(string message)
        {
            if (!DebugLog) return;
            try
            {
                Console.Error.WriteLine("[BDSM] " + message);
            }
            catch { /* stderr 不可用时静默 */ }
        }
    }
}

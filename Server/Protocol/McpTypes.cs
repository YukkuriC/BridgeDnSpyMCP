// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BDSM.Server.Protocol
{
    #region JSON-RPC 基础类型

    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JsonElementWrapper Params { get; set; }
    }

    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("error")]
        public JsonRpcError Error { get; set; }
    }

    public class JsonRpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }

    // 用于包装 System.Text.Json 的 JsonElement（Newtonsoft 兼容层）
    public class JsonElementWrapper
    {
        public object RawValue { get; set; }
    }

    #endregion

    #region MCP 协议特定类型

    public class InitializeRequestParams
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; set; }

        [JsonProperty("clientInfo")]
        public ClientInfo ClientInfo { get; set; }
    }

    public class ClientCapabilities
    {
        [JsonProperty("sampling")]
        public SamplingCapability Sampling { get; set; }
    }

    public class SamplingCapability
    {
        [JsonProperty("objectPrefixes")]
        public List<string> ObjectPrefixes { get; set; }
    }

    public class ClientInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class InitializeResult
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2025-03-26";

        [JsonProperty("capabilities")]
        public ServerCapabilities Capabilities { get; set; }

        [JsonProperty("serverInfo")]
        public ServerInfoData ServerInfo { get; set; }
    }

    public class ServerCapabilities
    {
        [JsonProperty("tools")]
        public ToolsCapability Tools { get; set; }
    }

    public class ToolsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }

    public class ServerInfoData
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "BridgeDnSpyMCP";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";
    }

    #endregion

    #region Tool 相关

    public class ListToolsResult
    {
        [JsonProperty("tools")]
        public List<Tool> Tools { get; set; }
    }

    public class ContentBase
    {
        [JsonProperty("type")]
        public string Type { get; protected set; }

        protected ContentBase(string type)
        {
            Type = type;
        }
    }

    public class TextContent : ContentBase
    {
        [JsonProperty("text")]
        public string Text { get; private set; }

        public TextContent(string text) : base("text")
        {
            Text = text;
        }
    }

    public class CallToolRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public Dictionary<string, object> Arguments { get; set; }
    }

    public class CallToolResult
    {
        [JsonProperty("content")]
        public List<ContentBase> Content { get; set; }

        [JsonProperty("isError")]
        public bool IsError { get; set; }
    }

    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public InputSchema InputSchema { get; set; }
    }

    public class InputSchema
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "object";

        [JsonProperty("properties")]
        public Dictionary<string, PropertySchema> Properties { get; set; }

        [JsonProperty("required")]
        public List<string> Required { get; set; }
    }

    public class PropertySchema
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("enum")]
        public List<string> EnumValues { get; set; }

        [JsonProperty("items")]
        public PropertySchema Items { get; set; }
    }

    #endregion
}

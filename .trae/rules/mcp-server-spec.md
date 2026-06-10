# MCP Server 官方约定与接口规范

> 基于 MCP 官方规范 (2025-11-25 版本)
> 来源: https://modelcontextprotocol.io/specification/2025-11-25

---

## 一、基础协议约定

### 1. 通信协议
- **消息格式**: JSON-RPC 2.0
- **连接模式**: 有状态连接
- **能力协商**: 服务器和客户端在初始化时协商支持的功能

### 2. 参与者角色

| 角色 | 说明 |
|------|------|
| **Host** (宿主) | LLM 应用程序，发起连接 |
| **Client** (客户端) | 宿主应用内的连接器 |
| **Server** (服务器) | 提供上下文和能力的服务 |

### 3. 传输层
- **stdio**: 本地标准输入/输出（子进程通信）
- **Streamable HTTP**: HTTP 流式传输（远程部署）
- **SSE**: Server-Sent Events（遗留支持）

---

## 二、Server 核心功能（三大原语）

### 控制层级

| 原语 | 控制方式 | 说明 | 示例 |
|------|----------|------|------|
| **Prompts** | 用户控制 | 用户主动选择的交互模板 | 斜杠命令、菜单选项 |
| **Resources** | 应用控制 | 客户端自动管理的上下文数据 | 文件内容、git 历史 |
| **Tools** | 模型控制 | LLM 自动调用的执行函数 | API 请求、文件写入 |

---

## 三、Tools 接口规范

### 1. 能力声明

```json
{
  "capabilities": {
    "tools": {
      "listChanged": true
    }
  }
}
```

`listChanged`: 是否支持工具列表变更通知

### 2. 必须实现的接口

#### (1) `tools/list` - 列出可用工具

**请求:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": { "cursor": "optional-cursor-value" }
}
```

**响应:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "get_weather",
        "title": "Weather Information Provider",
        "description": "Get current weather information for a location",
        "inputSchema": {
          "type": "object",
          "properties": {
            "location": { "type": "string", "description": "City name or zip code" }
          },
          "required": ["location"]
        },
        "icons": [{ "src": "https://example.com/weather-icon.png", "mimeType": "image/png", "sizes": ["48x48"] }],
        "execution": { "taskSupport": "optional" }
      }
    ],
    "nextCursor": "next-page-cursor"
  }
}
```

#### (2) `tools/call` - 调用工具

**请求:**

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "get_weather",
    "arguments": { "location": "New York" }
  }
}
```

**响应:**

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      { "type": "text", "text": "Current weather in New York:\nTemperature: 72°F\nConditions: Partly cloudy" }
    ],
    "isError": false
  }
}
```

支持结构化结果（可选）:

```json
{
  "result": {
    "content": [{ "type": "text", "text": "{\"temperature\": 22.5, \"conditions\": \"Partly cloudy\"}" }],
    "structuredContent": { "temperature": 22.5, "conditions": "Partly cloudy" }
  }
}
```

#### (3) `notifications/tools/list_changed` - 工具列表变更通知（可选）

当声明了 `listChanged` 能力时，服务器 SHOULD 在工具列表变更时发送:

```json
{ "jsonrpc": "2.0", "method": "notifications/tools/list_changed" }
```

### 3. Tool 定义字段规范

| 字段 | 类型 | 必须 | 说明 |
|------|------|------|------|
| `name` | string | **是** | 唯一标识符，见下方命名规则 |
| `title` | string | 否 | 可读名称，用于 UI 显示 |
| `description` | string | 否 | 功能描述，影响 LLM 选择决策 |
| `icons` | array | 否 | 图标数组 `{ src, mimeType, sizes }` |
| `inputSchema` | object | **是** | JSON Schema 参数定义 |
| `outputSchema` | object | 否 | 输出结构 Schema，用于结果验证 |
| `annotations` | object | 否 | 行为注解 |
| `execution` | object | 否 | 执行属性 `{ taskSupport: "forbidden"|"optional"|"required" }` |

### 4. Tool 命名规则

- 长度: **1-128 字符**
- 大小写敏感
- 允许字符: `A-Z a-z 0-9 _ - .`
- 禁止: 空格、逗号、其他特殊字符
- 服务器内必须唯一

**有效示例:** `getUser`, `DATA_EXPORT_v2`, `admin.tools.list`

### 5. inputSchema 规范

- 默认使用 JSON Schema **2020-12**（可通过 `$schema` 字段指定其他版本）
- **MUST** 是有效的 JSON Schema 对象（不能为 null）
- 无参数工具推荐写法: `{ "type": "object", "additionalProperties": false }`

**示例 - 有参数:**

```json
{
  "name": "calculate_sum",
  "description": "Add two numbers",
  "inputSchema": {
    "type": "object",
    "properties": {
      "a": { "type": "number" },
      "b": { "type": "number" }
    },
    "required": ["a", "b"]
  }
}
```

**示例 - 无参数:**

```json
{
  "name": "get_current_time",
  "description": "Returns the current server time",
  "inputSchema": { "type": "object", "additionalProperties": false }
}
```

### 6. Tool Result 内容类型

#### 非结构化内容 (`content` 数组)

```json
// 文本
{ "type": "text", "text": "Tool result text" }

// 图片 (base64)
{ "type": "image", "data": "base64-encoded-data", "mimeType": "image/png" }

// 音频 (base64)
{ "type": "audio", "data": "base64-encoded-data", "mimeType": "audio/wav" }

// 资源链接
{ "type": "resource_link", "uri": "file:///project/src/main.rs", "name": "main.rs", "mimeType": "text/x-rust" }

// 嵌入资源
{ "type": "resource", "resource": { "uri": "...", "mimeType": "...", "text": "..." } }
```

所有内容类型支持可选的 `annotations`:
```json
{ "audience": ["user", "assistant"], "priority": 0.9, "lastModified": "2025-05-03T14:30:00Z" }
```

#### 结构化内容 (`structuredContent`)

返回 JSON 对象。若提供 `outputSchema`，结果 MUST 符合该 Schema。
向后兼容建议: 同时返回序列化后的 TextContent。

### 7. 错误处理

两类错误机制:

| 类型 | 触发场景 | 返回方式 | 示例 |
|------|----------|----------|------|
| **Protocol Error** | 未知工具、格式错误 | JSON-RPC error 字段 | `{ "code": -32602, "message": "Unknown tool" }` |
| **Tool Execution Error** | API 失败、输入验证失败、业务逻辑错误 | result 中 `isError: true` | 见下方 |

**Protocol Error 示例:**
```json
{ "jsonrpc": "2.0", "id": 3, "error": { "code": -32602, "message": "Unknown tool: invalid_tool_name" } }
```

**Tool Execution Error 示例:**
```json
{
  "jsonrpc": "2.0", "id": 4,
  "result": {
    "content": [{ "type": "text", "text": "Invalid date: must be in the future." }],
    "isError": true
  }
}
```

> 输入验证错误 SHOULD 返回为 Tool Execution Error（非 Protocol Error），以便 LLM 自我纠正。

---

## 四、Resources 接口规范

### 1. 能力声明

```json
{
  "capabilities": {
    "resources": {
      "subscribe": true,     // 是否支持资源变更订阅
      "listChanged": true    // 是否支持列表变更通知
    }
  }
}
```

两者均为可选，可单独或组合支持。

### 2. 必须实现的接口

#### (1) `resources/list` - 列出资源

**请求:**

```json
{ "jsonrpc": "2.0", "id": 1, "method": "resources/list", "params": { "cursor": "..." } }
```

**响应:**

```json
{
  "jsonrpc": "2.0", "id": 1,
  "result": {
    "resources": [{
      "uri": "file:///project/src/main.rs",
      "name": "main.rs",
      "title": "Rust Software Application Main File",
      "description": "Primary application entry point",
      "mimeType": "text/x-rust",
      "icons": [{ "src": "...", "mimeType": "image/png", "sizes": ["48x48"] }]
    }],
    "nextCursor": "..."
  }
}
```

#### (2) `resources/read` - 读取资源内容

**请求:**

```json
{ "jsonrpc": "2.0", "id": 2, "method": "resources/read", "params": { "uri": "file:///project/src/main.rs" } }
```

**响应:**

```json
{
  "jsonrpc": "2.0", "id": 2,
  "result": {
    "contents": [{
      "uri": "file:///project/src/main.rs",
      "mimeType": "text/x-rust",
      "text": "fn main() {\n println!(\"Hello world!\");\n}"
    }]
  }
}
```

#### (3) `resources/templates/list` - 列出资源模板

**响应:**

```json
{
  "result": {
    "resourceTemplates": [{
      "uriTemplate": "file:///{path}",
      "name": "Project Files",
      "title": "Project Files",
      "description": "Access files in the project directory",
      "mimeType": "application/octet-stream"
    }]
  }
}
```

#### (4) 订阅相关（可选）

- `resources/subscribe` - 订阅特定资源的变更通知
- `notifications/resources/updated` - 资源更新通知
- `notifications/resources/list_changed` - 资源列表变更通知

### 3. Resource 数据类型

**Resource 定义:**

| 字段 | 类型 | 必须 | 说明 |
|------|------|------|------|
| `uri` | string | **是** | 唯一标识符（URI） |
| `name` | string | **是** | 资源名称 |
| `title` | string | 否 | 可读标题 |
| `description` | string | 否 | 描述 |
| `mimeType` | string | 否 | MIME 类型 |
| `size` | number | 否 | 字节大小 |
| `icons` | array | 否 | 图标数组 |
| `annotations` | object | 否 | 注解 |

**Resource Content 内容类型:**

```json
// 文本内容
{ "uri": "file:///example.txt", "mimeType": "text/plain", "text": "Resource content" }

// 二进制内容
{ "uri": "file:///example.png", "mimeType": "image/png", "blob": "base64-encoded-data" }
```

### 4. 标准 URI Scheme

| Scheme | 用途 | 说明 |
|--------|------|------|
| `https://` | Web 资源 | 仅当客户端能直接获取时使用 |
| `file://` | 文件系统 | 不需映射到物理文件系统 |
| `git://` | Git 版本控制 | Git 集成相关 |
| 自定义 | 特定用途 | MUST 符合 RFC3986 |

### 5. Annotations 注解格式

```json
{
  "audience": ["user"],           // 目标受众: "user" / "assistant"
  "priority": 0.8,                // 重要性: 0.0(最低) ~ 1.0(最高)
  "lastModified": "ISO8601"       // 最后修改时间
}
```

### 6. 错误码

| 场景 | 错误码 |
|------|--------|
| 资源未找到 | `-32002` |
| 内部错误 | `-32603` |

---

## 五、Prompts 接口规范

### 1. 能力声明

```json
{
  "capabilities": {
    "prompts": { "listChanged": true }
  }
}
```

### 2. 必须实现的接口

#### (1) `prompts/list` - 列出提示模板

**请求:**

```json
{ "jsonrpc": "2.0", "id": 1, "method": "prompts/list", "params": { "cursor": "..." } }
```

**响应:**

```json
{
  "jsonrpc": "2.0", "id": 1,
  "result": {
    "prompts": [{
      "name": "code_review",
      "title": "Request Code Review",
      "description": "Asks the LLM to analyze code quality and suggest improvements",
      "arguments": [{ "name": "code", "description": "The code to review", "required": true }],
      "icons": [{ "src": "...", "mimeType": "image/svg+xml", "sizes": ["any"] }]
    }],
    "nextCursor": "..."
  }
}
```

#### (2) `prompts/get` - 获取提示内容

**请求:**

```json
{
  "jsonrpc": "2.0", "id": 2,
  "method": "prompts/get",
  "params": { "name": "code_review", "arguments": { "code": "def hello():\n print('world')" } }
}
```

**响应:**

```json
{
  "jsonrpc": "2.0", "id": 2,
  "result": {
    "description": "Code review prompt",
    "messages": [{
      "role": "user",
      "content": { "type": "text", "text": "Please review this Python code:\ndef hello():\n print('world')" }
    }]
  }
}
```

#### (3) `notifications/prompts/list_changed` - 提示列表变更通知（可选）

### 3. Prompt 定义字段

| 字段 | 类型 | 必须 | 说明 |
|------|------|------|------|
| `name` | string | **是** | 唯一标识符 |
| `title` | string | 否 | 可读名称 |
| `description` | string | 否 | 描述 |
| `arguments` | array | 否 | 参数列表 `[{ name, description, required }]` |
| `icons` | array | 否 | 图标数组 |

### 4. PromptMessage 内容类型

```json
// 文本
{ "type": "text", "text": "..." }

// 图片
{ "type": "image", "data": "base64...", "mimeType": "image/png" }

// 音频
{ "type": "audio", "data": "base64...", "mimeType": "audio/wav" }

// 嵌入资源
{ "type": "resource", "resource": { "uri": "...", "mimeType": "...", "text/blob": "..." } }
```

### 5. 错误码

| 场景 | 错误码 |
|------|--------|
| 无效的提示名 | `-32602` |
| 缺少必需参数 | `-32602` |
| 内部错误 | `-32603` |

---

## 六、通用约定

### 1. 分页 (Pagination)

所有 `*list` 操作均支持分页:

- 请求中传入 `cursor` 参数获取下一页
- 响应中返回 `nextCursor` 表示还有更多数据
- 无 `nextCursor` 或值为 null 表示已到最后

### 2. 补全 (Completion)

Prompt 和 Resource Template 的参数可通过 completion API 自动补全。

### 3. 统一注解 (Annotations)

Tools / Resources / Prompts 及其内容块均支持统一注解格式:

```json
{
  "audience": ["user", "assistant"],
  "priority": 0.0 ~ 1.0,
  "lastModified": "ISO8601 timestamp"
}
```

### 4. Icons 图标格式

Tools / Resources / Prompts / Resource Templates 均支持可选图标:

```json
{
  "src": "https://example.com/icon.png",
  "mimeType": "image/png",
  "sizes": ["48x48"]   // 或 ["any"] 用于 SVG
}
```

---

## 七、安全要求

### Server 端 (MUST)

1. **验证所有输入** - 工具参数、资源 URI、提示参数
2. **实施访问控制** - 敏感操作需权限检查
3. **限制调用频率** - Rate Limiting
4. **清理输出内容** - 防止信息泄露

### Client 端 (SHOULD)

1. 敏感操作需用户确认（人类在环）
2. 显示工具输入供用户审核（防止恶意数据外泄）
3. 验证工具结果后再传给 LLM
4. 设置超时时间
5. 记录使用日志用于审计

---

## 八、生命周期管理

### 初始化握手流程

1. Client 发送 `initialize` 请求，包含客户端能力声明
2. Server 响应 `initialize` 结果，包含服务端能力声明
3. Client 发送 `initialized` 通知（通知完成初始化）

### initialize 请求 (Client -> Server)

```json
{
  "jsonrpc": "2.0", "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-11-25",
    "capabilities": {},
    "clientInfo": { "name": "my-client", "version": "1.0.0" }
  }
}
```

### initialize 响应 (Server -> Client)

```json
{
  "jsonrpc": "2.0", "id": 1,
  "result": {
    "protocolVersion": "2025-11-25",
    "capabilities": { "tools": {}, "resources": {} },
    "serverInfo": { "name": "my-server", "version": "1.0.0" }
  }
}
```

### initialized 通知 (Client -> Server)

```json
{ "jsonrpc": "2.0", "method": "notifications/initialized" }
```

---

## 九、完整 JSON-RPC 方法索引

### Server 必须实现的方法

| 方法 | 方向 | 说明 |
|------|------|------|
| `initialize` | C->S | 初始化握手 |
| `ping` | C->S | 心跳检测 |

### Tools 相关

| 方法 | 方向 | 说明 |
|------|------|------|
| `tools/list` | C->S | 列出可用工具 |
| `tools/call` | C->S | 调用工具 |
| `notifications/tools/list_changed` | S->C | 工具列表变更通知 |

### Resources 相关

| 方法 | 方向 | 说明 |
|------|------|------|
| `resources/list` | C->S | 列出资源 |
| `resources/read` | C->S | 读取资源内容 |
| `resources/templates/list` | C->S | 列出资源模板 |
| `resources/subscribe` | C->S | 订阅资源变更 |
| `notifications/resources/updated` | S->C | 资源更新通知 |
| `notifications/resources/list_changed` | S->C | 资源列表变更通知 |

### Prompts 相关

| 方法 | 方向 | 说明 |
|------|------|------|
| `prompts/list` | C->S | 列出提示模板 |
| `prompts/get` | C->S | 获取提示内容 |
| `notifications/prompts/list_changed` | S->C | 提示列表变更通知 |

### 通用通知

| 方法 | 方向 | 说明 |
|------|------|------|
| `notifications/initialized` | C->S | 初始化完成通知 |
| `notifications/cancelled` | C->S/S->C | 取消请求通知 |
| `notifications/progress` | S->C | 进度通知 |

---

## 十、关键词语义 (RFC2119)

| 关键词 | 含义 |
|--------|------|
| **MUST** | 绝对要求 |
| **MUST NOT** | 绝对禁止 |
| **REQUIRED** | 同 MUST |
| **SHALL** | 同 MUST |
| **SHALL NOT** | 同 MUST NOT |
| **SHOULD** | 强烈推荐 |
| **SHOULD NOT** | 强烈不推荐 |
| **RECOMMENDED** | 推荐 |
| **NOT RECOMMENDED** | 不推荐 |
| **MAY** | 可选 |
| **OPTIONAL** | 同 MAY |

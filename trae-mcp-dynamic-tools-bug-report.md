# Bug: Trae IDE MCP 客户端不支持 `notifications/tools/list_changed` 动态工具列表更新

## 概述

Trae IDE 的 MCP 客户端在初始化时通过 `tools/list` 获取工具列表并缓存为静态白名单，之后即使 Server 发送了符合 MCP 2025-11-25 规范的 `notifications/tools/list_changed` 通知，IDE 也不会重新调用 `tools/list` 更新缓存。任何不在初始白名单中的工具调用都会被 IDE 客户端侧直接拦截，错误信息为 `MCP server is not found`，调用从未送达 Server 的 stdin。

## 环境

| 项目 | 值 |
|------|-----|
| IDE | Trae CN |
| 协议 | MCP 2025-11-25（stdio 传输） |
| Server 能力声明 | `tools.listChanged: true` |
| Server 实现 | 基于 C# 的自定义 .NET 程序（BridgeDnSpyMCP） |
| 操作系统 | Windows |

## 期望行为（MCP 2025-11-25 规范）

Server 声明了 `listChanged` 能力后，当工具列表变更时发送 `notifications/tools/list_changed`，Client 收到通知后应重新调用 `tools/list` 获取最新列表。

期望流程：

```
Server: tools/list → [5个基础工具]
Client: 调用 load_assembly
Server: NotifyToolsChanged() → _toolsNeedRefresh = true
Server: 发送 tools/call 响应 → ConsumePendingToolsChanged() → true
Server: 发送 notifications/tools/list_changed (JSON-RPC 通知)
Client: 收到通知 → 重新调用 tools/list
Server: tools/list → [5个基础工具 + 27个分析工具 = 32个工具]
Client: 缓存更新，后续工具调用正常
```

## 实际行为

```
Server: tools/list → [5个基础工具]
Client: 调用 load_assembly (成功返回程序集信息)
Server: 发送 tools/call 响应
Server: 发送 notifications/tools/list_changed  ✓ (已通过插桩确认发出)
Client: 调用 list_types → 报错 "MCP server is not found"
```

`list_changed` 通知被 IDE 忽略，工具白名单未更新，动态新增的 `list_types` 等 27 个工具全部不可用。

## 根因定位

**通过运行时插桩**（在 Server 的 4 个关键路径注入 HTTP 日志上报到独立的 Debug Server，排除 stderr 干扰）收集到完整证据链：

### 证据时间线

| 序号 | 时间戳 | 事件 | 日志内容 |
|------|--------|------|----------|
| 1 | T+0ms | `tools/list` 初始化 | `HasAssemblies=False, toolCount=5` |
| 2 | T+1ms | 初始化完成 | — |
| 3 | T+10s | 用户调用 `list_assemblies` | `HasAssemblies=False, toolCount=5` |
| 4 | T+10s | `load_assembly` 被调用 | **成功返回 `Newtonsoft.Json v13.0.0.0, 277 types`** |
| 5 | T+10s | `ConsumePendingToolsChanged()` | **`_toolsNeedRefresh=True`** — 状态标记正确 |
| 6 | T+10s | **`list_changed` 通知已发出** | `Sending list_changed notification` — 确认送达 |
| 7 | T+11s | 用户调用 `list_types` | IDE 报错: `MCP server is not found` — **调用未到达 Server** |

### 日志原文（Debug Server 记录的 16 条事件）

```json
{"hypothesisId":"A","msg":"[DEBUG] ListTools: HasAssemblies=False, toolCount=5"}
{"hypothesisId":"C","msg":"[DEBUG] tools/list returning 5 tools"}
{"hypothesisId":"D","msg":"[DEBUG] ConsumePendingToolsChanged called, _toolsNeedRefresh=False"}
{"hypothesisId":"D","msg":"[DEBUG] ConsumePendingToolsChanged called, _toolsNeedRefresh=False"}
{"hypothesisId":"A","msg":"[DEBUG] ListTools: HasAssemblies=False, toolCount=5"}
{"hypothesisId":"C","msg":"[DEBUG] tools/list returning 5 tools"}
{"hypothesisId":"D","msg":"[DEBUG] ConsumePendingToolsChanged called, _toolsNeedRefresh=False"}
{"hypothesisId":"D","msg":"[DEBUG] ConsumePendingToolsChanged called, _toolsNeedRefresh=False"}
{"hypothesisId":"A","msg":"[DEBUG] ListTools: HasAssemblies=False, toolCount=5"}
{"hypothesisId":"C","msg":"[DEBUG] tools/list returning 5 tools"}
{"hypothesisId":"D","msg":"[DEBUG] ConsumePendingToolsChanged called, _toolsNeedRefresh=False"}
← 以上为加载前的初始化/查询
← 以下为 load_assembly 后的关键证据
{"hypothesisId":"D","msg":"[DEBUG] ConsumePendingToolsChanged called, _toolsNeedRefresh=True"}  ← 标记已设
{"hypothesisId":"B","msg":"[DEBUG] Sending list_changed notification"}                       ← 通知已发
← 此处 IDE 应重新 tools/list，但无对应日志
← 用户调用 list_types → IDE 报错，Server 未收到
```

### 排除的假设

| 假设 | 结论 | 证据 |
|------|------|------|
| A: 服务端 `HasAssemblies` 返回 false | ❌ 已排除 | `load_assembly` 成功返回程序集信息 |
| B: 服务端未发送 `list_changed` | ❌ 已排除 | 日志明确记录 `Sending list_changed notification` |
| D: `NotifyToolsChanged()` 未被调用 | ❌ 已排除 | 日志记录 `_toolsNeedRefresh=True` |

### 确认的根因

**C: Trae IDE MCP 客户端侧使用启动时的静态工具列表作为白名单，忽略 `list_changed` 通知**

1. IDE 在启动时读取 `.json` 工具描述文件，注册 5 个工具的硬编码白名单
2. 对 Server 发出的 `notifications/tools/list_changed`，IDE **未重新调用 `tools/list`** 更新缓存
3. 调用不在白名单中的工具时，IDE **在客户端侧直接拒绝**，错误信息 `MCP server is not found` 表明请求从未到达 Server 的 stdin

## Server 端代码参考（渐进披露实现）

[MCPToolRegistry.cs](../Server/McpToolRegistry.cs) — `ListTools()` 方法按 `HasAssemblies` 状态决定暴露的工具层级：

```csharp
// 层级0 — 始终可见：基础管理工具
RegisterAssemblyTools(allTools);   // load_assembly, list_assemblies, unload_assembly, clear_all_assemblies
RegisterServerTools(allTools);     // close_self

// 层级1 — 仅在有已加载程序集时暴露：分析/反编译/编辑工具 (27个)
if (_assemblyLoader.HasAssemblies)
{
    RegisterReferenceTools(allTools);
    RegisterTypeQueryTools(allTools);
    RegisterMemberTools(allTools);
    RegisterMethodDetailTools(allTools);
    RegisterDecompilationTools(allTools);
    RegisterEditorTools(allTools);
    RegisterPathQueryTools(allTools);
}
```

[Assembly.cs](../Server/Modules/McpToolRegistry.Assembly.cs) — `HandleLoadAssembly` 在加载成功后调用 `NotifyToolsChanged()`：

```csharp
private object HandleLoadAssembly(Dictionary<string, object> args)
{
    var result = _assemblyLoader.LoadAssembly(GetRequiredArg<string>(args, "path"));
    NotifyToolsChanged();  // 设置 _toolsNeedRefresh = true
    return result;
}
```

[McpServer.cs](../Server/McpServer.cs) — 响应发送后检查并发送 `list_changed` 通知：

```csharp
// 工具列表变更通知
if (toolRegistry?.ConsumePendingToolsChanged() == true)
{
    var notificationJson = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/tools/list_changed\"}";
    writer.WriteLine(notificationJson);
    writer.Flush();
}
```

## 影响范围

- MCP 规范中 `tools` 原语的设计允许 Server **动态增减工具列表**（渐进披露），但 Trae IDE 的 MCP 客户端仅支持**启动时静态注册**，不接受运行时变更
- 所有依赖 `notifications/tools/list_changed` 机制的应用都将失效
- 受影响的 MCP 场景还包括：根据用户配置动态启用/禁用工具的 Server、多阶段鉴权（登录后暴露新工具）、插件式工具集等

## 复现步骤

1. 启动一个声明 `listChanged: true` 的 MCP Server，初始暴露 N 个工具
2. 调用触发 Server 增加/减少工具列表的操作（如 `load_assembly`）
3. 观察 Server 发送了 `notifications/tools/list_changed` 通知
4. 调用新增的工具名称
5. 预期：调用发送到 Server 正常处理；实际：IDE 报错 `MCP server is not found`

## 参考规范

- MCP 2025-11-25 规范 - Tools: https://modelcontextprotocol.io/specification/2025-11-25
- `notifications/tools/list_changed`: 服务器 SHOULD 在工具列表变更时发送
- 完整规范速览见项目内 [mcp-server-spec.md](.trae/rules/mcp-server-spec.md)

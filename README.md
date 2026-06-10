# DnSpyMCP

> 生成于 GLM-5V-Turbo

基于 [dnSpyEx/dnSpy](https://github.com/dnSpyEx/dnSpy) 的 MCP (Model Context Protocol) 服务器，将 .NET 程序集反编译与分析能力暴露为 AI 可调用的工具接口。

## 已实现功能（Phase 1 - MVP）

共 **16 个工具**，覆盖程序集加载、类型浏览、成员查询、C#/IL 反编译四大类。

### 程序集加载与浏览

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `load_assembly` | 加载 .dll/.exe/.netmodule 程序集到内存 | `path` (string, 必填) |
| `list_assemblies` | 列出当前已加载的所有程序集信息 | 无 |
| `list_types` | 列出已加载程序集中的所有顶层类型，支持按命名空间过滤 | `assembly_path`, `namespace_filter`(可选) |
| `list_namespaces` | 列出已加载程序集中的所有命名空间及其包含的类型数量 | `assembly_path` |
| `find_type` | 按名称或全限定名搜索类型，支持模糊匹配 | `assembly_path`, `query` |
| `get_type_info` | 获取类型的详细信息：基类、接口、成员统计、修饰符等 | `assembly_path`, `full_type_name` |

### 成员查询

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `list_methods` | 列出类型的所有方法（含签名、返回值、可见性） | `assembly_path`, `full_type_name` |
| `list_fields` | 列出类型的所有字段（含类型、常量值、可见性） | `assembly_path`, `full_type_name` |
| `list_properties` | 列出类型的所有属性（含类型、getter/setter 可见性） | `assembly_path`, `full_type_name` |
| `list_events` | 列出类型的所有事件 | `assembly_path`, `full_type_name` |
| `get_method_info` | 获取方法的完整签名与元数据信息 | `assembly_path`, `full_type_name`, `method_name` |
| `get_method_il` | 获取方法的 IL 指令列表（偏移量、操作码、操作数） | `assembly_path`, `full_type_name`, `method_name` |

### 反编译输出

| 工具名 | 功能 | 输出格式 |
|--------|------|----------|
| `decompile_type` | 将指定类型反编译为 C# 源码文本 | C# |
| `decompile_method` | 将单个方法反编译为 C# 源码 | C# |
| `decompile_assembly` | 反编译整个程序集中指定命名空间下的所有类型 | C#（多类型合并） |
| `decompile_to_il` | 将方法输出为 ILASM 格式中间语言文本 | ILASM |

## 技术架构

```
DnSpyMCP Server (.NET Framework 4.8, C#)
  |
  +-- 核心依赖（来自 dnSpy 安装目录）
  |     |-- dnlib.dll              -- .NET 元数据读写
  |     |-- dnSpy.Contracts*.dll   -- dnSpy 契约接口
  |     |-- ILSpy.*.dll            -- 反编译引擎
  |     |-- System.Composition.dll -- MEF v2 容器
  |     |-- Newtonsoft.Json.dll    -- JSON 序列化
  |
  +-- 服务层
  |     |-- AssemblyLoaderService      -- 程序集加载与管理 (dnlib)
  |     |-- MetadataBrowserService     -- 类型/成员枚举与查询
  |     |-- DecompilationService       -- 基于 MEF 发现 IDecompiler 的反编译服务
  |
  +-- MCP 协议层
  |     |-- McpToolRegistry           -- 16 个工具的注册与调用分发
  |     |-- McpServer                 -- stdio NDJSON JSON-RPC 消息循环
  |     |-- McpTypes                  -- 协议类型定义
```

## 项目结构

```
DnSpyMCP/
  DnSpyMCP.csproj              -- .NET Framework 4.8 项目文件
  path.props / path.example.props  -- 配置 dnSpy 安装路径引用
  Program.cs                   -- 入口点，组装服务并启动 MCP stdio 循环
  Models/
    Models.cs                 -- 数据模型（AssemblyInfo, TypeInfo, MethodInfo 等）
  Services/
    AssemblyLoaderService.cs      -- dnlib 程序集加载与管理
    MetadataBrowserService.cs     -- 类型/方法/字段/属性/事件枚举与查询
    DecompilationService.cs       -- 基于 dnSpy IDecompiler + MEF 的反编译服务
  Server/
    Protocol/McpTypes.cs          -- JSON-RPC / MCP 协议类型定义
    McpToolRegistry.cs           -- 工具注册与调用分发
    McpServer.cs                 -- stdio NDJSON JSON-RPC 消息循环
```

## 构建与运行

### 前置条件

- .NET Framework 4.8 运行时/SDK
- [dnSpyEx](https://github.com/dnSpyEx/dnSpy) 安装（用于获取 DLL 引用）
- Visual Studio 或 MSBuild

### 构建步骤

1. 复制 `path.example.props` 为 `path.props`，修改其中的 `DnSpyPath` 指向你的 dnSpy 安装目录：

```xml
<!-- path.props -->
<Project>
  <PropertyGroup>
    <DnSpyPath>your-dnspy-install-path</DnSpyPath>
  </PropertyGroup>
</Project>
```

2. 构建：

```bash
dotnet build DnSpyMCP.csproj
```

3. 编译产物位于 `bin\Debug\DnSpyMCP.exe`。

### 作为 MCP 服务器使用

将 `bin\Debug\DnSpyMCP.exe` 配置为 MCP 客户端的 stdio 类型服务器即可。例如在 Trae IDE 中配置：

```json
{
  "mcpServers": {
    "dnspy-mcp": {
      "command": "your-dnspy-install-path\\bin\\DnSpyMCP.exe",
      "args": []
    }
  }
}
```

> 注意：需确保 `DnSpyMCP.exe` 与 dnSpy 的 DLL 文件在同一目录下（或通过 `path.props` 配置的路径可找到所有依赖）。

## 设计决策

| 决策 | 说明 |
|------|------|
| 目标框架 | .NET Framework 4.8，零额外 NuGet 依赖，普通电脑可直接运行 |
| 反编译方式 | 通过 MEF 容器从 dnSpy DLL 中发现和创建 `IDecompiler` 实例（dnSpy 的反编译器实现均为 internal） |
| 输出目标 | 使用 `StringBuilderDecompilerOutput`（dnSpy 自带的公开 headless 输出类） |
| JSON 序列化 | 使用 Newtonsoft.Json（dnSpy 自带） |
| 传输协议 | stdio NDJSON（换行分隔 JSON），UTF-8 无 BOM |
| 协议版本 | MCP 2025-11-25 |

## 待实现功能

详见 [TODO.md](TODO.md)，主要包括：

- **Phase 2**：程序集编辑能力（重命名、增删成员、IL 编辑、保存重签名）
- **Phase 3**：高级分析（调用图、依赖分析、资源操作、混淆检测）
- **Phase 4**：调试器（启动/附加调试、断点管理、变量查看，复杂度高）

## 参考资源

- [dnSpy 仓库](https://github.com/dnSpyEx/dnSpy)
- [dnlib (底层元数据库)](https://github.com/0xd4d/dnlib)
- [ILSpy (反编译引擎)](https://github.com/icsharpcode/ILSpy)
- [MCP 官方规范](https://modelcontextprotocol.io/specification/2025-11-25)

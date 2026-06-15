# BridgeDnSpyMCP

> 生成于 GLM-5V-Turbo

基于 [dnSpyEx/dnSpy](https://github.com/dnSpyEx/dnSpy) 的 MCP (Model Context Protocol) 服务器，将 .NET 程序集反编译与分析能力暴露为 AI 可调用的工具接口。

## 已实现功能（Phase 1 + 引用查找）

共 **20 个工具**（正常模式 19 个 + Setup 模式 1 个），覆盖程序集加载管理、引用查找、类型浏览、成员查询、C#/IL 反编译、服务器管理六大类。

### 程序集加载与管理

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `load_assembly` | 加载 .dll/.exe/.netmodule 程序集到内存 | `path` |
| `list_assemblies` | 列出当前已加载的所有程序集信息 | 无 |
| `unload_assembly` | 移除单个已加载的程序集，释放资源 | `path` |
| `clear_all_assemblies` | 清空所有已加载的程序集，释放全部资源 | 无 |

### 引用查找

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `find_references` | 查找某个成员（方法/字段/属性/事件）在程序集中的所有引用位置。返回包含类型、方法、IL 偏移量等信息的列表 | `assembly_path`, `full_type_name`, `member_name` |
| `find_all_string_refs` | 查找程序集中包含指定字符串的 ldstr 指令位置，用于定位硬编码字符串的使用处 | `assembly_path`, `search_string` |

### 类型浏览与查询

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `list_types` | 列出已加载程序集中的所有顶层类型，支持按命名空间过滤 | `assembly_path`, `namespace_filter` (可选) |
| `list_namespaces` | 列出已加载程序集中的所有命名空间及其包含的类型数量 | `assembly_path` |
| `find_type` | 按名称或全限定名搜索类型，支持模糊匹配 | `assembly_path`, `query` |
| `get_type_info` | 获取类型的详细信息：基类、接口、成员统计、修饰符等 | `assembly_path`, `full_type_name` |

### 成员枚举与查询

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `list_methods` | 列出类型的所有方法（含签名、返回值、可见性等） | `assembly_path`, `full_type_name` |
| `list_fields` | 列出类型的所有字段（含类型、常量值、可见性等） | `assembly_path`, `full_type_name` |
| `list_properties` | 列出类型的所有属性（含类型、getter/setter 可见性等） | `assembly_path`, `full_type_name` |
| `list_events` | 列出类型的所有事件 | `assembly_path`, `full_type_name` |
| `get_method_info` | 获取单个方法的完整签名和元数据信息 | `assembly_path`, `full_type_name`, `method_name` |
| `get_method_il` | 获取方法的 IL 指令列表（偏移量、操作码、操作数） | `assembly_path`, `full_type_name`, `method_name` |

### 反编译输出

| 工具名 | 功能 | 输出格式 |
|--------|------|----------|
| `decompile_type` | 将指定类型反编译为 C# 源码文本 | C# |
| `decompile_method` | 将单个方法反编译为 C# 源码 | C# |
| `decompile_assembly` | 反编译整个程序集中指定命名空间下的所有类型。结果以 JSON 对象返回（key=类型全名, value=C#代码） | C#（多类型合并） |
| `decompile_to_il` | 将指定方法输出为 ILASM 格式的中间语言文本（含 .method 声明、.maxstack、指令列表） | ILASM |

### 服务器管理

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `close_self` | 优雅关闭 MCP 服务器进程 | 无 |

### Setup 模式（仅自检未通过时可用）

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `configure_dnspy_path` | 配置 dnSpy 安装路径并自动执行自检。通过后需重启服务器以激活完整功能 | `path` |

## 技术架构

```
BridgeDnSpyMCP Server (.NET Framework 4.8, C#)
  |
  +-- 核心依赖（运行时从 dnSpy 安装目录远程加载，不拷贝本地）
  |     |-- dnlib.dll              -- .NET 元数据读写
  |     |-- dnSpy.Contracts*.dll   -- dnSpy 契约接口
  |     |-- dnSpy.Analyzer.x       -- 引用分析引擎（通过反射调用 internal 类型）
  |     |-- ILSpy.*.dll            -- 反编译引擎
  |     |-- System.Composition.dll -- MEF v2 容器
  |     |-- Newtonsoft.Json.dll    -- JSON 序列化
  |
  +-- 入口与基础设施
  |     |-- Program.cs                 -- 入口点，初始化配置 -> 依赖解析 -> 自检 -> 组装服务 -> 启动 MCP 循环
  |     |-- ConfigManager.cs           -- bdsm.ini 配置文件管理（读/写/默认值/损坏恢复）
  |     |-- DnSpyDependencyResolver.cs -- DLL 远程加载（AssemblyResolve）、自检机制、Setup 模式切换
  |
  +-- 服务层
  |     |-- AssemblyLoaderService      -- 程序集加载/卸载/枚举 (dnlib)
  |     |-- ReferenceFinderService     -- 引用查找（委托 dnSpy ScopedWhereUsedAnalyzer + PLINQ 并行扫描）
  |     |-- MetadataBrowserService     -- 类型/成员枚举与查询（聚合 AssemblyLoader + ReferenceFinder）
  |     |-- DecompilationService       -- 基于 MEF 发现 IDecompiler 的反编译服务（C#/IL）
  |     |-- DnSpyUtils.cs              -- 公共工具方法（编译器生成判断、IL 操作数格式化）
  |
  +-- 数据模型
  |     |-- Models/Models.cs           -- AssemblyInfo, TypeInfo, MethodInfo, ReferenceInfo 等
  |
  +-- MCP 协议层
  |     |-- Server/McpToolRegistry.cs          -- 工具注册表核心：字段、构造函数、ListTools/CallTool 分发、辅助方法
  |     |-- Server/Modules/McpToolRegistry.*.cs -- 按功能模块拆分的 partial class（定义+处理+分发器注册）
  |     |-- Server/McpServer.cs                -- stdio NDJSON JSON-RPC 消息循环
  |     |-- Server/Protocol/McpTypes.cs         -- 协议类型定义（Tool, InputSchema, ContentBase 等）
```

## 构建与运行

### 前置条件

- .NET Framework 4.8 运行时/SDK
- [dnSpyEx](https://github.com/dnSpyEx/dnSpy) 安装（用于获取 DLL 引用）

### 构建步骤

1. 复制 `path.example.props` 为 `path.props`，修改其中的 `DnSpyPath` 指向你的 dnSpy 安装目录：

```xml
<!-- path.props -->
<Project>
  <PropertyGroup>
    <DnSpyPath>C:/dnSpy</DnSpyPath>
  </PropertyGroup>
</Project>
```

2. 构建：

```bash
dotnet build
```

3. 编译产物位于 `bin\Debug\BridgeDnSpyMCP.exe`。

### 作为 MCP 服务器使用

#### 首次启动（自动进入 Setup 模式）

首次启动时，如果 `bdsm.ini` 中配置的 dnSpy 路径无效或不存在，服务器会**自动进入 Setup 模式**：

- 仅暴露 `configure_dnspy_path` 一个工具
- 通过该工具设置正确的 dnSpy 安装路径后，服务器会自动执行自检
- 自检通过后，**重启 MCP 服务器**即可激活全部 19 个工具

#### 正常模式

自检通过后，服务器以正常模式运行，暴露全部分析/反编译工具。

将 `BridgeDnSpyMCP.exe` 配置为 MCP 客户端的 stdio 类型服务器即可。例如在 Trae IDE 中配置：

```json
{
  "mcpServers": {
    "dnspy-mcp": {
      // "cwd": "path\\to\\folder"
      "command": "path\\to\\BridgeDnSpyMCP.exe",
      "args": []
    }
  }
}
```

#### 运行时配置说明

服务器通过 exe 同目录下的 `bdsm.ini` 文件管理配置：

```ini
; BDSM Configuration

DnSpyPath=C:/dnSpy
```

- 首次启动若不存在此文件，服务器会自动生成含默认值的 `bdsm.ini`
- 也可在运行中通过 `configure_dnspy_path` 工具动态修改
- 修改路径后需重启服务器使新配置生效

> 注意：`BridgeDnSpyMCP.exe` 无需与 dnSpy 的 DLL 放在同一目录。DLL 在运行时通过 `AppDomain.AssemblyResolve` 从 dnSpy 安装目录远程加载。

## 设计决策

| 决策 | 说明 |
|------|------|
| 目标框架 | .NET Framework 4.8，零 NuGet 依赖（含构建期），普通 Windows 电脑可直接运行 |
| 依赖加载方式 | 运行时通过 `AssemblyResolve` 从 dnSpy 安装目录远程加载 DLL，不拷贝不枚举，支持 dnSpy 根目录和 `bin\` 子目录两级搜索 |
| internal 类型访问 | 通过反射 + Lazy 缓存调用 `dnSpy.Analyzer.x` 的 `ScopedWhereUsedAnalyzer<T>` 等内部类型，构造签名经 `dnspy-console` 反编译确认 |
| 自检与 Setup 模式 | 启动时尝试加载 dnSpy.Console.exe 验证依赖链完整性；失败则降级为仅暴露配置工具的 Setup 模式，让 MCP agent 自助修复路径 |
| 反编译方式 | 直接使用 ILSpy 公开 API：`AstBuilder`（C#）与 `ReflectionDisassembler`（IL），无需经过 internal 的 `IDecompiler`/`CSharpDecompiler` |
| 输出目标 | 使用自定义 `PlainTextOutput : IDecompilerOutput`，将反编译结果收集到 StringBuilder，忽略 IDE 特有的颜色/引用标记 |
| JSON 序列化 | 使用 Newtonsoft.Json（dnSpy 自带），CamelCase 策略 |
| 传输协议 | stdio NDJSON（换行分隔 JSON），UTF-8 无 BOM |
| 协议版本 | MCP 2025-11-25 |
| 配置管理 | 使用自定义 INI 解析器（`bdsm.ini`），无需额外依赖；读取失败时自动删除损坏文件并重建 |

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

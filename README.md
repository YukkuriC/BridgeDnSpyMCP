# BridgeDnSpyMCP

> 生成于 GLM-5V-Turbo

基于 [dnSpyEx/dnSpy](https://github.com/dnSpyEx/dnSpy) 的 MCP (Model Context Protocol) 服务器，将 .NET 程序集反编译与分析能力暴露为 AI 可调用的工具接口。

## 已实现功能

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
| `list_members` | 列出指定类型的成员。通过 `op` 参数指定成员类型：methods（方法）、fields（字段）、properties（属性）、events（事件） | `assembly_path`, `full_type_name`, `op` |
| `get_method_info` | 获取单个方法的完整签名和元数据信息 | `assembly_path`, `full_type_name`, `method_name` |
| `get_method_il` | 获取方法的 IL 指令列表（偏移量、操作码、操作数） | `assembly_path`, `full_type_name`, `method_name` |

### 反编译输出

| 工具名 | 功能 | 输出格式 |
|--------|------|----------|
| `decompile_type` | 将指定类型反编译为 C# 源码文本 | C# |
| `decompile_method` | 将单个方法反编译为 C# 源码 | C# |
| `decompile_assembly` | 反编译整个程序集中指定命名空间下的所有类型。结果以 JSON 对象返回（key=类型全名, value=C#代码） | C#（多类型合并） |
| `decompile_to_il` | 将指定方法输出为 ILASM 格式的中间语言文本（含 .method 声明、.maxstack、指令列表） | ILASM |

### 程序集编辑

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `rename_type` | 重命名类型 | `assembly_path`, `full_type_name`, `new_name` |
| `rename_type_namespace` | 修改类型的命名空间（仅非嵌套类型） | `assembly_path`, `full_type_name`, `new_namespace` |
| `rename_method` | 重命名方法 | `assembly_path`, `full_type_name`, `method_name`, `new_name` |
| `rename_field` | 重命名字段 | `assembly_path`, `full_type_name`, `field_name`, `new_name` |
| `rename_property` | 重命名属性 | `assembly_path`, `full_type_name`, `property_name`, `new_name` |
| `rename_event` | 重命名事件 | `assembly_path`, `full_type_name`, `event_name`, `new_name` |
| `add_field` | 添加字段到指定类型 | `assembly_path`, `full_type_name`, `field_name`, `field_type`(可选), `is_static`(可选), `is_public`(可选) |
| `add_method` | 添加空方法到指定类型（仅含 ret） | `assembly_path`, `full_type_name`, `method_name`, `param_count`(可选), `is_static`(可选), `is_public`(可选) |
| `remove_member` | 删除成员（方法/字段/属性/事件） | `assembly_path`, `full_type_name`, `member_name`, `member_type`(可选, 默认 method) |
| `edit_method_il` | 替换方法的 IL 指令（支持范围）。不提供 start_offset/end_offset 替换整个方法体；仅 start_offset 从该偏移到末尾；两者都提供替换 [start_offset, end_offset) 区间。每行一条 "OpCode Operand" | `assembly_path`, `full_type_name`, `method_name`, `instructions`(字符串数组), `start_offset`(可选), `end_offset`(可选) |
| `insert_il_instruction` | 在指定偏移位置插入 IL 指令（支持批量）。提供 instruction 单条；提供 instructions 数组批量插入 | `assembly_path`, `full_type_name`, `method_name`, `offset`, `instruction`(与 instructions 二选一), `instructions`(数组, 与 instruction 二选一) |
| `remove_il_instruction` | 删除 IL 指令（支持范围/批量/单条）。offset+end_offset 删除 [offset, end_offset) 范围；offsets 数组删除多个指定偏移；仅 offset 删除单条 | `assembly_path`, `full_type_name`, `method_name`, `offset`(与 end_offset/offsets 配合时可选), `end_offset`(可选), `offsets`(数组, 可选) |
| `save_assembly` | 将修改后的程序集保存到新路径（不覆盖原文件） | `assembly_path`, `output_path` |
| `change_type_visibility` | 修改类型的访问修饰符（非嵌套：public/internal；嵌套：6种） | `assembly_path`, `full_type_name`, `visibility` |
| `change_method_visibility` | 修改方法的访问修饰符（public/private/protected/internal/protected_internal/private_protected） | `assembly_path`, `full_type_name`, `method_name`, `visibility` |
| `custom_attribute_op` | 自定义特性操作。通过 `op` 参数指定操作方向：add（添加）/ remove（删除）。支持构造函数参数 + 命名参数，CorLib 类型自动回退 | `op`, `assembly_path`, `full_type_name`, `member_name`, `member_type`, `attribute_type_name`, `constructor_args`(可选), `named_args`(可选) |

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
  |     |-- DecompilationService       -- 反编译服务：AstBuilder(C#) + ReflectionDisassembler(IL)
  |     |-- AssemblyEditorService      -- 程序集编辑（重命名、命名空间修改、增删成员、IL 编辑、保存）(dnlib) |     |-- DnSpyUtils.cs              -- 公共工具方法（编译器生成判断、IL 操作数格式化）
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
- 自检通过后，**重启 MCP 服务器**即可激活全部工具

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

- **Phase 3**：高级分析（调用图、依赖分析、资源操作、混淆检测）
- **Phase 4**：调试器（启动/附加调试、断点管理、变量查看，复杂度高）

## 参考资源

- [dnSpy 仓库](https://github.com/dnSpyEx/dnSpy)
- [dnlib (底层元数据库)](https://github.com/0xd4d/dnlib)
- [ILSpy (反编译引擎)](https://github.com/icsharpcode/ILSpy)
- [MCP 官方规范](https://modelcontextprotocol.io/specification/2025-11-25)

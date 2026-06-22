# BridgeDnSpyMCP - 基于 dnSpy 的 MCP 服务器功能规划

> 生成于 GLM-5V-Turbo

## 背景

[dnSpyEx/dnSpy](https://github.com/dnSpyEx/dnSpy) 是 .NET 生态中最强大的开源反编译器、调试器与程序集编辑器。本规划分析如何将其核心能力通过 **MCP (Model Context Protocol)** 协议暴露为 AI 可调用的工具接口。

**重要前提**: dnSpy 本身是 GUI 应用，无原生 headless/CLI 模式。MCP 服务器需引用 dnSpy 的底层库（dnlib、ICSharpCode.Decompiler、Roslyn 等）构建新的宿主程序。

---

## 程序集编辑类工具

### 2. 结构编辑

| 工具名 | 功能 | 对应 dnSpy API | 优先级 |
|--------|------|---------------|--------|
| `add_type` | 向程序集添加新类型定义 | `ModuleDef.Types.Add(TypeDef)` | P2 |
| `add_assembly_reference` | 添加程序集引用 | `AssemblyRefUser` | P2 |

### 3. IL 编辑（高级）

> 以下两个工具为语法糖，均可用已实现的 `edit_method_il` / `insert_il_instruction` 等基础工具等价实现，优先级可延后。

| 工具名 | 功能 | 对应 dnSpy API | 优先级 |
|--------|------|---------------|--------|
| `inject_method_call` | 在方法开头/结尾注入方法调用 | 插入 IL Instruction | P3（等价于 `insert_il_instruction` 批量插入） |
| `nop_method_body` | 清空方法体（设为 throw 或 nop） | 替换 Instructions | P3（等价于 `edit_method_il` 整体替换） |

### 4. 保存与导出

| 工具名 | 功能 | 对应 dnSpy API | 优先级 |
|--------|------|---------------|--------|
| `save_assembly_with_resign` | 保存并重新签名强命名程序集 | `ModuleWriter` + 强命名重签 | P2 |
| `export_to_project` | 导出为 Visual Studio 解决方案/项目 | `ProjectExporter` | P2 |
| `read_resource` | 读取程序集的嵌入式资源（图标/字符串表等） | `IResource` / `Resource.SetData()` | P2 |
| `replace_resource` | 替换嵌入式资源 | P2 |

---

## 调试器类工具

| 工具名 | 功能 | 对应 dnSpy API | 优先级 |
|--------|------|---------------|--------|
| `debug_launch` | 启动目标 .NET 程序并进入调试会话 | `IDbManager.Launch()` | P2 |
| `debug_attach` | 附加到正在运行的 .NET 进程 | `IDbManager.Attach()` | P2 |
| `debug_detach` | 分离调试器 | `IDbManager.Detach()` | P2 |
| `debug_set_breakpoint` | 在指定方法/地址设置断点 | `IBreakpointsService.Create()` | P2 |
| `debug_remove_breakpoint` | 删除断点 | `IBreakpoint.Remove()` | P2 |
| `debug_list_breakpoints` | 列出所有断点及状态 | `IBreakpointsService` | P2 |
| `debug_continue` | 继续执行（F5） | `IProcess.Continue()` | P2 |
| `debug_step_over` | 单步跳过（F10） | `IThread.Step(StepKind.Over)` | P2 |
| `debug_step_into` | 单步进入（F11） | `IThread.Step(StepKind.Into)` | P2 |
| `debug_step_out` | 单步跳出（Shift+F11） | `IThread.StepOut()` | P2 |
| `debug_pause` | 中断执行（暂停全部线程） | `IProcess.Break()` | P2 |
| `debug_get_callstack` | 获取当前线程的调用栈 | `IThread.CallStack` | P2 |
| `debug_get_threads` | 列出所有托管线程 | `IProcess.Threads` | P2 |
| `debug_get_locals` | 获取当前栈帧的局部变量 | `IStackFrame.GetLocals()` | P2 |
| `debug_get_arguments` | 获取当前方法的参数值 | `IStackFrame.GetArguments()` | P2 |
| `debug_evaluate_expression` | 计算表达式求值（即时窗口能力） | `IEvaluationService.Evaluate()` | P2 |
| `debug_set_variable` | 修改变量/参数的值 | `IValue.SetValue()` | P2 |
| `debug_get_modules` | 列出已加载模块及符号状态 | `IProcess.Modules` | P2 |
| `debug_watch_expression` | 设置监视表达式并获取值 | `IWatchService` | P3 |

---

## 高级分析类工具

| 工具名 | 功能 | 实现思路 | 优先级 |
|--------|------|---------|--------|
| `analyze_call_graph` | 分析方法的完整调用图（上下游调用链） | 遍历 IL Instructions 构建有向图 | P2 |
| `analyze_inheritance_chain` | 分析类型的完整继承链 | `TypeDef.BaseType` 递归遍历 | P2 |
| `analyze_dependencies` | 分析程序集的外部依赖关系 | 遍历 `AssemblyRefs` | P2 |
| `compare_assemblies` | 比较两个版本程序集的差异 | 逐成员对比元数据 + IL diff | P3 |
| `search_il_pattern` | 按 IL 指令模式搜索（如查找所有 callvirt） | IL Instruction 模式匹配 | P2 |
| `deobfuscate_analysis` | 基础混淆分析（检测常见混淆特征） | 启发式规则匹配 | P3 |
| `generate_documentation` | 从程序集生成 API 文档注释 | 结合 XML Doc + 反编译结果 | P3 |

---

## 技术架构

> 采用轻量级 headless 方案，详见 [README.md 技术架构](../README.md#L68-L101)。
> 以下仅记录已确定方案中**尚未实现**的能力。

### 待实现能力

（当前无未实现的 P1 能力。P2/P3 详见上方工具表。）

## 关键技术依赖

| 库 | 版本 | 用途 | 许可证 |
|----|------|------|--------|
| **dnlib** | v4.x | .NET 元数据读写核心 | MIT |
| **ICSharpCode.Decompiler** | v8.x | ILSpy 反编译引擎 | MIT |
| **Microsoft.CodeAnalysis** | v4.x | Roslyn 编译器平台 | MIT |
| **System.Composition.Hosting** | v8.x | MEF v2 轻量容器 | MIT |
| **Microsoft.Diagnostics.Runtime** (ClrMD) | v2.x | CLR 运行时诊断（调试器用） | MIT |

---

## 实施路线图建议

### Phase 1 - MVP（反编译 + 浏览 + 编辑） -- 已完成，详见 [README.md](README.md)

### Phase 3 - 高级分析与扩展
- [ ] 实现 `read_resource` / `replace_resource` -- 资源操作
- [ ] 实现 `analyze_call_graph` / `analyze_dependencies` -- 调用图与依赖分析
- [ ] 实现 `export_to_project` -- VS 项目导出
- [ ] StreamableHTTP 传输支持

### Phase 4 - 调试器（可选，高复杂度）
- [ ] 调试器运行时初始化（ClrMD / DAC）
- [ ] 实现 `debug_launch` / `debug_attach` / `debug_detach`
- [ ] 实现断点管理与执行控制
- [ ] 实现变量查看与表达式求值
- [ ] 调试会话状态管理与并发安全

---

## 参考资源

- **dnSpy 仓库**: https://github.com/dnSpyEx/dnSpy
- **dnlib (底层元数据库)**: https://github.com/0xd4d/dnlib
- **ILSpy (反编译引擎)**: https://github.com/icsharpcode/ILSpy
- **MCP SDK (.NET)**: https://modelcontextprotocol.io
- **ClrMD (CLR 诊断运行时)**: https://github.com/microsoft/clrmd

---
alwaysApply: false
description: 实现新功能时适用
---
# 代码复用规则

## 适用范围

实现新功能时适用。

## 原则

优先复用 dnSpy 已有实现，在合理范围内追求代码量最小化。

## 复用方式

- **公开成员**：直接调用。
- **internal / private 成员**：通过 **反射 + Lazy 缓存** 访问，构造签名必须经 `dnspy-console` 反编译确认，禁止自行重新实现或猜测签名。
  若公开 API 与反射均可实现且复杂度相当，优先选用公开 API。

## 探索方式

通过 dnSpy MCP 已有工具探索目标程序集。若当前 MCP 缺少所需探索能力（需新增工具才能完成），暂停任务并请求用户重新编译、重启 MCP 后再继续。

---

# 反射缓存规范

> 基于 ReferenceFinderService.AnalyzerReflection 正面模板提炼
> 来源: Services/ReferenceFinderService.cs (AnalyzerReflection 静态类)

## 核心原则

**反射结果必须缓存，禁止在热路径（循环/频繁调用）中重复执行反射查找。**

## 模式

| 场景 | 容器 | 示意图 |
|------|------|--------|
| 单次加载、全局唯一（Type / MethodInfo） | `Lazy<T>` | `static readonly Lazy<Type> _t = new(() => ...)` |
| 按键多实例（跨泛型/跨类型/按需查找） | `ConcurrentDictionary<K, V>` | miss 时 `GetOrAdd` 或先 `TryGetValue` 再反射+写入 |
| 构建后不变的只读映射表 | `Dictionary<K, V>` | 按需填充（miss 时反射并写入） |

**一律按需填充**（miss 时才反射并缓存），不预建。

## 禁止事项

- **禁止**在循环体或每请求路径中执行 `GetType()` / `GetField()` / `GetProperty()` / `GetMethod()` 而不缓存结果
- **禁止**对同一 Type 重复调用 `MakeGenericType()` / `GetConstructors()` / `GetMethods()`
- **例外**：低频操作（如每次保存仅调用一次的反射）可豁免，但应在代码注释中标明原因

// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using BDSM.Models;

namespace BDSM.Services
{
    /// <summary>
    /// 路径式成员解析服务。
    /// 接收点分路径（如 System.IO.File.Exists），在已加载程序集中通过 BFS 多路径并行追踪，
    /// 歧义时分叉继续解析直到路径终点，一次性返回所有完整匹配结果。
    /// </summary>
    public class PathQueryService
    {
        readonly AssemblyLoaderService _loader;

        public PathQueryService(AssemblyLoaderService loader)
        {
            _loader = loader;
        }

        // ===== 公开接口 =====

        /// <summary>
        /// 解析点分路径，返回匹配结果。
        /// 采用 BFS 多路径并行追踪：歧义时分叉而非中断，确保一次调用解到终点。
        /// </summary>
        /// <param name="path">点分路径，如 "System.IO.File.Exists" 或 "MyClass.SomeField"</param>
        /// <param name="assemblyPath">可选，限定搜索范围到指定程序集</param>
        /// <param name="searchInheritance">成员未命中时是否沿基类/接口搜索，默认 false</param>
        public PathQueryResult Resolve(string path, string assemblyPath = null,
            bool searchInheritance = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new UserException("Path must not be empty.");

            var segments = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new UserException("Path must not be empty.");

            var modules = GetSearchModules(assemblyPath);
            var maxPerLayer = modules.Count * 30;

            // 初始层：每个模块一个空命名空间状态
            var currentLayer = new List<PathState>();
            foreach (var mod in modules)
                currentLayer.Add(new PathState { Module = mod });

            // 逐段 BFS 推进
            for (int segIdx = 0; segIdx < segments.Length; segIdx++)
            {
                var segment = segments[segIdx];
                bool isLast = (segIdx == segments.Length - 1);
                var nextLayer = new List<PathState>();

                foreach (var state in currentLayer)
                    Advance(state, segment, isLast, searchInheritance, nextLayer);

                // 去重（同一类型+同一路径语义的去重）
                nextLayer = DeduplicateStates(nextLayer);

                if (nextLayer.Count == 0)
                {
                    // 全部路径在此段断掉，返回最近的有效候选或未匹配
                    return BuildTerminalResult(currentLayer, path, allSegmentsConsumed: false);
                }

                if (nextLayer.Count > maxPerLayer)
                {
                    throw new UserException(string.Format(
                        "Path ambiguity too large at segment '{0}': {1} active paths exceed limit ({2}). " +
                        "Please provide a more specific path.",
                        segment, nextLayer.Count, maxPerLayer));
                }

                currentLayer = nextLayer;
            }

            // 所有段消耗完毕，收集终态
            return BuildTerminalResult(currentLayer, path, allSegmentsConsumed: true);
        }

        // ===== BFS 路径状态 =====

        class PathState
        {
            public ModuleDefMD Module;              // 所属模块
            public List<string> NsParts;             // 已确认的命名空间段
            public TypeDef Type;                     // 已匹配到的类型（null = 仍在命名空间阶段）
            public int TypeMatchedAtSegment;          // 类型匹配发生在第几段
            public string MatchKind;                 // 终态时: "type" | "field" | "property" | "method" | "event"
            public object Target;                    // 终态时的数据模型

            public bool IsTerminal => Target != null;
            // 只有命中成员(Target!=0)才算终态；停在类型上的状态由 BuildTerminalResult 归入断点
        }

        // ===== 单步推进：将一个状态按当前段推进，产出 0~N 个后续状态 =====

        void Advance(PathState state, string segment, bool isLast,
            bool searchInheritance, List<PathState> output)
        {
            if (state.Type == null)
                AdvanceInNamespace(state, segment, output);
            else
                AdvanceInMember(state, segment, isLast, searchInheritance, output);
        }

        void AdvanceInNamespace(PathState state, string segment,
            List<PathState> output)
        {
            var nsParts = state.NsParts ?? new List<string>();
            var candidateNs = nsParts.Count > 0 ? string.Join(".", nsParts) + "." + segment : segment;
            var fullTypeName = candidateNs;

            // 策略 A: 命名空间扩展（精确匹配 OR 存在以该前缀开头的子命名空间）
            bool nsExists = state.Module.Types.Any(t => !t.IsNested && t.Namespace == candidateNs)
                || state.Module.Types.Any(t => !t.IsNested && t.Namespace.StartsWith(candidateNs + "."));

            // 策略 B: 精确全限定名类型匹配
            var exactType = AssemblyLoaderService.FindTypeByName(state.Module, fullTypeName);
            if (exactType != null) DedupAddType(exactType, state, output);

            // 命名空间与精确类型同时存在 → 分两条路
            if (nsExists && exactType != null)
            {
                output.Add(new PathState
                {
                    Module = state.Module,
                    NsParts = new List<string>(nsParts) { segment }
                });
                return;   // 类型分支已在上面添加
            }
            if (nsExists)
            {
                output.Add(new PathState
                {
                    Module = state.Module,
                    NsParts = new List<string>(nsParts) { segment }
                });
                return;
            }
            if (exactType != null) return;   // 已添加

            // 策略 D: 名称忽略命名空间（仅首段有效，语义为"直接从类名开始"）
            if (nsParts.Count == 0)
            {
                var nameTypes = state.Module.Types.Where(t => !t.IsNested && t.Name == segment).ToList();
                foreach (var t in nameTypes)
                    DedupAddType(t, state, output);
            }
        }

        void AdvanceInMember(PathState state, string segment, bool isLast,
            bool searchInheritance, List<PathState> output)
        {
            var type = state.Type;
            var allMatches = CollectMembers(type, segment, isLast);

            // 嵌套类型（允许路径继续深入）
            foreach (var nt in type.NestedTypes.Where(nt => nt.Name == segment))
            {
                if (isLast)
                    allMatches.Add(("type", MetadataBrowserService.ToTypeInfo(nt)));
                else
                    output.Add(new PathState
                    {
                        Module = state.Module,
                        NsParts = null,
                        Type = nt,
                        TypeMatchedAtSegment = state.TypeMatchedAtSegment
                    });
            }

            // 将命中的成员转为终态
            foreach (var m in allMatches)
            {
                output.Add(new PathState
                {
                    Module = state.Module,
                    NsParts = null,
                    Type = type,
                    TypeMatchedAtSegment = state.TypeMatchedAtSegment,
                    MatchKind = m.Kind,
                    Target = m.Data
                });
            }

            // 无匹配 → 尝试继承搜索（若启用）
            if (allMatches.Count == 0 && searchInheritance)
                SearchInheritance(state, segment, isLast, output);
        }

        void DedupAddType(TypeDef type, PathState parent,
            List<PathState> output)
        {
            // 避免同一 FullName 的重复类型状态
            if (output.Any(s => s.Type?.FullName == type.FullName)) return;
            output.Add(new PathState
            {
                Module = parent.Module,
                NsParts = null,
                Type = type
            });
        }

        static List<PathState> DeduplicateStates(List<PathState> states)
        {
            var seen = new HashSet<string>();
            var result = new List<PathState>();
            foreach (var s in states)
            {
                // 终态去重 key: kind + target identity
                // 非终态(类型中间态)去重 key: type fullname
                string key;
                if (s.Target != null)
                    key = s.MatchKind + ":" + s.Target.GetType().FullName;
                else if (s.Type != null)
                    key = "type:" + s.Type.FullName;
                else
                    key = "ns:" + (s.NsParts != null ? string.Join(".", s.NsParts) : "") + "@" + s.Module.Location;

                if (seen.Add(key))
                    result.Add(s);
            }
            return result;
        }

        // ===== 结果构建 =====

        PathQueryResult BuildTerminalResult(List<PathState> finalStates, string originalPath, bool allSegmentsConsumed)
        {
            var terminals = finalStates.Where(s => s.IsTerminal).ToList();
            var nonTerminals = finalStates.Where(s => !s.IsTerminal).ToList();

            // 有终态结果（成员命中）
            if (terminals.Count > 0)
            {
                var cands = terminals.Select(t =>
                {
                    string kind = t.MatchKind ?? "type";
                    return MakeCandidate(kind, t.Target, t.Module.Location);
                }).ToList();

                return new PathQueryResult
                {
                    Matched = true,
                    Results = cands,
                    ResolvedPath = originalPath
                };
            }

            // 无终态，检查非终态
            if (nonTerminals.Count > 0)
            {
                var typeStates = nonTerminals.Where(s => s.Type != null).ToList();

                if (allSegmentsConsumed && typeStates.Count > 0)
                {
                    // 路径完全消耗完毕且停在类型上 → 成功匹配（如查询 "JsonConvert"）
                    var cands = typeStates.Take(20)
                        .Select(s => MakeCandidate("type",
                            MetadataBrowserService.ToTypeInfo(s.Type), s.Module.Location))
                        .ToList();
                    return new PathQueryResult
                    {
                        Matched = true,
                        Results = cands,
                        ResolvedPath = originalPath
                    };
                }

                if (!allSegmentsConsumed && typeStates.Count > 0)
                {
                    // 路径中途断在类型上（后续段无法匹配成员）→ 断点候选
                    var cands = typeStates.Take(20)
                        .Select(s => MakeCandidate("type",
                            MetadataBrowserService.ToTypeInfo(s.Type), s.Module.Location))
                        .ToList();
                    return new PathQueryResult { Matched = false, Results = cands, ResolvedPath = originalPath };
                }

                // 仅解析了部分命名空间
                var nsCands = nonTerminals
                    .Where(s => s.NsParts != null && s.NsParts.Count > 0 && s.Type == null)
                    .Take(20)
                    .Select(s =>
                    {
                        var nsStr = string.Join(".", s.NsParts);
                        return MakeCandidate("namespace",
                            new { Name = nsStr, Hint = "namespace prefix" }, s.Module.Location);
                    })
                    .ToList();

                if (nsCands.Count > 0)
                    return new PathQueryResult { Matched = false, Results = nsCands, ResolvedPath = originalPath };
            }

            // 完全无匹配
            return new PathQueryResult { Matched = false, Results = new List<PathQueryCandidate>(), ResolvedPath = originalPath };
        }

        // ===== 辅助方法 =====

        List<ModuleDefMD> GetSearchModules(string assemblyPath)
        {
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                var mod = _loader.GetModule(assemblyPath);
                return mod != null ? new List<ModuleDefMD> { mod } : new List<ModuleDefMD>();
            }
            return _loader.ListAssemblies()
                .Select(a => _loader.GetModule(a.Location))
                .Where(m => m != null)
                .ToList();
        }

        static PathQueryCandidate MakeCandidate(string kind, object target, string assemblyPath)
        {
            return new PathQueryCandidate
            {
                MatchKind = kind,
                Target = target,
                ResolvedAssemblyPath = assemblyPath
            };
        }

        // ===== 成员收集（从 AdvanceInMember 提取的纯函数） =====

        static List<(string Kind, object Data)> CollectMembers(
            TypeDef type, string segment, bool isLast)
        {
            var matches = new List<(string Kind, object Data)>();

            // 字段
            foreach (var f in type.Fields.Where(f => IsAccessible(f) && f.Name == segment))
                matches.Add(("field", MetadataBrowserService.ToFieldInfo(f)));

            // 属性
            foreach (var p in type.Properties.Where(p => p.Name == segment))
                matches.Add(("property", MetadataBrowserService.ToPropertyInfo(p)));

            // 方法（仅末尾段）
            if (isLast)
            {
                foreach (var m in type.Methods.Where(m => m.Name == segment))
                    matches.Add(("method", MetadataBrowserService.ToMethodInfo(m, type)));
            }

            // 事件
            foreach (var e in type.Events.Where(e => e.Name == segment))
                matches.Add(("event", MetadataBrowserService.ToEventInfo(e)));

            return matches;
        }

        /// <summary>可见性过滤：仅 public 和 protected</summary>
        static bool IsAccessible(FieldDef field)
        {
            return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
        }

        // ===== 继承搜索（独立 BFS，嵌入在 AdvanceInMember 的无匹配分支中） =====

        void SearchInheritance(PathState parentState, string segment, bool isLast,
            List<PathState> output)
        {
            var visited = new HashSet<TypeDef> { parentState.Type };
            var queue = new Queue<(TypeDef Type, int Distance)>();

            // 入队基类
            var baseDef = TryResolve(parentState.Type.BaseType);
            if (baseDef != null) queue.Enqueue((baseDef, 1));

            // 入队接口
            foreach (var ifaceImpl in parentState.Type.Interfaces)
            {
                var ifaceDef = TryResolve(ifaceImpl.Interface);
                if (ifaceDef != null) queue.Enqueue((ifaceDef, 1));
            }

            while (queue.Count > 0)
            {
                var (currentType, dist) = queue.Dequeue();
                if (!visited.Add(currentType)) continue;

                // 在当前类型中搜索成员
                var matches = CollectMembers(currentType, segment, isLast);

                // 接口显式实现：额外匹配 "InterfaceName.MemberName" 格式
                if (isLast && currentType.IsInterface)
                {
                    foreach (var em in currentType.Methods.Where(m => m.Name.EndsWith("." + segment)))
                        matches.Add(("method", MetadataBrowserService.ToMethodInfo(em, currentType)));
                }

                foreach (var m in matches)
                {
                    output.Add(new PathState
                    {
                        Module = parentState.Module,
                        NsParts = null,
                        Type = parentState.Type,          // 保持原始查询类型
                        TypeMatchedAtSegment = parentState.TypeMatchedAtSegment,
                        MatchKind = m.Kind,
                        Target = m.Data
                    });
                }

                // 继续展开：基类的基类 + 接口
                if (currentType.BaseType != null)
                {
                    var nextBase = TryResolve(currentType.BaseType);
                    if (nextBase != null) queue.Enqueue((nextBase, dist + 1));
                }
                foreach (var ifaceImpl in currentType.Interfaces)
                {
                    var ifaceDef = TryResolve(ifaceImpl.Interface);
                    if (ifaceDef != null) queue.Enqueue((ifaceDef, dist + 1));
                }
            }
        }

        static TypeDef TryResolve(ITypeDefOrRef typeRef)
        {
            if (typeRef is TypeDef td) return td;
            if (typeRef is TypeRef tr)
            {
                try { return tr.Resolve(); }
                catch { return null; } // 缺少依赖程序集或解析失败
            }
            return null;
        }
    }
}

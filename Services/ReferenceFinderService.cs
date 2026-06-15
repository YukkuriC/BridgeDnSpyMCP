// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Documents;
using BDSM.Models;

namespace BDSM.Services
{
    /* ==================================================================
     * 反射缓存：延迟解析 dnSpy.Analyzer.TreeNodes.ScopedWhereUsedAnalyzer<T>
     * 该类型为 internal，通过反射获取泛型定义后创建实例。
     * Lazy 保证在 DnSpyDependencyResolver 注册 AssemblyResolve 之后才触发类型加载。
     * 所有反射结果均缓存，避免重复查找。
     * ================================================================== */

    static class AnalyzerReflection
    {
        const string TypeName = "dnSpy.Analyzer.TreeNodes.ScopedWhereUsedAnalyzer`1";

        static readonly Lazy<Type> _typeLazy = new Lazy<Type>(() =>
        {
            var asm = Assembly.Load("dnSpy.Analyzer.x");
            return asm.GetType(TypeName, throwOnError: true);
        });

        // 泛型定义 -> 已构造类型缓存
        static readonly ConcurrentDictionary<Type, Type> _constructedCache = new ConcurrentDictionary<Type, Type>();

        // 已构造类型 -> 构造函数数组缓存
        static readonly ConcurrentDictionary<Type, ConstructorInfo[]> _ctorCache = new ConcurrentDictionary<Type, ConstructorInfo[]>();

        // PerformAnalysis 方法信息缓存（所有泛型实例共享同一 MethodDef）
        static readonly Lazy<System.Reflection.MethodInfo> _performAnalysisLazy = new Lazy<System.Reflection.MethodInfo>(() =>
            _constructedCache.Values.First().GetMethod("PerformAnalysis", BindingFlags.Public | BindingFlags.Instance));

        /// <summary>
        /// 通过反射创建 ScopedWhereUsedAnalyzer&lt;T&gt; 实例（public 构造函数）。
        /// 签名：(IDsDocumentService, &lt;member类型&gt;, Func&lt;TypeDef, IEnumerable&lt;T&gt;&gt;, ScopedWhereUsedAnalyzerOptions)
        /// </summary>
        public static object Create<T>(IDsDocumentService docSvc, IMemberRef member, Func<TypeDef, IEnumerable<T>> scanFn)
        {
            var t = typeof(T);
            var constructed = _constructedCache.GetOrAdd(t, _ => _typeLazy.Value.MakeGenericType(t));
            var ctors = _ctorCache.GetOrAdd(constructed, c => c.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            var ctor = ctors.Single(c => c.GetParameters()[1].ParameterType.IsAssignableFrom(member.GetType()));
            return ctor.Invoke(new object[] { docSvc, member, scanFn, (int)0 });
        }

        /// <summary>
        /// 通过反射调用 PerformAnalysis(CancellationToken) 返回 IEnumerable&lt;T&gt;。
        /// </summary>
        public static IEnumerable<T> PerformAnalysis<T>(object analyzer, CancellationToken ct) =>
            (IEnumerable<T>)_performAnalysisLazy.Value.Invoke(analyzer, new object[] { ct });
    }

    /* ==================================================================
     * 最小适配器层：将 AssemblyLoaderService 适配为 dnSpy 文档服务接口
     * 仅实现 ScopedWhereUsedAnalyzer 运行时实际调用的成员
     * ================================================================== */

    /// <summary>
    /// 将 ModuleDefMD 包装为 IDsDocument，供 ScopedWhereUsedAnalyzer 遍历。
    /// </summary>
    class ModuleDocument : IDsDocument
    {
        readonly ModuleDefMD _mod;
        public ModuleDocument(ModuleDefMD mod) => _mod = mod;

        // IDsDocument 核心属性
        public ModuleDef ModuleDef => _mod;
        public AssemblyDef AssemblyDef => _mod.Assembly;
        public string Filename { get => _mod.Location; set { } }
        public bool IsAutoLoaded { get; set; }
        public TList<IDsDocument> Children => new TList<IDsDocument>();
        public bool ChildrenLoaded => true;
        public DsDocumentInfo? SerializedDocument => null;
        public IDsDocumentNameKey Key => throw new NotImplementedException();
        public dnlib.PE.IPEImage PEImage => null;

        // IAnnotations（IDsDocument 基接口，分析器不调用，显式实现以规避泛型约束）
        T IAnnotations.AddAnnotation<T>(T annotation) => annotation;
        T IAnnotations.Annotation<T>() => default;
        IEnumerable<T> IAnnotations.Annotations<T>() => Enumerable.Empty<T>();
        void IAnnotations.RemoveAnnotations<T>() { }
    }

    /// <summary>
    /// 将 AssemblyLoaderService 适配为 IDsDocumentService。
    /// ScopedWhereUsedAnalyzer 仅调用 GetDocuments() 获取模块列表，
    /// 其余 17 个接口成员在分析器路径中不被触发。
    /// </summary>
    class McpDocumentService : IDsDocumentService
    {
        readonly AssemblyLoaderService _loader;
        public McpDocumentService(AssemblyLoaderService loader) => _loader = loader;

        /// <summary>核心方法：返回所有已加载模块的文档包装。</summary>
        public IDsDocument[] GetDocuments() =>
            _loader.ListAssemblies()
                  .Select(a => new ModuleDocument(_loader.GetModule(a.Location)))
                  .ToArray();

        // 以下成员在 ScopedWhereUsedAnalyzer 执行路径中不会被调用
        public IDisposable DisableAssemblyLoad() => null;
        public event EventHandler<NotifyDocumentCollectionChangedEventArgs> CollectionChanged { add { } remove { } }
        public IDsDocument GetOrAdd(IDsDocument d) => d;
        public IDsDocument ForceAdd(IDsDocument d, bool _, object __) => d;
        public IDsDocument TryGetOrCreate(DsDocumentInfo i, bool __ = false) => null;
        public IDsDocument TryCreateOnly(DsDocumentInfo i) => null;
        public IDsDocument Resolve(IAssembly a, ModuleDef m) => null;
        public IDsDocument FindAssembly(IAssembly a) => null;
        public IDsDocument FindAssembly(IAssembly a, FindAssemblyOptions o) => null;
        public IDsDocument Find(IDsDocumentNameKey k) => null;
        public void Remove(IDsDocumentNameKey k) { }
        public void Remove(IEnumerable<IDsDocument> ds) { }
        public void Clear() { }
        public void SetDispatcher(Action<Action> d) { }
        public IDsDocument CreateDocument(DsDocumentInfo i, string f, bool m = false) => null;
        public IDsDocument CreateDocument(DsDocumentInfo i, byte[] d, string f, bool l, bool m = false) => null;
        public IAssemblyResolver AssemblyResolver => null;
    }

    /* ==================================================================
     * 引用查找服务 -- 通过反射调用 dnSpy ScopedWhereUsedAnalyzer
     * ================================================================== */

    /// <summary>
    /// 引用查找服务。通过反射调用 dnSpy 内部分析引擎
    /// （ScopedWhereUsedAnalyzer），自动处理可访问性范围判定、
    /// 友元程序集检测、PLINQ 并行扫描。
    /// </summary>
    public class ReferenceFinderService
    {
        readonly AssemblyLoaderService _loader;
        // 必须用 Lazy 延迟初始化，不可简写为 static readonly 字段。
        // 若在类加载时直接 new SigComparer()，此时 DnSpyDependencyResolver 尚未注册，
        // dnlib 内部会因找不到依赖程序集而抛出 FileNotFoundException。
        private static readonly Lazy<object> _comparerLazy = new Lazy<object>(() => new SigComparer());
        private static SigComparer Cmp => (SigComparer)_comparerLazy.Value;

        public ReferenceFinderService(AssemblyLoaderService loader) => _loader = loader;

        /// <summary>
        /// 查找某个成员在所有已加载程序集中的引用位置。
        /// 通过反射构造 ScopedWhereUsedAnalyzer 并执行分析。
        /// </summary>
        public List<ReferenceInfo> FindReferences(string assemblyPath, string fullTypeName, string memberName)
        {
            var module = _loader.GetModule(assemblyPath)
                       ?? throw new InvalidOperationException("Assembly not loaded: " + assemblyPath);
            var targetType = ResolveType(module, fullTypeName)
                          ?? throw new NotFoundException("Type '" + fullTypeName + "' not found.");

            // 解析目标成员
            var targetMember = ResolveMember(targetType, memberName);

            // 通过反射构造分析器并执行
            var docSvc = new McpDocumentService(_loader);
            _currentTarget = targetMember;
            try
            {
                var analyzer = AnalyzerReflection.Create<ReferenceInfo>(docSvc, targetMember, ScanType);
                return AnalyzerReflection.PerformAnalysis<ReferenceInfo>(analyzer, CancellationToken.None).ToList();
            }
            finally { _currentTarget = null; }
        }

        /// <summary>
        /// 查找所有已加载程序集中包含指定字符串的 ldstr 指令位置。
        /// </summary>
        public List<StringRefInfo> FindAllStringRefs(string searchString)
        {
            var results = new ConcurrentBag<StringRefInfo>();
            var allModules = _loader.ListAssemblies()
                                   .Select(a => _loader.GetModule(a.Location))
                                   .Where(m => m != null)
                                   .ToList();

            Parallel.ForEach(allModules, module =>
            {
                foreach (var type in module.Types)
                foreach (var method in type.Methods.Where(m => m.HasBody))
                foreach (var instr in method.Body.Instructions.Where(i =>
                    i.OpCode.Code == Code.Ldstr && (i.Operand as string)?.Contains(searchString) == true))
                {
                    results.Add(new StringRefInfo
                    {
                        ContainingType = type.FullName,
                        ContainingMethod = method.FullName,
                        Offset = (int)instr.Offset,
                        StringValue = (string)instr.Operand
                    });
                }
            });

            return results.OrderBy(r => r.ContainingType).ThenBy(r => r.Offset).ToList();
        }

        /* ---- 回调：ScopedWhereUsedAnalyzer 对每个类型调用此方法执行 IL 扫描 ---- */

        // 通过闭包捕获 _currentTarget，避免每次调用传参
        IMemberRef _currentTarget;

        IEnumerable<ReferenceInfo> ScanType(TypeDef type)
        {
            foreach (var method in type.Methods.Where(m => m.HasBody))
            foreach (var instr in method.Body.Instructions)
            {
                if (!MatchInstruction(instr)) continue;

                var locationMethod = ResolveOriginalLocation(method);
                yield return new ReferenceInfo
                {
                    ContainingType = type.FullName,
                    ContainingMethod = locationMethod.FullName,
                    Offset = (int)instr.Offset,
                    OpCode = instr.OpCode.Name,
                    Operand = DnSpyUtils.FormatOperand(instr.Operand)
                };
            }
        }

        bool MatchInstruction(Instruction instr)
        {
            if (_currentTarget is IMethod mr)
            {
                var op = instr.Operand as IMethod;
                return op != null && !op.IsField
                    && op.Name == mr.Name
                    && IsReferencedBy(((MethodDef)_currentTarget).DeclaringType, op.DeclaringType)
                    && Cmp.Equals(op, _currentTarget);
            }
            if (_currentTarget is IField fr)
            {
                var op = instr.Operand as IField;
                return op != null
                    && op.Name == fr.Name
                    && IsReferencedBy(fr.DeclaringType, op.DeclaringType)
                    && Cmp.Equals(op, _currentTarget);
            }
            return false;
        }

        /* ---- 成员解析 ---- */

        static IMemberRef ResolveMember(TypeDef type, string memberName)
        {
            var method = type.Methods.FirstOrDefault(m => m.Name == memberName);
            if (method != null) { return method; }

            var field = type.Fields.FirstOrDefault(f => f.Name == memberName);
            if (field != null) { return field; }

            var prop = type.Properties.FirstOrDefault(p => p.Name == memberName);
            if (prop != null) { return prop.GetMethod ?? prop.SetMethod; }

            var evt = type.Events.FirstOrDefault(e => e.Name == memberName);
            if (evt != null) { return evt.AddMethod ?? evt.RemoveMethod ?? evt.InvokeMethod; }

            throw new NotFoundException("Member '" + memberName + "' not found.");
        }

        static TypeDef ResolveType(ModuleDefMD module, string fullTypeName) =>
            module.Types.FirstOrDefault(t => t.FullName == fullTypeName)
            ?? module.Types.FirstOrDefault(t => t.FullName.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase));

        /* ---- 工具方法 ---- */

        static bool IsReferencedBy(ITypeDefOrRef analyzedType, ITypeDefOrRef refScope)
        {
            if (refScope == null || analyzedType == null) return false;
            var resolvedScope = refScope is TypeDef td ? td : (refScope as TypeRef)?.Resolve();
            var resolvedAnalyzed = analyzedType is TypeDef atd ? atd : (analyzedType as TypeRef)?.Resolve();
            return Cmp.Equals(resolvedAnalyzed, resolvedScope);
        }

        static MethodDef ResolveOriginalLocation(MethodDef method)
        {
            if (!DnSpyUtils.IsCompilerGenerated(method) || method.DeclaringType?.DeclaringType == null) return method;

            var parent = method.DeclaringType.DeclaringType;
            foreach (var m in parent.Methods.Where(x => x.HasBody && !DnSpyUtils.IsCompilerGenerated(x)))
            foreach (var instr in m.Body.Instructions)
            {
                var mr = instr.Operand as IMethod;
                if (mr != null && Cmp.Equals(mr, method)) return m;
            }
            return method;
        }
    }
}

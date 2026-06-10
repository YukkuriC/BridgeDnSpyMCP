// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using DnSpyMCP.Models;

namespace DnSpyMCP.Services
{
    /// <summary>
    /// 反编译服务。
    /// 基于 dnSpy 的 IDecompiler 接口（通过 MEF 容器获取反编译器实例）提供 C# / IL 反编译输出能力。
    /// </summary>
    public class DecompilationService
    {
        private readonly AssemblyLoaderService _loader;
        private readonly Lazy<IDecompiler> _csharpDecompiler;
        private readonly Lazy<IDecompiler> _ilDecompiler;
        private bool _initialized;

        public DecompilationService(AssemblyLoaderService loader)
        {
            _loader = loader;
            // 延迟初始化：首次调用反编译方法时才创建 MEF 容器
            _csharpDecompiler = new Lazy<IDecompiler>(CreateCSharpDecompiler);
            _ilDecompiler = new Lazy<IDecompiler>(CreateILDecompiler);
        }

        // ---- C# 反编译 ----

        /// <summary>
        /// 将指定类型反编译为 C# 源码文本。
        /// </summary>
        public string DecompileType(string assemblyPath, string fullTypeName)
        {
            var module = RequireModule(assemblyPath);
            var type = FindType(module, fullTypeName);
            if (type == null)
                throw new NotFoundException("Type '" + fullTypeName + "' not found.");

            return DecompileWith(decompiler =>
            {
                var output = CreateOutput();
                decompiler.Decompile(type, output, CreateContext());
                return output;
            });
        }

        /// <summary>
        /// 将单个方法反编译为 C# 源码文本。
        /// </summary>
        public string DecompileMethod(string assemblyPath, string fullTypeName, string methodName)
        {
            var module = RequireModule(assemblyPath);
            var type = FindType(module, fullTypeName);
            if (type == null)
                throw new NotFoundException("Type '" + fullTypeName + "' not found.");

            var method = type.Methods.FirstOrDefault(m =>
                m.Name == methodName || m.FullName.EndsWith("." + methodName));
            if (method == null)
                throw new NotFoundException("Method '" + methodName + "' not found in type '" + fullTypeName + "'.");

            return DecompileWith(decompiler =>
            {
                var output = CreateOutput();
                decompiler.Decompile(method, output, CreateContext());
                return output;
            });
        }

        /// <summary>
        /// 反编译整个程序集，返回每个类型的 C# 源码字典。
        /// </summary>
        public Dictionary<string, string> DecompileAssembly(string assemblyPath, string namespaceFilter = null)
        {
            var module = RequireModule(assemblyPath);
            var decompiler = _csharpDecompiler.Value;
            var ctx = CreateContext();
            var result = new Dictionary<string, string>();

            var types = module.Types
                .Where(t => !t.IsNested && !IsCompilerGenerated(t))
                .Where(t => namespaceFilter == null || t.Namespace == namespaceFilter)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var output = CreateOutput();
                    decompiler.Decompile(type, output, ctx);
                    result[type.FullName] = output.GetText();
                }
                catch
                {
                    result[type.FullName] = "// Unable to decompile " + type.FullName;
                }
            }

            return result;
        }

        // ---- IL 反编译 ----

        /// <summary>
        /// 将指定方法输出为 ILASM 格式的中间语言文本。
        /// </summary>
        public string DecompileMethodToIL(string assemblyPath, string fullTypeName, string methodName)
        {
            var module = RequireModule(assemblyPath);
            var type = FindType(module, fullTypeName);
            if (type == null)
                throw new NotFoundException("Type '" + fullTypeName + "' not found.");

            var method = type.Methods.FirstOrDefault(m =>
                m.Name == methodName || m.FullName.EndsWith("." + methodName));
            if (method == null)
                throw new NotFoundException("Method '" + methodName + "' not found in type '" + fullTypeName + "'.");

            // 优先使用 IL 反编译器（输出格式化的 IL），否则回退到手动构建
            try
            {
                var ilDecompiler = _ilDecompiler.Value;
                var output = CreateOutput();
                ilDecompiler.Decompile(method, output, CreateContext());
                return output.GetText();
            }
            catch
            {
                // 回退：手动从 MethodBody 构建原始 IL 文本
                return BuildRawIL(method);
            }
        }

        // ---- 内部：MEF 初始化与反编译器创建 ----

        private IDecompiler CreateCSharpDecompiler()
        {
            EnsureInitialized();
            // 从 MEF 容器中查找 C# 反编译器（通过 GUID 或名称匹配）
            foreach (var decompiler in GetAllDecompilers())
            {
                // C# 反编译器的 UniqueGuid 通常以特定模式标识
                var name = decompiler.UniqueNameUI ?? "";
                if (name.Contains("C#", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
                    decompiler.FileExtension == ".cs")
                {
                    return decompiler;
                }
            }
            // 如果没找到 C# 特定的，返回第一个可用的
            var all = GetAllDecompilers().ToList();
            if (all.Count > 0) return all[0];
            throw new InvalidOperationException("No decompilers available via MEF.");
        }

        private IDecompiler CreateILDecompiler()
        {
            EnsureInitialized();
            foreach (var decompiler in GetAllDecompilers())
            {
                var name = decompiler.UniqueNameUI ?? "";
                if (name.Contains("IL", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("ILSpy", StringComparison.OrdinalIgnoreCase))
                {
                    return decompiler;
                }
            }
            // 回退到 C# 反编译器（至少能工作）
            return _csharpDecompiler.Value;
        }

        // ---- 内部辅助 ----

        private string DecompileWith(Func<IDecompiler, StringBuilderDecompilerOutput> decomposeAction)
        {
            return decomposeAction(_csharpDecompiler.Value).GetText();
        }

        private static StringBuilderDecompilerOutput CreateOutput()
        {
            return new StringBuilderDecompilerOutput();
        }

        private static DecompilationContext CreateContext()
        {
            return new DecompilationContext { CalculateILSpans = false };
        }

        private ModuleDefMD RequireModule(string assemblyPath)
        {
            var module = _loader.GetModule(assemblyPath);
            if (module == null)
                throw new InvalidOperationException(
                    "Assembly not loaded: " + assemblyPath + ". Call load_assembly first.");
            return module;
        }

        private static TypeDef FindType(ModuleDefMD module, string fullTypeName)
        {
            var exact = module.Types.FirstOrDefault(t => t.FullName == fullTypeName);
            if (exact != null) return exact;
            return module.Types.FirstOrDefault(t =>
                t.FullName.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCompilerGenerated(TypeDef t)
        {
            return (t.Name.Contains("<") && t.Name.Contains(">"));
        }

        private static string BuildRawIL(dnlib.DotNet.MethodDef method)
        {
            var sb = new StringBuilder();
            sb.AppendLine(".method " + method.FullName);
            sb.AppendLine("{");

            if (method.Body != null)
            {
                sb.AppendLine("\t.maxstack " + (method.Body.MaxStack > 0 ? method.Body.MaxStack : 8));

                if (method.Body.Variables.Count > 0)
                {
                    sb.AppendLine("\t.locals init (");
                    for (int i = 0; i < method.Body.Variables.Count; i++)
                    {
                        var v = method.Body.Variables[i];
                        var comma = i < method.Body.Variables.Count - 1 ? "," : "";
                        sb.AppendLine("\t\t[" + i + "] " + v.Type + comma);
                    }
                    sb.AppendLine("\t)");
                }

                sb.AppendLine();
                foreach (var instr in method.Body.Instructions)
                {
                    var operandStr = FormatILOperand(instr.Operand);
                    var line = operandStr != null
                        ? "\t" + instr.OpCode.Name + " " + operandStr
                        : "\t" + instr.OpCode.Name;
                    sb.AppendLine("IL_" + instr.Offset.ToString("X4") + ": " + line);
                }
            }
            else
            {
                sb.AppendLine("\t// Abstract or extern method - no body");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string FormatILOperand(object operand)
        {
            if (operand == null) return null;

            var im = operand as dnlib.DotNet.IMethod;
            if (im != null) return im.FullName;

            var f = operand as dnlib.DotNet.IField;
            if (f != null) return f.FullName;

            var mr = operand as dnlib.DotNet.IMemberRef;
            if (mr != null) return mr.FullName;

            var tr = operand as dnlib.DotNet.ITypeDefOrRef;
            if (tr != null) return tr.ToString();

            var s = operand as string;
            if (s != null) return "\"" + s + "\"";

            if (operand is sbyte || operand is byte || operand is short || operand is ushort ||
                operand is int || operand is uint || operand is long || operand is ulong)
                return operand.ToString();

            if (operand is float) return ((float)operand).ToString("G9");
            if (operand is double) return ((double)operand).ToString("G17");

            var instr = operand as dnlib.DotNet.Emit.Instruction;
            if (instr != null) return "IL_" + instr.Offset.ToString("X4");

            return operand.ToString();
        }

        // ---- MEF 静态容器（进程级单例）----

        private static readonly object _initLock = new object();
        private static List<IDecompiler> _allDecompilers;

        private void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;
                InitializeMEF();
                _initialized = true;
            }
        }

        private static void InitializeMEF()
        {
            // 通过反射加载 dnSpy 的反编译器 DLL 并使用 MEF 获取 IDecompiler 导出
            var dnSpyBinDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

            // 尝试从已知的 dnSpy 安装路径或程序集位置发现目录
            var searchPaths = GetSearchPaths(dnSpyBinDir);

            var catalog = new System.ComponentModel.Composition.Hosting.AggregateCatalog();
            foreach (var dir in searchPaths)
            {
                if (Directory.Exists(dir))
                {
                    catalog.Catalogs.Add(
                        new System.ComponentModel.Composition.Hosting.DirectoryCatalog(dir));
                }
            }

            var container = new System.ComponentModel.Composition.Hosting.CompositionContainer(catalog);

            try
            {
                // IDecompilerCreator 是工厂接口，每个导出者可创建一个或多个 IDecompiler
                var creators = container.GetExportedValues<IDecompilerCreator>();
                var list = new List<IDecompiler>();
                foreach (var creator in creators)
                {
                    try
                    {
                        foreach (var dec in creator.Create())
                        {
                            list.Add(dec);
                        }
                    }
                    catch
                    {
                        // 某些创建者可能因缺少运行时依赖而失败，跳过
                    }
                }
                _allDecompilers = list;
            }
            finally
            {
                container.Dispose();
            }

            if (_allDecompilers == null || _allDecompilers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Failed to load any decompilers via MEF. " +
                    "Ensure dnSpy DLLs are accessible from: " +
                    string.Join("; ", searchPaths));
            }
        }

        private static IEnumerable<IDecompiler> GetAllDecompilers()
        {
            return _allDecompilers ?? Enumerable.Empty<IDecompiler>();
        }

        /// <summary>
        /// 确定搜索 dnSpy DLL 的目录列表。
        /// 优先级：1. path.props 配置的 DnSpyPath\bin 2. 当前程序目录 3. 上级目录\bin
        /// </summary>
        private static List<string> GetSearchPaths(string baseDir)
        {
            var paths = new List<string>();

            // 1. 尝试从 path.props 中读取 DnSpyPath（通过 AppDomain 或环境变量）
            var dnSpyPathFromProps = TryGetDnSpyPathFromConfig();
            if (!string.IsNullOrEmpty(dnSpyPathFromProps))
            {
                paths.Add(Path.Combine(dnSpyPathFromProps, "bin"));
            }

            // 2. 当前程序所在目录
            paths.Add(baseDir);

            // 3. 常见相对路径
            paths.Add(Path.Combine(baseDir, "bin"));
            paths.Add(Path.GetDirectoryName(baseDir) ?? baseDir);

            // 4. 尝试常见安装位置
            var envDnSpy = Environment.GetEnvironmentVariable("DNSPY_PATH");
            if (!string.IsNullOrEmpty(envDnSpy))
                paths.Add(Path.Combine(envDnSpy, "bin"));

            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string TryGetDnSpyPathFromConfig()
        {
            // 尝试读取同目录下的 path.props
            var propsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path.props");
            if (File.Exists(propsFile))
            {
                var content = File.ReadAllText(propsFile);
                // 简单 XML 解析提取 DnSpyPath 值
                var match = System.Text.RegularExpressions.Regex.Match(
                    content, "<DnSpyPath>([^<]+)</DnSpyPath>");
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }
            return null;
        }
    }
}

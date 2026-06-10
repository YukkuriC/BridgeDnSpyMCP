// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DnSpyMCP
{
    /// <summary>
    /// 启动时检查 dnSpy 所需 DLL 是否存在于程序目录，
    /// 若缺失则提示用户输入 dnSpy 安装路径，自动从 /bin 复制所有依赖。
    /// </summary>
    internal static class DllDependencyResolver
    {
        // ---- 所需 DLL 名称列表（与 csproj 中 Reference 一致，排除 .NET Framework 内置程序集）----

        private static readonly string[] RequiredDlls =
        {
            // dnlib
            "dnlib.dll",

            // ILSpy 反编译引擎
            "ICSharpCode.Decompiler.dll",
            "ICSharpCode.NRefactory.dll",
            "ICSharpCode.NRefactory.CSharp.dll",
            "ICSharpCode.NRefactory.VB.dll",
            "ICSharpCode.TreeView.dll",

            // dnSpy 反编译器接口与实现
            "dnSpy.Contracts.Logic.dll",
            "dnSpy.Contracts.DnSpy.dll",
            "dnSpy.Decompiler.dll",
            "dnSpy.Decompiler.ILSpy.Core.dll",
            "dnSpy.Decompiler.ILSpy.x.dll",

            // Roslyn
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "dnSpy.Roslyn.dll",

            // MEF v2
            "System.Composition.AttributedModel.dll",
            "System.Composition.Convention.dll",
            "System.Composition.Hosting.dll",
            "System.Composition.Runtime.dll",
            "System.Composition.TypedParts.dll",
            "Microsoft.VisualStudio.Composition.dll",
            "Microsoft.VisualStudio.Validation.dll",
            "Microsoft.VisualStudio.Text.Logic.dll",
            "Microsoft.VisualStudio.Text.Data.dll",
            "Microsoft.VisualStudio.CoreUtility.dll",

            // Roslyn / ILSpy 传递依赖
            "Iced.dll",
            "System.Collections.Immutable.dll",
            "System.Reflection.Metadata.dll",
            "System.Memory.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
            "System.Buffers.dll",
            "System.Numerics.Vectors.dll",
            "System.ValueTuple.dll",
            "System.Threading.Tasks.Extensions.dll",
            "System.Threading.Tasks.Dataflow.dll",
            "System.Text.Encoding.CodePages.dll",

            // JSON 序列化
            "Newtonsoft.Json.dll",
            "System.Text.Json.dll",
        };

        /// <summary>
        /// 检查并确保所有所需 DLL 就位。若缺失则引导用户完成复制。
        /// </summary>
        public static void EnsureDependencies()
        {
            var targetDir = AppDomain.CurrentDomain.BaseDirectory;

            // 最早期注册 AssemblyResolve 兜底，确保即使文件复制有边界情况也能加载
            RegisterAssemblyResolve(targetDir);

            var missing = GetMissingDlls(targetDir);

            if (missing.Count == 0)
                return;

            Console.Error.WriteLine("[DnSpyMCP] Missing " + missing.Count + " required DLL(s):");
            foreach (var dll in missing)
                Console.Error.WriteLine("  - " + dll);
            Console.Error.WriteLine();

            // 尝试从已保存的配置中恢复路径
            var savedPath = TryGetSavedPath(targetDir);
            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(Path.Combine(savedPath, "bin")))
            {
                Console.Error.WriteLine("[DnSpyMCP] Using previously saved dnSpy path: " + savedPath);
                if (TryCopyFrom(savedPath, targetDir, missing))
                    return;
                Console.Error.WriteLine("[DnSpyMCP] Copy from saved path failed, please re-enter.");
            }

            // 循环提示直到成功或用户放弃
            while (true)
            {
                Console.Error.Write("[DnSpyMCP] Please enter the dnSpy installation directory (e.g. C:\\Program Files\\dnSpy): ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.Error.WriteLine("[DnSpyMCP] No input provided. Cannot continue without required DLLs.");
                    Environment.Exit(1);
                }

                var dnSpyRoot = input.Trim().TrimEnd('\\', '/');
                var dnSpyBin = Path.Combine(dnSpyRoot, "bin");

                if (!Directory.Exists(dnSpyBin))
                {
                    Console.Error.WriteLine("[DnSpyMCP] Error: '" + dnSpyBin + "' does not exist.");
                    continue;
                }

                if (TryCopyFrom(dnSpyRoot, targetDir, missing))
                {
                    SavePath(targetDir, dnSpyRoot);
                    return;
                }
            }
        }

        // ---- 内部方法 ----

        /// <summary>
        /// 注册 AppDomain.AssemblyResolve 事件，作为 CLR 默认探测失败后的兜底加载机制。
        /// 搜索顺序：程序目录 -> 已保存的 dnSpy bin 路径 -> 环境变量路径。
        /// </summary>
        private static void RegisterAssemblyResolve(string baseDir)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new System.Reflection.AssemblyName(args.Name);
                var dllName = assemblyName.Name + ".dll";

                // 1. 程序所在目录
                var localPath = Path.Combine(baseDir, dllName);
                if (File.Exists(localPath))
                    return System.Reflection.Assembly.LoadFrom(localPath);

                // 2. 从已保存的 dnSpy 路径查找
                var savedPath = TryGetSavedPath(baseDir);
                if (!string.IsNullOrEmpty(savedPath))
                {
                    var dnSpyPath = Path.Combine(savedPath, "bin", dllName);
                    if (File.Exists(dnSpyPath))
                        return System.Reflection.Assembly.LoadFrom(dnSpyPath);
                }

                // 3. 环境变量 DNSPY_PATH
                var envPath = Environment.GetEnvironmentVariable("DNSPY_PATH");
                if (!string.IsNullOrEmpty(envPath))
                {
                    var envDllPath = Path.Combine(envPath, "bin", dllName);
                    if (File.Exists(envDllPath))
                        return System.Reflection.Assembly.LoadFrom(envDllPath);
                }

                return null;
            };
        }

        private static List<string> GetMissingDlls(string targetDir)
        {
            var missing = new List<string>();
            foreach (var dll in RequiredDlls)
            {
                if (!File.Exists(Path.Combine(targetDir, dll)))
                    missing.Add(dll);
            }
            return missing;
        }

        private static bool TryCopyFrom(string dnSpyRoot, string targetDir, List<string> missing)
        {
            var sourceBin = Path.Combine(dnSpyRoot, "bin");
            int copied = 0;
            int failed = 0;

            foreach (var dll in missing)
            {
                var src = Path.Combine(sourceBin, dll);
                var dst = Path.Combine(targetDir, dll);

                if (!File.Exists(src))
                {
                    Console.Error.WriteLine("  [SKIP] " + dll + " - not found in source");
                    failed++;
                    continue;
                }

                try
                {
                    File.Copy(src, dst, overwrite: true);
                    copied++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("  [FAIL] " + dll + " - " + ex.Message);
                    failed++;
                }
            }

            Console.Error.WriteLine("[DnSpyMCP] Copied " + copied + " DLL(s), " + failed + " failed.");
            return failed == 0 && copied > 0;
        }

        private static string TryGetSavedPath(string baseDir)
        {
            // 优先从同目录的 path.props 读取
            var propsFile = Path.Combine(baseDir, "path.props");
            if (File.Exists(propsFile))
            {
                try
                {
                    var content = File.ReadAllText(propsFile);
                    var match = Regex.Match(content, "<DnSpyPath>([^<]+)</DnSpyPath>");
                    if (match.Success)
                        return match.Groups[1].Value.Trim();
                }
                catch { }
            }

            // 其次尝试环境变量
            var envPath = Environment.GetEnvironmentVariable("DNSPY_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(Path.Combine(envPath, "bin")))
                return envPath;

            return null;
        }

        private static void SavePath(string baseDir, string dnSpyRoot)
        {
            var propsFile = Path.Combine(baseDir, "path.props");
            try
            {
                var content = "<Project>\r\n    <PropertyGroup>\r\n        <DnSpyPath>" + dnSpyRoot + "</DnSpyPath>\r\n    </PropertyGroup>\r\n</Project>\r\n";
                File.WriteAllText(propsFile, content);
                Console.Error.WriteLine("[DnSpyMCP] dnSpy path saved to path.props for future use.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[DnSpyMCP] Warning: could not save path.props - " + ex.Message);
            }
        }
    }
}

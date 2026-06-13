// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BDSM
{
    /// <summary>
    /// 启动时通过 ConfigManager 配置的 dnSpy 路径加载 dnSpy.Console.exe，
    /// 以其引用链自动拉入所有依赖 DLL（不拷贝、不枚举、不使用 MEF）。
    /// 定义"自检"为验证 exe 可加载。自检失败时进入简化模式，仅暴露配置工具供 MCP agent 设置路径。
    /// </summary>
    internal static class DnSpyDependencyResolver
    {
        private static readonly string[] ExeCandidates = { "dnSpy.Console.exe", "dnSpy.exe" };

        /// <summary>自检是否通过</summary>
        public static bool SelfCheckPassed { get; private set; }

        /// <summary>自检结果消息</summary>
        public static string SelfCheckMessage { get; private set; }

        // ---- 公开接口 ----

        /// <summary>
        /// 初始化：注册 AssemblyResolve 兜底，执行自检。
        /// 自检成功则 SelfCheckPassed=true，可继续正常流程；
        /// 失败则 SelfCheckPassed=false，应进入简化模式。
        /// </summary>
        public static void Initialize()
        {
            RegisterAssemblyResolve();
            RunSelfCheck();
        }

        /// <summary>
        /// MCP 工具调用：设置 dnSpy 路径并重新自检。
        /// 返回自检结果的文本消息。
        /// </summary>
        public static string ConfigureAndRetry(string dnSpyPath)
        {
            if (string.IsNullOrWhiteSpace(dnSpyPath))
                return "Error: path cannot be empty.";

            var root = dnSpyPath.Trim().TrimEnd('\\', '/');
            ConfigManager.Set("DnSpyPath", root);

            RunSelfCheck();

            if (SelfCheckPassed)
            {
                var exePath = FindExe(root);
                return "Self-check PASSED! Loaded: " + exePath +
                    "\n\nPlease restart the MCP server to activate full functionality.";
            }
            else
            {
                return "Self-check FAILED. " + SelfCheckMessage +
                    "\n\nPlease verify the path points to a valid dnSpy installation directory." +
                    "\nCurrent configured path: " + root;
            }
        }

        // ---- 内部方法 ----

        /// <summary>
        /// 执行一次自检：尝试从 ConfigManager.DnSpyPath 加载 dnSpy Console/GUI exe。
        /// 加载成功即通过（依赖链由 AssemblyResolve 自动解析）。
        /// </summary>
        private static void RunSelfCheck()
        {
            var dnSpyRoot = ConfigManager.DnSpyPath;
            if (string.IsNullOrEmpty(dnSpyRoot))
            {
                SelfCheckPassed = false;
                SelfCheckMessage = "No dnSpy path configured.";
                Console.Error.WriteLine("[BDSM] " + SelfCheckMessage);
                return;
            }

            var exePath = FindExe(dnSpyRoot);
            if (exePath == null || !File.Exists(exePath))
            {
                SelfCheckPassed = false;
                SelfCheckMessage = "dnSpy executable not found at: " + dnSpyRoot +
                    " (expected: " + string.Join(" or ", ExeCandidates) + ")";
                Console.Error.WriteLine("[BDSM] " + SelfCheckMessage);
                return;
            }

            try
            {
                // 尝试加载 exe 触发完整依赖链
                Assembly.Load(AssemblyName.GetAssemblyName(exePath));
                SelfCheckPassed = true;
                SelfCheckMessage = "[BDSM] Self-check passed: loaded " + exePath;
            }
            catch (Exception ex)
            {
                SelfCheckPassed = false;
                SelfCheckMessage = "Failed to load " + exePath + ": " + ex.GetType().Name + " - " + ex.Message;
            }

            Console.Error.WriteLine(SelfCheckMessage);
        }

        /// <summary>
        /// 注册 AppDomain.AssemblyResolve 事件。
        /// 搜索顺序：exe 所在目录 -> bin\ 子目录 -> 程序目录 -> 环境变量 DNSPY_PATH
        /// 凡是搜索路径中存在该 DLL 就用 Assembly.Load() 加载，
        /// 找不到则返回 null 回退给 CLR 默认行为。
        /// </summary>
        private static void RegisterAssemblyResolve()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name);
                var dllName = assemblyName.Name + ".dll";

                // 1. dnSpy 安装根目录（exe 所在位置）
                var dnSpyRoot = ConfigManager.DnSpyPath;
                if (!string.IsNullOrEmpty(dnSpyRoot))
                {
                    var rootDll = Path.Combine(dnSpyRoot, dllName);
                    if (File.Exists(rootDll))
                        return Assembly.Load(AssemblyName.GetAssemblyName(rootDll));

                    // 1b. bin\ 子目录（dnSpy 将依赖 DLL 放在此处）
                    var binDll = Path.Combine(dnSpyRoot, "bin", dllName);
                    if (File.Exists(binDll))
                        return Assembly.Load(AssemblyName.GetAssemblyName(binDll));
                }

                // 2. 程序所在目录
                var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
                if (File.Exists(localPath))
                    return Assembly.Load(AssemblyName.GetAssemblyName(localPath));

                // 回退 CLR 默认解析
                return null;
            };
        }

        /// <summary>在 dnSpy 根目录中查找可用的 exe 文件。</summary>
        internal static string FindExe(string dnSpyRoot)
        {
            if (string.IsNullOrEmpty(dnSpyRoot)) return null;

            foreach (var candidate in ExeCandidates)
            {
                var fullPath = Path.Combine(dnSpyRoot, candidate);
                if (File.Exists(fullPath)) return fullPath;
            }
            return null;
        }
    }
}

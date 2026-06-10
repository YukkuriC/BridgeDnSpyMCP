// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using DnSpyMCP.Models;

namespace DnSpyMCP.Services
{
    /// <summary>
    /// 程序集加载与管理服务。
    /// 基于 dnlib 的 ModuleDefMD 加载 .NET 程序集，提供统一的模块生命周期管理。
    /// </summary>
    public class AssemblyLoaderService
    {
        private readonly ConcurrentDictionary<string, ModuleDefMD> _assemblies =
            new ConcurrentDictionary<string, ModuleDefMD>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 加载程序集并返回唯一标识符（使用路径作为 key）。
        /// 若该路径已加载则直接返回已有实例。
        /// </summary>
        public AssemblyInfo LoadAssembly(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Assembly file not found: " + fullPath);

            var module = _assemblies.GetOrAdd(fullPath, p => ModuleDefMD.Load(p));
            return ToAssemblyInfo(module);
        }

        /// <summary>
        /// 从已加载的程序集中移除指定项，释放资源。
        /// </summary>
        public bool UnloadAssembly(string path)
        {
            var fullPath = Path.GetFullPath(path);
            ModuleDefMD ignored;
            return _assemblies.TryRemove(fullPath, out ignored);
        }

        /// <summary>
        /// 获取已加载的所有程序集信息。
        /// </summary>
        public IReadOnlyList<AssemblyInfo> ListAssemblies()
        {
            return _assemblies.Values.Select(ToAssemblyInfo).ToList();
        }

        /// <summary>
        /// 根据路径获取已加载的 ModuleDefMD 实例。
        /// </summary>
        public ModuleDefMD GetModule(string path)
        {
            var fullPath = Path.GetFullPath(path);
            ModuleDefMD module;
            return _assemblies.TryGetValue(fullPath, out module) ? module : null;
        }

        /// <summary>
        /// 根据路径前缀或名称模糊匹配已加载的程序集。
        /// </summary>
        public ModuleDefMD FindModule(string query)
        {
            var exact = GetModule(query);
            if (exact != null) return exact;

            var fileName = Path.GetFileName(query);
            foreach (var pair in _assemblies)
            {
                if (Path.GetFileName(pair.Key).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            foreach (var pair in _assemblies)
            {
                if (pair.Value.Assembly != null &&
                    string.Equals(pair.Value.Assembly.Name.ToString(), query, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
                if (string.Equals(pair.Value.Name, query, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            return null;
        }

        private static AssemblyInfo ToAssemblyInfo(ModuleDefMD module)
        {
            var nonNestedTypes = module.Types.Where(t => !t.IsNested).ToList();
            return new AssemblyInfo
            {
                Name = module.Assembly != null ? module.Assembly.Name.ToString() : module.Name,
                FullName = module.Assembly != null ? module.Assembly.FullName : module.FullName,
                Location = module.Location,
                RuntimeVersion = module.RuntimeVersion,
                TypeCount = nonNestedTypes.Count,
                Is64Bit = Environment.Is64BitProcess,
                Kind = module.Kind
            };
        }
    }
}

// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using BDSM;
using BDSM.Models;

namespace BDSM.Services
{
    /// <summary>
    /// 程序集加载与管理服务。
    /// 基于 dnlib 的 ModuleDefMD 加载 .NET 程序集，提供统一的模块生命周期管理。
    /// </summary>
    public class AssemblyLoaderService
    {
        private readonly ConcurrentDictionary<string, ModuleDefMD> _assemblies =
            new ConcurrentDictionary<string, ModuleDefMD>(StringComparer.OrdinalIgnoreCase);

        /// <summary>是否有已加载的程序集</summary>
        public bool HasAssemblies => _assemblies.Count > 0;

        /// <summary>
        /// 加载程序集并返回唯一标识符（使用路径作为 key）。
        /// 若该路径已加载则直接返回已有实例。
        /// </summary>
        public AssemblyInfo LoadAssembly(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                throw new UserException("Assembly file not found: " + fullPath);

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
        /// 清空所有已加载的程序集，释放资源。
        /// </summary>
        public int ClearAllAssemblies()
        {
            var count = _assemblies.Count;
            _assemblies.Clear();
            return count;
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
        /// 路径非法或程序集未加载时，allowNull=false 抛出 UserException，allowNull=true 返回 null。
        /// </summary>
        public ModuleDefMD GetModule(string path, bool allowNull = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return allowNull ? null : throw new UserException("assembly_path must not be empty.");

            string fullPath;
            try { fullPath = Path.GetFullPath(path); }
            catch (ArgumentException) { return allowNull ? null : throw new UserException("Invalid assembly_path: " + path); }

            ModuleDefMD module;
            if (!_assemblies.TryGetValue(fullPath, out module))
                return allowNull ? null : throw new UserException("Assembly not loaded: " + path + ". Call load_assembly first.");
            return module;
        }

        /// <summary>
        /// 根据路径前缀或名称模糊匹配已加载的程序集。
        /// </summary>
        public ModuleDefMD FindModule(string query)
        {
            var exact = GetModule(query, allowNull: true);
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

        // ===== 成员查找（统一入口） =====

        public static TypeDef FindTypeByName(ModuleDefMD module, string fullTypeName)
        {
            var exact = module.Types.FirstOrDefault(t => t.FullName == fullTypeName);
            if (exact != null) return exact;
            return module.Types.FirstOrDefault(t =>
                t.FullName.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase));
        }

        public static MethodDef FindMethod(TypeDef type, string methodName)
        {
            return type.Methods.FirstOrDefault(m =>
                m.Name == methodName || m.FullName.EndsWith("." + methodName));
        }

        public static FieldDef FindField(TypeDef type, string fieldName)
        {
            return type.Fields.FirstOrDefault(f => f.Name == fieldName);
        }

        public static PropertyDef FindProperty(TypeDef type, string propertyName)
        {
            return type.Properties.FirstOrDefault(p => p.Name == propertyName);
        }

        public static EventDef FindEvent(TypeDef type, string eventName)
        {
            return type.Events.FirstOrDefault(e => e.Name == eventName);
        }

        public TypeDef RequireType(string assemblyPath, string fullTypeName)
        {
            var module = GetModule(assemblyPath);
            var type = FindTypeByName(module, fullTypeName);
            if (type == null)
                throw new UserException("Type '" + fullTypeName + "' not found in assembly.");
            return type;
        }

        public MethodDef RequireMethod(string assemblyPath, string fullTypeName, string methodName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var method = FindMethod(type, methodName);
            if (method == null)
                throw new UserException("Method '" + methodName + "' not found in type '" + fullTypeName + "'.");
            return method;
        }

        public FieldDef RequireField(string assemblyPath, string fullTypeName, string fieldName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var field = FindField(type, fieldName);
            if (field == null)
                throw new UserException("Field '" + fieldName + "' not found in type '" + fullTypeName + "'.");
            return field;
        }

        public PropertyDef RequireProperty(string assemblyPath, string fullTypeName, string propertyName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var prop = FindProperty(type, propertyName);
            if (prop == null)
                throw new UserException("Property '" + propertyName + "' not found in type '" + fullTypeName + "'.");
            return prop;
        }

        public EventDef RequireEvent(string assemblyPath, string fullTypeName, string eventName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var evt = FindEvent(type, eventName);
            if (evt == null)
                throw new UserException("Event '" + eventName + "' not found in type '" + fullTypeName + "'.");
            return evt;
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

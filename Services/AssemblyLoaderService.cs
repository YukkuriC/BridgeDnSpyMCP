// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BDSM;
using BDSM.Models;

namespace BDSM.Services
{
    /// <summary>
    /// 程序集加载与管理服务。
    /// 基于 dnlib 的 ModuleDefMD 加载 .NET 程序集，提供统一的模块生命周期管理。
    /// 集成 dnlib AssemblyResolver 支持依赖自动加载与探测路径管理。
    /// </summary>
    public class AssemblyLoaderService
    {
        private readonly ConcurrentDictionary<string, ModuleDefMD> _assemblies =
            new ConcurrentDictionary<string, ModuleDefMD>(StringComparer.OrdinalIgnoreCase);

        private readonly ModuleContext _moduleContext;
        private readonly AssemblyResolver _assemblyResolver;

        // 用户手动添加的探测路径（与 probe_paths 工具共用）
        private readonly HashSet<string> _userProbePaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public AssemblyLoaderService()
        {
            _assemblyResolver = new AssemblyResolver();
            _moduleContext = new ModuleContext(_assemblyResolver);
        }

        /// <summary>
        /// 加载程序集并返回唯一标识符（使用路径作为 key）。
        /// 若该路径已加载则直接返回已有实例。
        /// 使用 ModuleContext 启用依赖自动加载。
        /// </summary>
        public AssemblyInfo LoadAssembly(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                throw new UserException("Assembly file not found: " + fullPath);

            var module = _assemblies.GetOrAdd(fullPath, p =>
            {
                var mod = ModuleDefMD.Load(p, _moduleContext);
                _assemblyResolver.AddToCache(mod);
                // 自动添加模块所在目录到搜索路径
                AddSearchPath(Path.GetDirectoryName(mod.Location));
                // 读取 .config 文件中的探测路径
                AddConfigProbePaths(mod.Location);
                // 递归加载 AssemblyRef 依赖
                LoadDependencies(mod);
                return mod;
            });
            return ToAssemblyInfo(module);
        }

        /// <summary>
        /// 递归加载模块的 AssemblyRef 依赖。
        /// 使用 AssemblyResolver 在搜索路径中查找并加载依赖程序集。
        /// </summary>
        private void LoadDependencies(ModuleDef module)
        {
            foreach (var asmRef in module.GetAssemblyRefs())
            {
                var asmName = asmRef.Name.ToString();
                if (string.IsNullOrEmpty(asmName))
                    continue;
                // 跳过已加载的同名程序集
                if (_assemblies.Values.Any(m =>
                    m.Assembly != null && m.Assembly.Name.String.Equals(asmName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                try
                {
                    var asm = _assemblyResolver.Resolve(asmRef, module);
                    if (asm == null)
                        continue;
                    var depMod = asm.ManifestModule;
                    if (depMod == null)
                        continue;
                    var depPath = depMod.Location;
                    if (string.IsNullOrEmpty(depPath) || _assemblies.ContainsKey(depPath))
                        continue;
                    if (depMod is ModuleDefMD depModMD)
                    {
                        _assemblies.TryAdd(depPath, depModMD);
                        _assemblyResolver.AddToCache(depModMD);
                        AddSearchPath(Path.GetDirectoryName(depPath));
                        AddConfigProbePaths(depPath);
                        LoadDependencies(depMod);
                    }
                }
                catch
                {
                }
            }
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

        // ===== 跨程序集查找（兜底机制） =====

        /// <summary>
        /// 遍历所有已加载模块查找类型（兜底机制）。
        /// 当未指定程序集或在指定程序集中未找到类型时调用。
        /// </summary>
        public TypeDef FindTypeAcrossAssemblies(string fullTypeName)
        {
            foreach (var pair in _assemblies)
            {
                var type = FindTypeByName(pair.Value, fullTypeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        /// <summary>
        /// 要求类型存在，支持跨程序集兜底查找。
        /// </summary>
        /// <param name="assemblyPath">可选，指定程序集路径。为 null 时在所有已加载程序集中查找。</param>
        /// <param name="fullTypeName">类型全限定名</param>
        public TypeDef RequireType(string assemblyPath, string fullTypeName)
        {
            // 指定了程序集路径：先在该程序集中查找
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                var module = GetModule(assemblyPath, allowNull: false);
                var type = FindTypeByName(module, fullTypeName);
                if (type != null)
                    return type;
                // 指定程序集未找到，尝试跨程序集兜底
            }

            // 未指定程序集 或 指定程序集未找到：跨程序集查找
            var foundType = FindTypeAcrossAssemblies(fullTypeName);
            if (foundType == null)
            {
                var msg = string.IsNullOrEmpty(assemblyPath)
                    ? "Type '" + fullTypeName + "' not found in any loaded assembly."
                    : "Type '" + fullTypeName + "' not found in specified assembly or any other loaded assembly.";
                throw new UserException(msg);
            }
            return foundType;
        }

        /// <summary>
        /// 要求方法存在，支持跨程序集兜底查找。
        /// </summary>
        public MethodDef RequireMethod(string assemblyPath, string fullTypeName, string methodName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var method = FindMethod(type, methodName);
            if (method == null)
                throw new UserException("Method '" + methodName + "' not found in type '" + fullTypeName + "'.");
            return method;
        }

        /// <summary>
        /// 要求字段存在，支持跨程序集兜底查找。
        /// </summary>
        public FieldDef RequireField(string assemblyPath, string fullTypeName, string fieldName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var field = FindField(type, fieldName);
            if (field == null)
                throw new UserException("Field '" + fieldName + "' not found in type '" + fullTypeName + "'.");
            return field;
        }

        /// <summary>
        /// 要求属性存在，支持跨程序集兜底查找。
        /// </summary>
        public PropertyDef RequireProperty(string assemblyPath, string fullTypeName, string propertyName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var prop = FindProperty(type, propertyName);
            if (prop == null)
                throw new UserException("Property '" + propertyName + "' not found in type '" + fullTypeName + "'.");
            return prop;
        }

        /// <summary>
        /// 要求事件存在，支持跨程序集兜底查找。
        /// </summary>
        public EventDef RequireEvent(string assemblyPath, string fullTypeName, string eventName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var evt = FindEvent(type, eventName);
            if (evt == null)
                throw new UserException("Event '" + eventName + "' not found in type '" + fullTypeName + "'.");
            return evt;
        }

        // ===== 探测路径管理 =====

        /// <summary>
        /// 将目录添加到 AssemblyResolver 搜索路径（内部使用，去重）。
        /// </summary>
        private void AddSearchPath(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;
            if (!_assemblyResolver.PreSearchPaths.Contains(dir))
                _assemblyResolver.PreSearchPaths.Add(dir);
        }

        /// <summary>
        /// 读取 .config 文件中的 probing privatePath 并添加到搜索路径。
        /// 例如 &lt;probing privatePath="bin;lib" /&gt; 会添加 bin 和 lib 子目录。
        /// </summary>
        private void AddConfigProbePaths(string modulePath)
        {
            var configFile = modulePath + ".config";
            if (!File.Exists(configFile))
                return;

            var sourceDir = Path.GetDirectoryName(modulePath);
            try
            {
                var doc = new XmlDocument();
                doc.Load(configFile);
                foreach (XmlNode node in doc.GetElementsByTagName("probing"))
                {
                    var privatePath = node.Attributes?["privatePath"]?.Value;
                    if (string.IsNullOrEmpty(privatePath))
                        continue;
                    foreach (var part in privatePath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var combined = Path.GetFullPath(Path.Combine(sourceDir, part.Trim()));
                        AddSearchPath(combined);
                    }
                }
            }
            catch
            {
                // .config 解析失败时静默忽略
            }
        }

        /// <summary>
        /// 获取用户手动添加的探测路径列表。
        /// </summary>
        public List<string> GetPreSearchPaths()
        {
            return _userProbePaths.ToList();
        }

        /// <summary>
        /// 添加探测路径到 AssemblyResolver 的优先搜索列表。
        /// </summary>
        public void AddProbePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new UserException("Probe path must not be empty.");

            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
                throw new UserException("Probe path directory not found: " + fullPath);

            _userProbePaths.Add(fullPath);
            AddSearchPath(fullPath);
        }

        /// <summary>
        /// 从 AssemblyResolver 的优先搜索列表移除探测路径。
        /// </summary>
        public void RemoveProbePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var fullPath = Path.GetFullPath(path);
            _userProbePaths.Remove(fullPath);
            _assemblyResolver.PreSearchPaths.Remove(fullPath);
        }

        // ===== 程序集依赖查询 =====

        /// <summary>
        /// 获取程序集的外部依赖清单。
        /// </summary>
        public List<AssemblyRefInfo> GetAssemblyDependencies(string assemblyPath)
        {
            var module = GetModule(assemblyPath);
            var refs = module.GetAssemblyRefs();

            return refs.Select(ar => new AssemblyRefInfo
            {
                Name = ar.Name.ToString(),
                Version = ar.Version?.ToString() ?? "unknown",
                Culture = ar.Culture?.ToString() ?? "neutral",
                PublicKeyToken = "unknown", // dnlib AssemblyRef 不直接暴露 PublicKeyToken 属性
                Token = ar.MDToken.ToString()
            }).ToList();
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

// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BDSM;
using BDSM.Models;

namespace BDSM.Services
{
    /// <summary>
    /// 元数据浏览服务。
    /// 提供类型、方法、字段、属性、事件的枚举与详情查询能力。
    /// </summary>
    public class MetadataBrowserService
    {
        private readonly AssemblyLoaderService _loader;
        private readonly ReferenceFinderService _refFinder;

        public MetadataBrowserService(AssemblyLoaderService loader, ReferenceFinderService refFinder)
        {
            _loader = loader;
            _refFinder = refFinder;
        }

        // ---- 类型查询 ----

        /// <summary>
        /// 列出程序集中所有非嵌套顶层类型。
        /// </summary>
        public List<TypeInfo> ListTypes(string assemblyPath, string namespaceFilter = null)
        {
            var module = _loader.GetModule(assemblyPath);
            return module.Types
                .Where(t => !t.IsNested && !IsCompilerGenerated(t))
                .Where(t => namespaceFilter == null || t.Namespace == namespaceFilter)
                .Select(ToTypeInfo)
                .ToList();
        }

        /// <summary>
        /// 列出程序集中的所有命名空间。
        /// </summary>
        public List<NamespaceInfo> ListNamespaces(string assemblyPath)
        {
            var module = _loader.GetModule(assemblyPath);
            return module.Types
                .Where(t => !t.IsNested)
                .Select(t => t.Namespace)
                .Distinct()
                .OrderBy(ns => ns)
                .Select(ns => new NamespaceInfo
                {
                    Name = ns,
                    TypeCount = module.Types.Count(t => t.Namespace == ns)
                })
                .ToList();
        }

        /// <summary>
        /// 按名称搜索类型（支持模糊匹配）。
        /// </summary>
        public List<TypeInfo> FindType(string assemblyPath, string query)
        {
            var module = _loader.GetModule(assemblyPath);
            var lowerQuery = query.ToLowerInvariant();

            return module.Types
                .Where(t => !IsCompilerGenerated(t))
                .Where(t =>
                    t.Name.ToLowerInvariant().Contains(lowerQuery) ||
                    t.FullName.ToLowerInvariant().Contains(lowerQuery) ||
                    t.Namespace.ToLowerInvariant().Contains(lowerQuery))
                .Take(50)
                .Select(ToTypeInfo)
                .ToList();
        }

        /// <summary>
        /// 获取类型的详细信息。
        /// </summary>
        public TypeInfo GetTypeInfo(string assemblyPath, string fullTypeName)
        {
            var module = _loader.GetModule(assemblyPath);
            var type = AssemblyLoaderService.FindTypeByName(module, fullTypeName);
            if (type == null) return null;
            return ToTypeInfo(type);
        }

        // ---- 成员枚举 ----

        public List<MethodInfo> ListMethods(string assemblyPath, string fullTypeName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            return type.Methods
                .Where(m => !m.IsSpecialName || IsPropertyAccessor(m) || IsEventAccessor(m))
                .Select(m => ToMethodInfo(m, type))
                .ToList();
        }

        public List<FieldInfoData> ListFields(string assemblyPath, string fullTypeName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            return type.Fields.Select(ToFieldInfo).ToList();
        }

        public List<PropertyInfoData> ListProperties(string assemblyPath, string fullTypeName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            return type.Properties.Select(ToPropertyInfo).ToList();
        }

        public List<EventInfoData> ListEvents(string assemblyPath, string fullTypeName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            return type.Events.Select(ToEventInfo).ToList();
        }

        // ---- 方法详情 ----

        public MethodInfo GetMethodInfo(string assemblyPath, string fullTypeName, string methodName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var method = AssemblyLoaderService.FindMethod(type, methodName);
            if (method == null) return null;
            return ToMethodInfo(method, type);
        }

        public List<InstructionInfo> GetMethodIL(string assemblyPath, string fullTypeName, string methodName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var method = AssemblyLoaderService.FindMethod(type, methodName);
            if (method?.Body == null) return null;

            return method.Body.Instructions.Select(instr => new InstructionInfo
            {
                Offset = (int)instr.Offset,
                OpCode = instr.OpCode.Name,
                Operand = DnSpyUtils.FormatOperand(instr.Operand)
            }).ToList();
        }

        // ---- 引用查找（委托给 ReferenceFinderService） ----

        /// <summary>
        /// 查找某个成员在所有已加载程序集中的引用位置。
        /// 自动识别成员类型，按可访问性确定搜索范围，使用 SigComparer 精确匹配。
        /// </summary>
        public List<ReferenceInfo> FindReferences(string assemblyPath, string fullTypeName, string memberName) =>
            _refFinder.FindReferences(assemblyPath, fullTypeName, memberName);

        /// <summary>
        /// 查找所有已加载程序集中包含指定字符串的 ldstr 指令位置。
        /// </summary>
        public List<StringRefInfo> FindAllStringRefs(string searchString) =>
            _refFinder.FindAllStringRefs(searchString);

        private static bool IsCompilerGenerated(TypeDef t) => DnSpyUtils.IsCompilerGenerated(t);

        private static bool IsPropertyAccessor(MethodDef m)
        {
            return m.SemanticsAttributes == MethodSemanticsAttributes.Getter
                || m.SemanticsAttributes == MethodSemanticsAttributes.Setter;
        }

        private static bool IsEventAccessor(MethodDef m)
        {
            return m.SemanticsAttributes is MethodSemanticsAttributes.AddOn
                or MethodSemanticsAttributes.RemoveOn
                or MethodSemanticsAttributes.Fire;
        }

        internal static TypeInfo ToTypeInfo(TypeDef type)
        {
            string kind;
            if (type.IsInterface) kind = "interface";
            else if (type.IsEnum) kind = "enum";
            else if (type.IsValueType) kind = "struct";
            else if (type.BaseType != null &&
                     (type.BaseType.Name == "MulticastDelegate" || type.BaseType.Name == "Delegate")) kind = "delegate";
            else kind = "class";

            return new TypeInfo
            {
                Namespace = type.Namespace,
                Name = type.Name,
                FullName = type.FullName,
                BaseTypeName = type.BaseType != null ? type.BaseType.ToString() : null,
                InterfaceNames = type.Interfaces
                    .Select(i => i.Interface != null ? i.Interface.ToString() : null)
                    .Where(s => s != null)
                    .Cast<string>()
                    .ToList(),
                Kind = kind,
                Visibility = GetTypeVisibility(type.Attributes),
                IsAbstract = type.IsAbstract,
                IsSealed = type.IsSealed,
                IsStatic = type.IsSealed && type.IsAbstract,
                IsGeneric = type.HasGenericParameters,
                MethodCount = type.Methods.Count(m => !m.IsSpecialName || IsPropertyAccessor(m) || IsEventAccessor(m)),
                FieldCount = type.Fields.Count,
                PropertyCount = type.Properties.Count,
                EventCount = type.Events.Count,
                MetadataToken = (int)type.MDToken.Raw
            };
        }

        internal static MethodInfo ToMethodInfo(MethodDef method, TypeDef declaringType)
        {
            return new MethodInfo
            {
                Name = method.Name,
                FullName = method.FullName,
                DeclaringTypeName = declaringType.FullName,
                ReturnType = method.ReturnType.ToString(),
                Parameters = method.ParamDefs.Select(p => new ParameterInfo
                {
                    Name = p.Name,
                    Type = "",
                    IsOut = false,
                    IsRef = false,
                    HasDefault = p.HasDefault,
                    DefaultValue = p.Constant != null ? p.Constant.ToString() : null
                }).ToList(),
                GenericParameters = method.GenericParameters.Select(gp => gp.Name.ToString()).ToList(),
                Visibility = GetMethodVisibility(method.Attributes),
                IsStatic = method.IsStatic,
                IsVirtual = method.IsVirtual,
                IsAbstract = method.IsAbstract,
                IsConstructor = method.IsConstructor,
                IsPropertyGetter = method.SemanticsAttributes == MethodSemanticsAttributes.Getter,
                IsPropertySetter = method.SemanticsAttributes == MethodSemanticsAttributes.Setter,
                HasBody = method.HasBody,
                MetadataToken = (int)method.MDToken.Raw,
                CodeSize = null
            };
        }

        internal static FieldInfoData ToFieldInfo(FieldDef field)
        {
            return new FieldInfoData
            {
                Name = field.Name,
                FullName = field.FullName,
                FieldType = field.FieldType.ToString(),
                Visibility = GetFieldVisibility(field.Attributes),
                IsStatic = field.IsStatic,
                IsReadOnly = (field.Attributes & FieldAttributes.InitOnly) != 0,
                IsLiteral = field.IsLiteral,
                ConstantValue = field.Constant,
                MetadataToken = (int)field.MDToken.Raw
            };
        }

        internal static PropertyInfoData ToPropertyInfo(PropertyDef prop)
        {
            return new PropertyInfoData
            {
                Name = prop.Name,
                PropertyType = prop.PropertySig != null ? prop.PropertySig.ToString() : "unknown",
                HasGet = prop.GetMethod != null,
                HasSet = prop.SetMethod != null,
                Visibility = prop.GetMethod != null ? GetMethodVisibility(prop.GetMethod.Attributes) : "private",
                MetadataToken = (int)prop.MDToken.Raw
            };
        }

        internal static EventInfoData ToEventInfo(EventDef evt)
        {
            return new EventInfoData
            {
                Name = evt.Name,
                EventType = evt.EventType.ToString(),
                Visibility = evt.AddMethod != null ? GetMethodVisibility(evt.AddMethod.Attributes) : "private",
                MetadataToken = (int)evt.MDToken.Raw
            };
        }

        internal static string GetTypeVisibility(TypeAttributes attrs)
        {
            var mask = attrs & TypeAttributes.VisibilityMask;
            if (mask == TypeAttributes.Public || mask == TypeAttributes.NestedPublic) return "public";
            if (mask == TypeAttributes.NotPublic || mask == TypeAttributes.NestedAssembly) return "internal";
            if (mask == TypeAttributes.NestedPrivate) return "private";
            if (mask == TypeAttributes.NestedFamily) return "protected";
            if (mask == TypeAttributes.NestedFamORAssem) return "protected internal";
            if (mask == TypeAttributes.NestedFamANDAssem) return "private protected";
            return "unknown";
        }

        internal static string GetMethodVisibility(MethodAttributes attrs)
        {
            var mask = attrs & MethodAttributes.MemberAccessMask;
            if (mask == MethodAttributes.Public) return "public";
            if (mask == MethodAttributes.Private) return "private";
            if (mask == MethodAttributes.Family) return "protected";
            if (mask == MethodAttributes.Assembly) return "internal";
            if (mask == MethodAttributes.FamORAssem) return "protected internal";
            if (mask == MethodAttributes.FamANDAssem) return "private protected";
            return "unknown";
        }

        internal static string GetFieldVisibility(FieldAttributes attrs)
        {
            var mask = attrs & FieldAttributes.FieldAccessMask;
            if (mask == FieldAttributes.Public) return "public";
            if (mask == FieldAttributes.Private) return "private";
            if (mask == FieldAttributes.Family) return "protected";
            if (mask == FieldAttributes.Assembly) return "internal";
            if (mask == FieldAttributes.FamORAssem) return "protected internal";
            if (mask == FieldAttributes.FamANDAssem) return "private protected";
            return "unknown";
        }
    }

    /// <summary>
    /// 未找到目标实体的异常
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}

// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
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

        public MetadataBrowserService(AssemblyLoaderService loader)
        {
            _loader = loader;
        }

        // ---- 类型查询 ----

        /// <summary>
        /// 列出程序集中所有非嵌套顶层类型。
        /// </summary>
        public List<TypeInfo> ListTypes(string assemblyPath, string namespaceFilter = null)
        {
            var module = RequireModule(assemblyPath);
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
            var module = RequireModule(assemblyPath);
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
            var module = RequireModule(assemblyPath);
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
            var module = RequireModule(assemblyPath);
            var type = FindTypeByName(module, fullTypeName);
            if (type == null) return null;
            return ToTypeInfo(type);
        }

        // ---- 成员枚举 ----

        public List<MethodInfo> ListMethods(string assemblyPath, string fullTypeName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            return type.Methods
                .Where(m => !m.IsSpecialName || IsPropertyAccessor(m) || IsEventAccessor(m))
                .Select(m => ToMethodInfo(m, type))
                .ToList();
        }

        public List<FieldInfoData> ListFields(string assemblyPath, string fullTypeName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            return type.Fields.Select(ToFieldInfo).ToList();
        }

        public List<PropertyInfoData> ListProperties(string assemblyPath, string fullTypeName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            return type.Properties.Select(ToPropertyInfo).ToList();
        }

        public List<EventInfoData> ListEvents(string assemblyPath, string fullTypeName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            return type.Events.Select(ToEventInfo).ToList();
        }

        // ---- 方法详情 ----

        public MethodInfo GetMethodInfo(string assemblyPath, string fullTypeName, string methodName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var method = type.Methods.FirstOrDefault(m =>
                m.Name == methodName || m.FullName.EndsWith("." + methodName));
            if (method == null) return null;
            return ToMethodInfo(method, type);
        }

        public List<InstructionInfo> GetMethodIL(string assemblyPath, string fullTypeName, string methodName)
        {
            var type = RequireType(assemblyPath, fullTypeName);
            var method = type.Methods.FirstOrDefault(m =>
                m.Name == methodName || m.FullName.EndsWith("." + methodName));
            if (method?.Body == null) return null;

            return method.Body.Instructions.Select(instr => new InstructionInfo
            {
                Offset = (int)instr.Offset,
                OpCode = instr.OpCode.Name,
                Operand = FormatOperand(instr.Operand)
            }).ToList();
        }

        // ---- 内部辅助 ----

        private static bool IsCompilerGenerated(TypeDef t)
        {
            return (t.Name.Contains("<") && t.Name.Contains(">"));
        }

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

        private static TypeInfo ToTypeInfo(TypeDef type)
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

        private static MethodInfo ToMethodInfo(MethodDef method, TypeDef declaringType)
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

        private static FieldInfoData ToFieldInfo(FieldDef field)
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

        private static PropertyInfoData ToPropertyInfo(PropertyDef prop)
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

        private static EventInfoData ToEventInfo(EventDef evt)
        {
            return new EventInfoData
            {
                Name = evt.Name,
                EventType = evt.EventType.ToString(),
                Visibility = evt.AddMethod != null ? GetMethodVisibility(evt.AddMethod.Attributes) : "private",
                MetadataToken = (int)evt.MDToken.Raw
            };
        }

        private static string GetTypeVisibility(TypeAttributes attrs)
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

        private static string GetMethodVisibility(MethodAttributes attrs)
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

        private static string GetFieldVisibility(FieldAttributes attrs)
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

        private static string FormatOperand(object operand)
        {
            if (operand == null) return null;

            var im = operand as IMethod;
            if (im != null) return im.FullName;

            var f = operand as IField;
            if (f != null) return f.FullName;

            var mr = operand as IMemberRef;
            if (mr != null) return mr.FullName;

            var tr = operand as ITypeDefOrRef;
            if (tr != null) return tr.ToString();

            var s = operand as string;
            if (s != null) return "\"" + s + "\"";

            if (operand is sbyte || operand is byte || operand is short || operand is ushort ||
                operand is int || operand is uint || operand is long || operand is ulong)
                return operand.ToString();

            if (operand is float) return ((float)operand).ToString("G9");
            if (operand is double) return ((double)operand).ToString("G17");

            return operand.ToString();
        }

        private ModuleDefMD RequireModule(string assemblyPath)
        {
            var module = _loader.GetModule(assemblyPath);
            if (module == null)
                throw new InvalidOperationException(
                    "Assembly not loaded: " + assemblyPath + ". Call load_assembly first.");
            return module;
        }

        private TypeDef RequireType(string assemblyPath, string fullTypeName)
        {
            var module = RequireModule(assemblyPath);
            var type = FindTypeByName(module, fullTypeName);
            if (type == null)
                throw new NotFoundException("Type '" + fullTypeName + "' not found in assembly.");
            return type;
        }

        private static TypeDef FindTypeByName(ModuleDefMD module, string fullTypeName)
        {
            var exact = module.Types.FirstOrDefault(t => t.FullName == fullTypeName);
            if (exact != null) return exact;
            return module.Types.FirstOrDefault(t =>
                t.FullName.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase));
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

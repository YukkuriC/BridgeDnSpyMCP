// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace BDSM.Models
{
    /// <summary>
    /// 程序集加载后的摘要信息
    /// </summary>
    public class AssemblyInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Location { get; set; }
        public string RuntimeVersion { get; set; }
        public int TypeCount { get; set; }
        public bool Is64Bit { get; set; }
        public ModuleKind Kind { get; set; }
    }

    /// <summary>
    /// 类型摘要信息
    /// </summary>
    public class TypeInfo
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string BaseTypeName { get; set; }
        public List<string> InterfaceNames { get; set; } = new List<string>();
        public string Kind { get; set; }
        public string Visibility { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsStatic { get; set; }
        public bool IsGeneric { get; set; }
        public int MethodCount { get; set; }
        public int FieldCount { get; set; }
        public int PropertyCount { get; set; }
        public int EventCount { get; set; }
        public int MetadataToken { get; set; }
    }

    /// <summary>
    /// 方法摘要信息
    /// </summary>
    public class MethodInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string DeclaringTypeName { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
        public List<string> GenericParameters { get; set; } = new List<string>();
        public string Visibility { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsConstructor { get; set; }
        public bool IsPropertyGetter { get; set; }
        public bool IsPropertySetter { get; set; }
        public bool HasBody { get; set; }
        public int MetadataToken { get; set; }
        public int? CodeSize { get; set; }
    }

    /// <summary>
    /// 字段摘要信息
    /// </summary>
    public class FieldInfoData
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string FieldType { get; set; }
        public string Visibility { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsLiteral { get; set; }
        public object ConstantValue { get; set; }
        public int MetadataToken { get; set; }
    }

    /// <summary>
    /// 属性摘要信息
    /// </summary>
    public class PropertyInfoData
    {
        public string Name { get; set; }
        public string PropertyType { get; set; }
        public bool HasGet { get; set; }
        public bool HasSet { get; set; }
        public string Visibility { get; set; }
        public int MetadataToken { get; set; }
    }

    /// <summary>
    /// 事件摘要信息
    /// </summary>
    public class EventInfoData
    {
        public string Name { get; set; }
        public string EventType { get; set; }
        public string Visibility { get; set; }
        public int MetadataToken { get; set; }
    }

    /// <summary>
    /// 参数信息
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOut { get; set; }
        public bool IsRef { get; set; }
        public bool HasDefault { get; set; }
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// IL 指令信息
    /// </summary>
    public class InstructionInfo
    {
        public int Offset { get; set; }
        public string OpCode { get; set; }
        public string Operand { get; set; }
    }

    /// <summary>
    /// 命名空间摘要信息
    /// </summary>
    public class NamespaceInfo
    {
        public string Name { get; set; }
        public int TypeCount { get; set; }
    }
}

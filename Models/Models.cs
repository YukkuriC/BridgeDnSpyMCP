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

    /// <summary>
    /// 成员引用位置信息（find_references 返回值）
    /// </summary>
    public class ReferenceInfo
    {
        public string ContainingType { get; set; }
        public string ContainingMethod { get; set; }
        public int Offset { get; set; }
        public string OpCode { get; set; }
        public string Operand { get; set; }
    }

    /// <summary>
    /// 字符串引用位置信息（find_all_string_refs 返回值）
    /// </summary>
    public class StringRefInfo
    {
        public string ContainingType { get; set; }
        public string ContainingMethod { get; set; }
        public int Offset { get; set; }
        public string StringValue { get; set; }
    }

    /// <summary>路径查询结果</summary>
    public class PathQueryResult
    {
        /// <summary>是否完整匹配到路径终点（true=全部段消耗完毕且Results非空，false=路径中途断掉或无结果）</summary>
        public bool Matched { get; set; }
        /// <summary>候选列表（永不为null；Matched=true时非空，Matched=false时可能为空或含断点候选）</summary>
        public List<PathQueryCandidate> Results { get; set; }
        /// <summary>原始查询路径</summary>
        public string ResolvedPath { get; set; }
    }

    /// <summary>路径查询候选</summary>
    public class PathQueryCandidate
    {
        public string MatchKind { get; set; }           // "type" | "field" | "property" | "method" | "event"
        public object Target { get; set; }              // 对应的数据模型
        public string ResolvedAssemblyPath { get; set; }
    }
}

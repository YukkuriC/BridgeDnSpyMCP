using System;
using dnlib.DotNet;
using BDSM;

namespace BDSM.Services
{
    /// <summary>
    /// 可见性转换辅助类。
    /// 统一处理 Type/Method/Field 的 visibility 属性转换逻辑。
    /// </summary>
    public static class VisibilityHelper
    {
        private const TypeAttributes TypeVisibilityMask = (TypeAttributes)0x00000007;
        private const MethodAttributes MethodVisibilityMask = (MethodAttributes)0x00000007;
        private const FieldAttributes FieldVisibilityMask = FieldAttributes.FieldAccessMask;

        // ===== 从 Attributes 获取可见性名称 =====

        /// <summary>
        /// 从 TypeAttributes 获取可见性名称。
        /// </summary>
        public static string GetTypeVisibility(TypeAttributes attrs)
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

        /// <summary>
        /// 从 MethodAttributes 获取可见性名称。
        /// </summary>
        public static string GetMethodVisibility(MethodAttributes attrs)
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

        /// <summary>
        /// 从 FieldAttributes 获取可见性名称。
        /// </summary>
        public static string GetFieldVisibility(FieldAttributes attrs)
        {
            var mask = attrs & FieldVisibilityMask;
            if (mask == FieldAttributes.Public) return "public";
            if (mask == FieldAttributes.Private) return "private";
            if (mask == FieldAttributes.Family) return "protected";
            if (mask == FieldAttributes.Assembly) return "internal";
            if (mask == FieldAttributes.FamORAssem) return "protected internal";
            if (mask == FieldAttributes.FamANDAssem) return "private protected";
            return "unknown";
        }

        // ===== 将可见性名称应用到 Attributes =====

        /// <summary>
        /// 将可见性字符串应用到 TypeAttributes。
        /// 支持两种输入格式："protected internal" 或 "protected_internal"。
        /// </summary>
        public static TypeAttributes ApplyTypeVisibility(TypeAttributes current, string visibility, bool isNested)
        {
            var cleared = current & ~TypeVisibilityMask;
            var normalized = NormalizeVisibilityName(visibility);

            if (!isNested)
            {
                switch (normalized)
                {
                    case "public": return cleared | TypeAttributes.Public;
                    case "internal": return cleared; // NotPublic = 0
                    default:
                        throw new UserException(string.Format(
                            "Non-nested type only supports 'public' or 'internal'. Got: '{0}'. " +
                            "For nested types use: public/private/protected/internal/protected internal/private protected.",
                            visibility));
                }
            }

            switch (normalized)
            {
                case "public": return cleared | TypeAttributes.NestedPublic;
                case "private": return cleared | TypeAttributes.NestedPrivate;
                case "protected": return cleared | TypeAttributes.NestedFamily;
                case "internal": return cleared | TypeAttributes.NestedAssembly;
                case "protected_internal": return cleared | TypeAttributes.NestedFamORAssem;
                case "private_protected": return cleared | TypeAttributes.NestedFamANDAssem;
                default:
                    throw new UserException(string.Format(
                        "Unknown visibility '{0}'. Supported: public/private/protected/internal/protected internal/private protected.",
                        visibility));
            }
        }

        /// <summary>
        /// 将可见性字符串应用到 MethodAttributes。
        /// 支持两种输入格式："protected internal" 或 "protected_internal"。
        /// </summary>
        public static MethodAttributes ApplyMethodVisibility(MethodAttributes current, string visibility)
        {
            var cleared = current & ~MethodVisibilityMask;
            var normalized = NormalizeVisibilityName(visibility);

            switch (normalized)
            {
                case "public": return cleared | MethodAttributes.Public;
                case "private": return cleared | MethodAttributes.Private;
                case "protected": return cleared | MethodAttributes.Family;
                case "internal": return cleared | MethodAttributes.Assembly;
                case "protected_internal": return cleared | MethodAttributes.FamORAssem;
                case "private_protected": return cleared | MethodAttributes.FamANDAssem;
                default:
                    throw new UserException(string.Format(
                        "Unknown visibility '{0}'. Supported: public/private/protected/internal/protected internal/private protected.",
                        visibility));
            }
        }

        // ===== 辅助方法 =====

        /// <summary>
        /// 将可见性名称标准化为下划线格式（用于 switch 匹配）。
        /// 支持 "protected internal" 和 "protected_internal" 两种输入。
        /// </summary>
        private static string NormalizeVisibilityName(string visibility)
        {
            return visibility.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }
}
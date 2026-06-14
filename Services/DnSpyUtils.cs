// 生成于 GLM-5V-Turbo

using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Analyzer.TreeNodes;

namespace BDSM.Services
{
    /// <summary>
    /// dnSpy 公共工具方法。复用 Publicizer 暴露的内部 API，
    /// 消除各 Service 中的重复实现。
    /// </summary>
    static class DnSpyUtils
    {
        /// <summary>
        /// 使用 dnSpy Helpers.IsCompilerGenerated 判断编译器生成类型/方法。
        /// 比 Name.Contains("&lt;") 启发式更准确。
        /// </summary>
        public static bool IsCompilerGenerated(TypeDef t) =>
            Helpers.IsCompilerGenerated(t);

        public static bool IsCompilerGenerated(MethodDef m) =>
            Helpers.IsCompilerGenerated(m);

        /// <summary>IL 操作数格式化为可读字符串。</summary>
        public static string FormatOperand(object operand)
        {
            if (operand == null) return null;
            if (operand is IMethod im) return im.FullName;
            if (operand is IField f) return f.FullName;
            if (operand is IMemberRef mr) return mr.FullName;
            if (operand is ITypeDefOrRef tr) return tr.ToString();
            if (operand is string s) return "\"" + s + "\"";
            if (operand is float fl) return fl.ToString("G9");
            if (operand is double dbl) return dbl.ToString("G17");
            if (operand is sbyte || operand is byte || operand is short || operand is ushort ||
                operand is int || operand is uint || operand is long || operand is ulong)
                return operand.ToString();
            return operand.ToString();
        }
    }
}

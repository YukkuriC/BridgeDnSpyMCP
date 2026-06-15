// 生成于 GLM-5V-Turbo

using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace BDSM.Services
{
    /// <summary>
    /// dnSpy 公共工具方法。
    /// </summary>
    static class DnSpyUtils
    {
        /// <summary>
        /// 判断编译器生成类型/方法（含 CompilerGeneratedAttribute）。
        /// 原 Helpers.IsCompilerGenerated 的等价实现（通过 dnspy-console 反编译确认）。
        /// </summary>
        public static bool IsCompilerGenerated(IHasCustomAttribute obj) =>
            obj != null && obj.CustomAttributes.IsDefined("System.Runtime.CompilerServices.CompilerGeneratedAttribute");

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

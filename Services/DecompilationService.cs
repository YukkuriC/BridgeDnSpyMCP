// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Disassembler;

namespace BDSM.Services
{
	/// <summary>
	/// 反编译服务。
	/// 基于 dnSpy / ICSharpCode.Decompiler 引擎，将已加载程序集中的类型/方法反编译为 C# 源码或 ILASM 文本。
	/// 使用公开 API 路径：AstBuilder（C#）与 ReflectionDisassembler（IL），无需经过 internal 的 CSharpDecompiler 包装类。
	/// </summary>
	public class DecompilationService
	{
		private readonly AssemblyLoaderService _loader;

		public DecompilationService(AssemblyLoaderService loader)
		{
			_loader = loader;
		}

		// ===== 公开接口（McpToolRegistry 调用） =====

		/// <summary> 将指定类型反编译为 C# 源码文本 </summary>
		public string DecompileType(string assemblyPath, string fullTypeName)
		{
			var module = RequireModule(assemblyPath);
			var type = ResolveType(module, fullTypeName);
			return DecompileTypeInternal(module, type);
		}

		/// <summary> 将单个方法反编译为 C# 源码文本 </summary>
		public string DecompileMethod(string assemblyPath, string fullTypeName, string methodName)
		{
			var module = RequireModule(assemblyPath);
			var type = ResolveType(module, fullTypeName);
			var method = ResolveMethod(type, methodName);
			return DecompileMethodInternal(module, type, method);
		}

		/// <summary> 反编译整个程序集中所有类型的 C# 源码。返回字典 key=类型全名, value=C#代码 </summary>
		public Dictionary<string, string> DecompileAssembly(string assemblyPath, string namespaceFilter = null)
		{
			var module = RequireModule(assemblyPath);
			var result = new Dictionary<string, string>();

			IEnumerable<TypeDef> types = module.Types;
			if (!string.IsNullOrEmpty(namespaceFilter))
				types = types.Where(t => string.Equals(t.Namespace, namespaceFilter, StringComparison.Ordinal));

			foreach (var type in types)
			{
				if (!IsCompilerGenerated(type))
					result[type.FullName] = DecompileTypeInternal(module, type);
			}
			return result;
		}

		/// <summary> 将单个方法输出为 ILASM 格式的中间语言文本 </summary>
		public string DecompileMethodToIL(string assemblyPath, string fullTypeName, string methodName)
		{
			var module = RequireModule(assemblyPath);
			var type = ResolveType(module, fullTypeName);
			var method = ResolveMethod(type, methodName);
			return DecompileToILInternal(method);
		}

		// ===== C# 反编译核心 =====

		private string DecompileTypeInternal(ModuleDefMD module, TypeDef type)
		{
			var output = new PlainTextOutput();
			var settings = CreateSettings(singleMember: false);
			var context = new DecompilerContext(settings.SettingsVersion, module, PlainTextColorProvider.Instance);
			context.Settings = settings;

			var builder = new AstBuilder(context);
			builder.AddType(type);
			builder.RunTransformations();
			builder.GenerateCode(output);

			return output.ToString();
		}

		private string DecompileMethodInternal(ModuleDefMD module, TypeDef type, MethodDef method)
		{
			var output = new PlainTextOutput();
			var settings = CreateSettings(singleMember: true);
			var context = new DecompilerContext(settings.SettingsVersion, module, PlainTextColorProvider.Instance);
			context.CurrentType = type;
			context.Settings = settings;

			var builder = new AstBuilder(context);
			builder.AddMethod(method);
			builder.RunTransformations();
			builder.GenerateCode(output);

			return output.ToString();
		}

		// ===== IL 反汇编核心 =====

		private string DecompileToILInternal(MethodDef method)
		{
			var output = new PlainTextOutput();
			var options = new DisassemblerOptions(1, CancellationToken.None, method.Module);
			var disassembler = new ReflectionDisassembler(output, true, options);
			disassembler.DisassembleMethod(method);
			return output.ToString();
		}

		// ===== 工厂 / 配置 =====

		private static DecompilerSettings CreateSettings(bool singleMember)
		{
			var s = new DecompilerSettings();
			if (singleMember)
			{
				s.UsingDeclarations = false;
				s.FullyQualifyAllTypes = true;
			}
			return s;
		}

		// ===== 模块/类型/方法解析 =====

		private ModuleDefMD RequireModule(string path)
		{
			var mod = _loader.GetModule(path);
			if (mod == null)
				throw new InvalidOperationException("Assembly not loaded: " + path);
			return mod;
		}

		private static TypeDef ResolveType(ModuleDefMD module, string fullName)
		{
			var exact = module.Types.FirstOrDefault(t => t.FullName == fullName);
			if (exact != null) return exact;
			return module.Types.FirstOrDefault(t =>
				t.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
				?? throw new NotFoundException("Type '" + fullName + "' not found in assembly.");
		}

		private static MethodDef ResolveMethod(TypeDef type, string name)
		{
			return type.Methods.FirstOrDefault(m =>
				m.Name == name || m.FullName.EndsWith("." + name))
				?? throw new NotFoundException("Method '" + name + "' not found in type " + type.FullName + ".");
		}

		private static bool IsCompilerGenerated(TypeDef t) => DnSpyUtils.IsCompilerGenerated(t);
	}

	// =====================================================================
	// 以下为反编译管线所需的辅助类型，均定义在本文件内以避免额外依赖
	// =====================================================================

	/// <summary>
	/// 最简 IDecompilerOutput 实现：将全部输出收集到 StringBuilder，
	/// 忽略颜色标注、引用标记、括号匹配等 IDE 特有信息。
	/// </summary>
	internal sealed class PlainTextOutput : IDecompilerOutput
	{
		private readonly StringBuilder _sb = new StringBuilder();
		private int _indent;
		private int _pos;

		public int Length => _sb.Length;
		public int NextPosition => _pos;
		public bool UsesCustomData => false;

		public void Write(string text, object color) => Append(text);
		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) => Append(text);
		public void Write(string text, int index, int length, object color) => Append(text, index, length);
		public void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) => Append(text, index, length);

		public void WriteLine()
		{
			_sb.AppendLine();
			_pos = _sb.Length;
		}

		public void IncreaseIndent() { _indent++; }
		public void DecreaseIndent() { _indent = Math.Max(0, _indent - 1); }

		public void AddBracePair(TextSpan left, TextSpan right, CodeBracesRangeFlags flags) { }
		public void AddSpanReference(object reference, int start, int end, string tag = null) { }
		public void AddLineSeparator(int position) { }
		public void AddDebugInfo(MethodDebugInfo info) { }
		public void WriteXmlDoc(string xmlDoc) { Append(xmlDoc); }
		public void AddCustomData<TData>(string key, TData data) { }

		public override string ToString() => _sb.ToString();

		private void Append(string text)
		{
			if (text == null) return;
			_sb.Append(text);
			_pos += text.Length;
		}

		private void Append(string text, int index, int length)
		{
			if (text == null) return;
			var sub = text.Substring(index, length);
			_sb.Append(sub);
			_pos += sub.Length;
		}
	}

	/// <summary>
	/// 极简 MetadataTextColorProvider 实现。
	/// 对所有元数据对象统一返回 BoxedTextColor.Text（不做语法着色），
	/// 避免 dnSpy 内部的 CSharpMetadataTextColorProvider（可能为 internal）带来的访问问题。
	/// </summary>
	internal sealed class PlainTextColorProvider : MetadataTextColorProvider
	{
		public static readonly PlainTextColorProvider Instance = new PlainTextColorProvider();

		public override object GetColor(object obj) => BoxedTextColor.Text;
	}
}

// 生成于 GLM-5V-Turbo

using System;
using BDSM;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.PE;

namespace BDSM.Services
{
    /// <summary>
    /// 程序集编辑服务。
    /// 基于 dnlib 提供重命名、成员增删、IL 编辑、保存等编辑能力。
    /// </summary>
    public class AssemblyEditorService
    {
        private readonly AssemblyLoaderService _loader;

        // OpCode 名称 -> 实例缓存。按需查找，首次访问时反射并缓存。
        private static readonly ConcurrentDictionary<string, OpCode> _opCodeCache =
            new ConcurrentDictionary<string, OpCode>(StringComparer.OrdinalIgnoreCase);

        public AssemblyEditorService(AssemblyLoaderService loader)
        {
            _loader = loader;
        }

        // ---- 重命名操作 ----

        /// <summary>
        /// 重命名类型。
        /// </summary>
        public object RenameType(string assemblyPath, string fullTypeName, string newName)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var oldName = type.Name;
            type.Name = newName;
            return new { success = true, message = string.Format("Type renamed: '{0}' -> '{1}'", oldName, newName) };
        }

        /// <summary>
        /// 修改类型的命名空间。仅对非嵌套类型有效（嵌套类型 Namespace 恒为空）。
        /// </summary>
        public object RenameTypeNamespace(string assemblyPath, string fullTypeName, string newNamespace)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            if (type.IsNested)
                throw new UserException(string.Format("Type '{0}' is a nested type (declared inside '{1}'). Nested types cannot have a namespace. Use nesting operations instead.",
                    fullTypeName, type.DeclaringType.FullName));
            var oldNs = type.Namespace;
            type.Namespace = newNamespace;
            return new { success = true, message = string.Format("Type namespace changed: '{0}' -> '{1}' ({2})", oldNs, newNamespace, type.FullName) };
        }

        /// <summary>
        /// 重命名方法。
        /// </summary>
        public object RenameMethod(string assemblyPath, string fullTypeName, string methodName, string newName)
        {
            var method = _loader.RequireMethod(assemblyPath, fullTypeName, methodName);
            var oldName = method.Name;
            method.Name = newName;
            return new { success = true, message = string.Format("Method renamed: '{0}' -> '{1}'", oldName, newName) };
        }

        /// <summary>
        /// 重命名字段。
        /// </summary>
        public object RenameField(string assemblyPath, string fullTypeName, string fieldName, string newName)
        {
            var field = _loader.RequireField(assemblyPath, fullTypeName, fieldName);
            var oldName = field.Name;
            field.Name = newName;
            return new { success = true, message = string.Format("Field renamed: '{0}' -> '{1}'", oldName, newName) };
        }

        /// <summary>
        /// 重命名属性。
        /// </summary>
        public object RenameProperty(string assemblyPath, string fullTypeName, string propertyName, string newName)
        {
            var prop = _loader.RequireProperty(assemblyPath, fullTypeName, propertyName);
            var oldName = prop.Name;
            prop.Name = newName;
            return new { success = true, message = string.Format("Property renamed: '{0}' -> '{1}'", oldName, newName) };
        }

        /// <summary>
        /// 重命名事件。
        /// </summary>
        public object RenameEvent(string assemblyPath, string fullTypeName, string eventName, string newName)
        {
            var evt = _loader.RequireEvent(assemblyPath, fullTypeName, eventName);
            var oldName = evt.Name;
            evt.Name = newName;
            return new { success = true, message = string.Format("Event renamed: '{0}' -> '{1}'", oldName, newName) };
        }

        // ---- 添加成员操作 ----

        /// <summary>
        /// 添加字段到指定类型。
        /// </summary>
        public object AddField(string assemblyPath, string fullTypeName, string fieldName, string fieldType = null, bool isStatic = false, bool isPublic = true)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var module = type.Module as ModuleDefMD;

            // 使用 Import 获取 String 类型签名
            var field = new FieldDefUser(fieldName, new FieldSig(module.Import(module.CorLibTypes.String)));

            if (isStatic) field.Attributes |= FieldAttributes.Static;
            if (isPublic) field.Attributes |= FieldAttributes.Public;
            else field.Attributes |= FieldAttributes.Private;

            type.Fields.Add(field);
            return new { success = true, message = string.Format("Field added: '{0}' (System.String)", fieldName) };
        }

        /// <summary>
        /// 添加空方法到指定类型。
        /// </summary>
        public object AddMethod(string assemblyPath, string fullTypeName, string methodName, int paramCount = 0, bool isStatic = false, bool isPublic = true, string returnType = null)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var module = type.Module as ModuleDefMD;

            var method = new MethodDefUser(methodName, MethodSig.CreateStatic(module.Import(module.CorLibTypes.Void)),
                MethodAttributes.PrivateScope | MethodAttributes.ReuseSlot);

            if (isPublic) method.Attributes |= MethodAttributes.Public;
            else method.Attributes |= MethodAttributes.Private;
            if (isStatic) method.Attributes |= MethodAttributes.Static;

            method.Body = new CilBody();
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            type.Methods.Add(method);
            return new { success = true, message = string.Format("Method added: '{0}' with {1} parameter(s)", methodName, paramCount) };
        }

        // ---- 删除成员操作 ----

        /// <summary>
        /// 删除指定方法。
        /// </summary>
        public object RemoveMember(string assemblyPath, string fullTypeName, string memberName, string memberType = "method")
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);

            switch (memberType.ToLowerInvariant())
            {
                case "method":
                    var method = AssemblyLoaderService.FindMethod(type, memberName);
                    if (method != null && type.Methods.Remove(method))
                        return new { success = true, message = string.Format("Method removed: '{0}'", memberName) };
                    break;

                case "field":
                    var field = AssemblyLoaderService.FindField(type, memberName);
                    if (field != null && type.Fields.Remove(field))
                        return new { success = true, message = string.Format("Field removed: '{0}'", memberName) };
                    break;

                case "property":
                    var prop = AssemblyLoaderService.FindProperty(type, memberName);
                    if (prop != null && type.Properties.Remove(prop))
                        return new { success = true, message = string.Format("Property removed: '{0}'", memberName) };
                    break;

                case "event":
                    var evt = AssemblyLoaderService.FindEvent(type, memberName);
                    if (evt != null && type.Events.Remove(evt))
                        return new { success = true, message = string.Format("Event removed: '{0}'", memberName) };
                    break;
            }

            throw new UserException(string.Format("{0} '{1}' not found in type '{2}'.", 
                Capitalize(memberType), memberName, fullTypeName));
        }

        // ---- IL 编辑操作 ----

        /// <summary>
        /// 替换方法的 IL 指令。
        /// - 不提供 start_offset / end_offset：替换整个方法体
        /// - 仅提供 start_offset：从该偏移处开始到方法末尾进行替换
        /// - 同时提供 start_offset + end_offset：替换 [start_offset, end_offset] 区间内的指令
        /// instructions 格式: 每行一条指令 "OpCode Operand"，如 "ldstr Hello World" 或 "ret"
        /// </summary>
        public object EditMethodIL(string assemblyPath, string fullTypeName, string methodName, List<string> instructions, int startOffset, int endOffset)
        {
            var method = _loader.RequireMethod(assemblyPath, fullTypeName, methodName);

            if (!method.HasBody)
                throw new UserException("Method has no body (abstract/external/PInvoke).");

            var module = method.Module as ModuleDefMD;
            var body = method.Body;
            var instrList = body.Instructions;

            bool hasStart = startOffset >= 0;
            bool hasEnd = endOffset >= 0;

            int startIdx = 0;
            int endIdx = instrList.Count;

            if (hasStart)
            {
                startIdx = FindInsertIndex(instrList, startOffset);
                if (startIdx >= instrList.Count && instrList.Count > 0)
                    throw new UserException(string.Format("No instruction at or after start_offset {0}.", startOffset));
            }

            if (hasEnd)
            {
                endIdx = FindInsertIndex(instrList, endOffset);
                if (endIdx <= startIdx)
                    throw new UserException(string.Format("end_offset ({0}) must be greater than start_offset ({1}).", endOffset, startOffset));
            }

            int removedCount = endIdx - startIdx;
            bool fullReplace = !hasStart && !hasEnd;

            if (fullReplace)
            {
                instrList.Clear();
                body.ExceptionHandlers.Clear();
            }
            else
            {
                for (int i = endIdx - 1; i >= startIdx; i--)
                    instrList.RemoveAt(i);
            }

            var parsed = new List<Instruction>();
            foreach (var line in instructions)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var instr = ParseInstruction(module, trimmed);
                if (instr != null)
                    parsed.Add(instr);
            }

            for (int i = parsed.Count - 1; i >= 0; i--)
                instrList.Insert(startIdx, parsed[i]);

            var range = fullReplace
                ? "full body"
                : hasEnd
                    ? string.Format("[{0}, {1})", startOffset, endOffset)
                    : string.Format("[{0}, end)", startOffset);

            return new
            {
                success = true,
                message = string.Format("IL updated for '{0}' at range {1}: replaced {2} instruction(s) with {3} instruction(s)",
                    methodName, range, removedCount, parsed.Count),
                range = range,
                removedCount = removedCount,
                insertedCount = parsed.Count,
                instructionCount = instrList.Count
            };
        }

        /// <summary>
        /// 在指定偏移位置插入 IL 指令（支持批量/单条）。
        /// - instructionText 不为 null：单条插入
        /// - instructionList 不为空：批量插入（在 offset 处按顺序插入全部指令）
        /// </summary>
        public object InsertILInstruction(string assemblyPath, string fullTypeName, string methodName, int offset, string instructionText, List<string> instructionList)
        {
            var method = _loader.RequireMethod(assemblyPath, fullTypeName, methodName);

            if (!method.HasBody)
                throw new UserException("Method has no body.");

            var module = method.Module as ModuleDefMD;
            var instrList = method.Body.Instructions;

            var toInsert = new List<Instruction>();
            if (instructionList != null && instructionList.Count > 0)
            {
                foreach (var line in instructionList)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var instr = ParseInstruction(module, trimmed);
                    if (instr == null)
                        throw new ArgumentException("Failed to parse instruction: " + trimmed);
                    toInsert.Add(instr);
                }
            }
            else if (instructionText != null)
            {
                var trimmed = instructionText.Trim();
                var instr = ParseInstruction(module, trimmed);
                if (instr == null)
                    throw new ArgumentException("Failed to parse instruction: " + instructionText);
                toInsert.Add(instr);
            }
            else
            {
                throw new UserException("Either 'instruction' or 'instructions' must be provided.");
            }

            var idx = FindInsertIndex(instrList, offset);
            for (int i = toInsert.Count - 1; i >= 0; i--)
                instrList.Insert(idx, toInsert[i]);

            var displayText = toInsert.Count == 1
                ? toInsert[0].OpCode.Name
                : string.Format("{0} instructions", toInsert.Count);

            return new
            {
                success = true,
                message = string.Format("Inserted {0} at offset {1}: {2}", toInsert.Count, offset, displayText),
                insertIndex = idx,
                insertedCount = toInsert.Count
            };
        }

        /// <summary>
        /// 删除 IL 指令（支持范围/批量/单条）。
        /// - 提供 end_offset（end_offset >= 0）：删除 [offset, end_offset) 范围内的所有指令
        /// - 提供 offsetList：删除多个偏移位置的指令
        /// - 其他情况：删除 offset 处的单条指令
        /// </summary>
        public object RemoveILInstruction(string assemblyPath, string fullTypeName, string methodName, int offset, int endOffset, List<int> offsetList)
        {
            var method = _loader.RequireMethod(assemblyPath, fullTypeName, methodName);

            if (!method.HasBody)
                throw new UserException("Method has no body.");

            var instrList = method.Body.Instructions;
            int removedCount = 0;

            if (endOffset >= 0)
            {
                if (endOffset <= offset)
                    throw new UserException("end_offset must be greater than offset.");

                int startIdx = FindInsertIndex(instrList, offset);
                int endIdx = FindInsertIndex(instrList, endOffset);

                if (startIdx >= instrList.Count)
                    throw new UserException(string.Format("No instruction at or after offset {0}.", offset));
                if (endIdx <= startIdx)
                    throw new UserException(string.Format("No instruction in range [{0}, {1}).", offset, endOffset));

                removedCount = endIdx - startIdx;
                for (int i = endIdx - 1; i >= startIdx; i--)
                    instrList.RemoveAt(i);

                return new
                {
                    success = true,
                    message = string.Format("Removed {0} instruction(s) in range [{1}, {2})", removedCount, offset, endOffset),
                    range = string.Format("[{0}, {1})", offset, endOffset),
                    removedCount = removedCount
                };
            }

            if (offsetList != null && offsetList.Count > 0)
            {
                var indices = new List<int>();
                foreach (var off in offsetList)
                {
                    var instr = instrList.FirstOrDefault(i => i.Offset == off);
                    if (instr == null)
                        throw new UserException(string.Format("No instruction at offset {0}.", off));
                    indices.Add(instrList.IndexOf(instr));
                }

                indices.Sort();
                for (int i = indices.Count - 1; i >= 0; i--)
                    instrList.RemoveAt(indices[i]);

                removedCount = indices.Count;
                return new
                {
                    success = true,
                    message = string.Format("Removed {0} instruction(s) at specified offsets", removedCount),
                    offsets = offsetList,
                    removedCount = removedCount
                };
            }

            var single = instrList.FirstOrDefault(i => i.Offset == offset);
            if (single == null)
                throw new UserException(string.Format("No instruction at offset {0}.", offset));

            instrList.Remove(single);
            return new
            {
                success = true,
                message = string.Format("Instruction removed at offset {0} ({1})", offset, single.OpCode.Name),
                removedCount = 1
            };
        }

        // ---- 可见性修改操作 ----

        /// <summary>
        /// 修改类型的访问修饰符。
        /// 支持的 visibility 值: public, internal(非嵌套), private(嵌套), protected(嵌套),
        ///   protected_internal(嵌套), private_protected(嵌套)。
        /// 非嵌套类型仅支持 public / internal；嵌套类型支持全部六种。
        /// </summary>
        public object ChangeTypeVisibility(string assemblyPath, string fullTypeName, string visibility)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var oldVis = GetTypeVisibilityName(type.Attributes);
            var newAttr = ApplyTypeVisibility(type.Attributes, visibility, type.IsNested);
            type.Attributes = newAttr;
            var newVis = GetTypeVisibilityName(newAttr);
            return new { success = true, message = string.Format("Type visibility changed: '{0}' -> '{1}' ({2})", oldVis, newVis, fullTypeName) };
        }

        /// <summary>
        /// 修改方法的访问修饰符。
        /// 支持的 visibility 值: public, private, protected, internal, protected_internal, private_protected。
        /// </summary>
        public object ChangeMethodVisibility(string assemblyPath, string fullTypeName, string methodName, string visibility)
        {
            var method = _loader.RequireMethod(assemblyPath, fullTypeName, methodName);
            var oldVis = GetMethodVisibilityName(method.Attributes);
            var newAttr = ApplyMethodVisibility(method.Attributes, visibility);
            method.Attributes = newAttr;
            var newVis = GetMethodVisibilityName(newAttr);
            return new { success = true, message = string.Format("Method visibility changed: '{0}' -> '{1}' ({2}.{3})", oldVis, newVis, fullTypeName, methodName) };
        }

        // ---- 自定义特性操作 ----

        /// <summary>
        /// 根据 member_type 解析目标成员（type/method/field/property/event）。
        /// </summary>
        private static IHasCustomAttribute ResolveTargetMember(AssemblyLoaderService loader,
            string assemblyPath, string fullTypeName, string memberName, string memberType)
        {
            var type = loader.RequireType(assemblyPath, fullTypeName);
            return memberType.ToLowerInvariant() switch
            {
                "type"     => type,
                "method"   => loader.RequireMethod(assemblyPath, fullTypeName, memberName),
                "field"    => loader.RequireField(assemblyPath, fullTypeName, memberName),
                "property" => loader.RequireProperty(assemblyPath, fullTypeName, memberName),
                "event"    => loader.RequireEvent(assemblyPath, fullTypeName, memberName),
                _ => throw new UserException(string.Format("Unsupported member_type: '{0}'. Supported: type/method/field/property/event.", memberType))
            };
        }

        public object AddCustomAttribute(string assemblyPath, string fullTypeName, string memberName,
            string memberType, string attributeTypeName, List<object> constructorArgs = null,
            Dictionary<string, object> namedArgs = null)
        {
            var type = _loader.RequireType(assemblyPath, fullTypeName);
            var module = type.Module as ModuleDefMD;

            var target = ResolveTargetMember(_loader, assemblyPath, fullTypeName, memberName, memberType);

            var ctorSig = BuildCtorSignature(module, constructorArgs);

            // 优先尝试在已加载程序集中解析为 TypeDef（同程序集或显式引用）
            var attrTypeDef = ResolveAttributeType(module, attributeTypeName);
            MethodDef ctorDef = null;
            if (attrTypeDef != null)
                ctorDef = FindMatchingConstructor(attrTypeDef, ctorSig);

            // 回退：通过 TypeRef + MemberRef 创建跨程序集引用（适用于 CorLib 类型等）
            CustomAttribute ca;
            if (ctorDef != null)
            {
                ca = new CustomAttribute(ctorDef);
            }
            else
            {
                var typeRef = ResolveCorLibTypeRef(module, attributeTypeName);
                if (typeRef == null)
                    throw new UserException(string.Format("Attribute type '{0}' not found in assembly, its references, or corlib.", attributeTypeName));
                var memberRef = new MemberRefUser(module, ".ctor", ctorSig);
                memberRef.Class = typeRef;
                ca = new CustomAttribute(memberRef);
            }

            // 填充构造函数参数
            if (constructorArgs != null && constructorArgs.Count > 0)
            {
                foreach (var arg in constructorArgs)
                    ca.ConstructorArguments.Add(CreateCAArgument(module, arg));
            }

            if (namedArgs != null)
            {
                foreach (var kvp in namedArgs)
                {
                    var caArg = CreateCAArgument(module, kvp.Value);
                    ca.NamedArguments.Add(new CANamedArgument(false, caArg.Type, kvp.Key, caArg));
                }
            }

            target.CustomAttributes.Add(ca);
            return new { success = true, message = string.Format("Custom attribute '{0}' added to {1} '{2}'.", attributeTypeName, memberType, memberName) };
        }

        /// <summary>
        /// 删除成员的自定义特性。
        /// 按 attribute_type_name 匹配删除（删除所有匹配的特性实例）。
        /// 若未提供 attribute_type_name，则删除该成员的所有自定义特性。
        /// </summary>
        public object RemoveCustomAttribute(string assemblyPath, string fullTypeName, string memberName,
            string memberType, string attributeTypeName = null)
        {
            var target = ResolveTargetMember(_loader, assemblyPath, fullTypeName, memberName, memberType);

            int removedCount;
            if (attributeTypeName == null)
            {
                removedCount = target.CustomAttributes.Count;
                target.CustomAttributes.Clear();
            }
            else
            {
                var toRemove = target.CustomAttributes.Where(ca =>
                {
                    if (ca.Constructor == null || ca.Constructor.DeclaringType == null) return false;
                    return string.Equals(ca.Constructor.DeclaringType.FullName, attributeTypeName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ca.Constructor.DeclaringType.ReflectionFullName, attributeTypeName, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                removedCount = toRemove.Count;
                foreach (var ca in toRemove)
                    target.CustomAttributes.Remove(ca);
            }

            return new
            {
                success = true,
                message = string.Format("Removed {0} custom attribute(s) from {1} '{2}'.", removedCount, memberType, memberName),
                removedCount = removedCount
            };
        }

        // ---- 保存操作 ----

        /// <summary>
        /// 将修改后的程序集保存到指定路径。
        /// 当 outputPath 与原始路径相同时，自动释放内存映射锁以支持原位覆盖。
        /// </summary>
        public object SaveAssembly(string assemblyPath, string outputPath)
        {
            var module = _loader.GetModule(assemblyPath);

            var fullPath = Path.GetFullPath(outputPath);
            var originalPath = Path.GetFullPath(module.Location);
            bool inPlace = string.Equals(fullPath, originalPath, StringComparison.OrdinalIgnoreCase);

            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (inPlace)
                DisableMemoryMappedIO(module);

            var options = new ModuleWriterOptions(module);
            var writer = new ModuleWriter(module, options);
            writer.Write(fullPath);

            return new
            {
                success = true,
                message = string.Format("Assembly saved to: {0} ({1})", fullPath, inPlace ? "in-place" : "new file"),
                outputPath = fullPath,
                originalPath = originalPath,
                inPlace = inPlace
            };
        }

        // ---- 辅助方法 ----

        /// <summary>
        /// 释放模块的内存映射 I/O 锁，使同进程可原位写入该文件。
        /// 原理：ModuleDefMD.Load() 默认使用 Memory-Mapped File 加载 PE 文件，
        /// Windows 会持有 FILE_LOCK_Mapped 锁。通过 IInternalPEImage.UnsafeDisableMemoryMappedIO()
        /// 可释放该锁（与 dnSpy 原位保存机制一致）。
        /// </summary>
        private static void DisableMemoryMappedIO(ModuleDefMD module)
        {
            // MetadataBase.peImage 是 protected 字段，类型为 IPEImage
            var metadata = module.Metadata;
            var peImageField = metadata.GetType().GetField("peImage",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (peImageField == null) return;

            var peImage = peImageField.GetValue(metadata) as IPEImage;
            if (peImage == null) return;

            var internalPeImage = peImage as IInternalPEImage;
            if (internalPeImage != null && internalPeImage.IsMemoryMappedIO)
                internalPeImage.UnsafeDisableMemoryMappedIO();
        }

        private static Instruction ParseInstruction(ModuleDefMD module, string text)
        {
            var parts = text.Split(new[] { ' ' }, 2);
            var opCodeName = ToPascalCaseOpCode(parts[0]);
            var operandStr = parts.Length > 1 ? parts[1] : null;

            // dnlib OpCodes 使用 PascalCase 命名（如 Ldstr, Callvirt, Ldc_I4_S），按需反射并缓存
            if (!_opCodeCache.TryGetValue(opCodeName, out var opCode))
            {
                var opCodeField = typeof(OpCodes).GetField(opCodeName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (opCodeField == null)
                    throw new UserException("Unknown opcode: " + opCodeName + " (original: " + parts[0] + ")");
                opCode = (OpCode)opCodeField.GetValue(null);
                _opCodeCache[opCodeName] = opCode;
            }

            if (operandStr == null || (int)opCode.OperandType == 0)
                return Instruction.Create(opCode);

            switch (opCode.OperandType)
            {
                case OperandType.InlineString:
                    return Instruction.Create(opCode, operandStr);

                case OperandType.ShortInlineI:
                case OperandType.InlineI:
                    int intVal;
                    if (int.TryParse(operandStr, out intVal))
                        return Instruction.Create(opCode, intVal);
                    break;

                case OperandType.ShortInlineR:
                case OperandType.InlineR:
                    double dblVal;
                    if (double.TryParse(operandStr,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out dblVal))
                        return Instruction.Create(opCode, dblVal);
                    break;

                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineBrTarget:
                    // Branch target - placeholder, will need fixup
                    return Instruction.Create(opCode, (int)0);

                default:
                    return Instruction.Create(opCode);
            }

            return Instruction.Create(opCode);
        }

        private static int FindInsertIndex(IList<Instruction> instructions, int offset)
        {
            for (var i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Offset >= offset)
                    return i;
            }
            return instructions.Count;
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1).ToLowerInvariant();
        }

        /// <summary>
        /// 将标准 IL opcode 名转换为 dnlib OpCodes 的 PascalCase 字段名。
        /// 例如: ldstr -> Ldstr, callvirt -> Callvirt, ldc.i4.s -> Ldc_I4_S, constrained. -> Constrained
        /// </summary>
        private static string ToPascalCaseOpCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            // 替换点号为下划线: ldc.i4.s -> ldc_i4_s
            var converted = name.Replace('.', '_');
            // 首字母大写，其余保持原样（dnlib 字段名是 PascalCase）
            return char.ToUpper(converted[0]) + converted.Substring(1);
        }

        // ---- 可见性辅助方法 ----

        private const TypeAttributes TypeVisibilityMask = (TypeAttributes)0x00000007;
        private const MethodAttributes MethodVisibilityMask = (MethodAttributes)0x00000007;

        private static string GetTypeVisibilityName(TypeAttributes attr)
        {
            var vis = attr & TypeVisibilityMask;
            if ((attr & TypeAttributes.VisibilityMask) != 0) // nested
            {
                switch (vis)
                {
                    case TypeAttributes.NestedPublic: return "public";
                    case TypeAttributes.NestedPrivate: return "private";
                    case TypeAttributes.NestedFamily: return "protected";
                    case TypeAttributes.NestedAssembly: return "internal";
                    case TypeAttributes.NestedFamANDAssem: return "private_protected";
                    case TypeAttributes.NestedFamORAssem: return "protected_internal";
                }
            }
            else
            {
                switch (vis)
                {
                    case TypeAttributes.Public: return "public";
                    default: return "internal";
                }
            }
            return "unknown";
        }

        private static string GetMethodVisibilityName(MethodAttributes attr)
        {
            switch (attr & MethodVisibilityMask)
            {
                case MethodAttributes.Private: return "private";
                case MethodAttributes.FamANDAssem: return "private_protected";
                case MethodAttributes.Assembly: return "internal";
                case MethodAttributes.Family: return "protected";
                case MethodAttributes.FamORAssem: return "protected_internal";
                case MethodAttributes.Public: return "public";
                default: return "private_scope";
            }
        }

        private static TypeAttributes ApplyTypeVisibility(TypeAttributes current, string visibility, bool isNested)
        {
            var cleared = current & ~TypeVisibilityMask;
            var lower = visibility.ToLowerInvariant().Replace("-", "_");

            if (!isNested)
            {
                switch (lower)
                {
                    case "public": return cleared | TypeAttributes.Public;
                    case "internal": return cleared; // NotPublic = 0
                    default:
                        throw new UserException(string.Format("Non-nested type only supports 'public' or 'internal'. Got: '{0}'. For nested types use: public/private/protected/internal/protected_internal/private_protected.", visibility));
                }
            }

            switch (lower)
            {
                case "public": return cleared | TypeAttributes.NestedPublic;
                case "private": return cleared | TypeAttributes.NestedPrivate;
                case "protected": return cleared | TypeAttributes.NestedFamily;
                case "internal": return cleared | TypeAttributes.NestedAssembly;
                case "protected_internal": return cleared | TypeAttributes.NestedFamORAssem;
                case "private_protected": return cleared | TypeAttributes.NestedFamANDAssem;
                default:
                    throw new UserException(string.Format("Unknown visibility '{0}'. Supported: public/private/protected/internal/protected_internal/private_protected.", visibility));
            }
        }

        private static MethodAttributes ApplyMethodVisibility(MethodAttributes current, string visibility)
        {
            var cleared = current & ~MethodVisibilityMask;
            var lower = visibility.ToLowerInvariant().Replace("-", "_");
            switch (lower)
            {
                case "public": return cleared | MethodAttributes.Public;
                case "private": return cleared | MethodAttributes.Private;
                case "protected": return cleared | MethodAttributes.Family;
                case "internal": return cleared | MethodAttributes.Assembly;
                case "protected_internal": return cleared | MethodAttributes.FamORAssem;
                case "private_protected": return cleared | MethodAttributes.FamANDAssem;
                default:
                    throw new UserException(string.Format("Unknown visibility '{0}'. Supported: public/private/protected/internal/protected_internal/private_protected.", visibility));
            }
        }

        // ---- 自定义特性辅助方法 ----

        private static TypeDef ResolveAttributeType(ModuleDefMD module, string attributeTypeName)
        {
            // 先在当前模块中查找
            var type = module.Find(attributeTypeName, false);
            if (type != null) return type;

            // 在引用的程序集中查找
            foreach (var asmRef in module.GetAssemblyRefs())
            {
                var resolved = module.Context.AssemblyResolver.Resolve(asmRef, module);
                if (resolved == null) continue;

                foreach (var mod in resolved.Modules)
                {
                    var modTypeDef = mod as ModuleDefMD;
                    if (modTypeDef == null) continue;
                    var found = modTypeDef.Find(attributeTypeName, false);
                    if (found != null) return found;
                }
            }

            // 尝试解析为 CorLib 类型（如 System.ObsoleteAttribute）
            var corLibType = ResolveCorLibTypeRef(module, attributeTypeName);
            if (corLibType != null)
            {
                var resolvedTypeDef = corLibType.Resolve();
                if (resolvedTypeDef != null) return resolvedTypeDef;
            }

            // 未找到，返回 null 由调用方决定是否回退到 TypeRef+MemberRef 路径
            return null;
        }

        /// <summary>
        /// 将全限定类型名拆分为命名空间和类型名，通过 CorLibTypes.GetTypeRef(ns, name) 获取 TypeRef。
        /// </summary>
        private static TypeRef ResolveCorLibTypeRef(ModuleDefMD module, string fullTypeName)
        {
            var dotIndex = fullTypeName.LastIndexOf('.');
            if (dotIndex < 0)
                return module.CorLibTypes.GetTypeRef("", fullTypeName);
            var ns = fullTypeName.Substring(0, dotIndex);
            var name = fullTypeName.Substring(dotIndex + 1);
            return module.CorLibTypes.GetTypeRef(ns, name);
        }

        private static MethodSig BuildCtorSignature(ModuleDefMD module, List<object> args)
        {
            if (args == null || args.Count == 0)
                return MethodSig.CreateInstance(module.CorLibTypes.Void);

            var sig = MethodSig.CreateInstance(module.CorLibTypes.Void);
            foreach (var arg in args)
            {
                sig.Params.Add(InferParamType(module, arg));
            }
            return sig;
        }

        private static TypeSig InferParamType(ModuleDefMD module, object value)
        {
            if (value is bool) return module.CorLibTypes.Boolean;
            if (value is int || value is long) return module.CorLibTypes.Int32;
            if (value is double || value is float) return module.CorLibTypes.Double;
            if (value is string) return module.CorLibTypes.String;
            if (value is System.Collections.IList list && list.Count > 0)
                return new SZArraySig(InferParamType(module, list[0]));
            return module.CorLibTypes.Object;
        }

        private static MethodDef FindMatchingConstructor(TypeDef attrTypeDef, MethodSig expectedSig)
        {
            foreach (var method in attrTypeDef.Methods)
            {
                if (!method.IsConstructor || method.IsStatic) continue;
                var methodSig = method.MethodSig;
                if (methodSig == null) continue;
                if (ParamsMatch(methodSig.Params, expectedSig.Params))
                    return method;
            }
            return null;
        }

        private static bool ParamsMatch(IList<TypeSig> actual, IList<TypeSig> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                if (!IsCompatibleType(actual[i], expected[i]))
                    return false;
            }
            return true;
        }

        private static bool IsCompatibleType(TypeSig actual, TypeSig expected)
        {
            // 宽松匹配：基本类型兼容即可
            var a = actual.RemovePinnedAndModifiers().GetElementType();
            var e = expected.RemovePinnedAndModifiers().GetElementType();

            // 数组类型特殊处理
            if (a == ElementType.SZArray && e == ElementType.SZArray) return true;
            if (a == ElementType.Array && e == ElementType.Array) return true;

            // 数值类型之间互相兼容（签名不精确匹配时用 object 兜底）
            if (IsNumericElement(a) && IsNumericElement(e)) return true;
            if (a == e) return true;
            if (e == ElementType.Object) return true; // object 兼容所有

            return false;
        }

        private static bool IsNumericElement(ElementType et)
        {
            switch (et)
            {
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                    return true;
                default:
                    return false;
            }
        }

        private static CAArgument CreateCAArgument(ModuleDefMD module, object value) => value switch
        {
            bool b   => new(module.CorLibTypes.Boolean, b),
            int i    => new(module.CorLibTypes.Int32, i),
            long l   => new(module.CorLibTypes.Int64, l),
            double d => new(module.CorLibTypes.Double, d),
            float f  => new(module.CorLibTypes.Single, f),
            string s => new(module.CorLibTypes.String, s),
            System.Collections.IList list => CreateArrayCAArg(module, list),
            _ => new(module.CorLibTypes.Object, value?.ToString() ?? "")
        };

        private static CAArgument CreateArrayCAArg(ModuleDefMD module, System.Collections.IList list)
        {
            var elemType = list.Count > 0 ? InferParamType(module, list[0]) : module.CorLibTypes.Object;
            var elements = new List<CAArgument>();
            foreach (var item in list)
                elements.Add(CreateCAArgument(module, item));
            return new CAArgument(new SZArraySig(elemType), elements);
        }
    }
}

// 生成于 GLM-5V-Turbo

using System;
using BDSM;
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

            OpCode opCode;
            try
            {
                // dnlib OpCodes 使用 PascalCase 命名（如 Ldstr, Callvirt, Ldc_I4_S）
                var opCodeField = typeof(OpCodes).GetField(opCodeName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (opCodeField == null)
                    throw new UserException("Unknown opcode: " + opCodeName + " (original: " + parts[0] + ")");
                opCode = (OpCode)opCodeField.GetValue(null);
            }
            catch (ArgumentException)
            {
                throw new UserException("Unknown opcode: " + opCodeName + " (original: " + parts[0] + ")");
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
    }
}

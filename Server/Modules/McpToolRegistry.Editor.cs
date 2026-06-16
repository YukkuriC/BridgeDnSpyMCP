// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>程序集编辑模块 -- rename / add / remove / edit_il / save</summary>
    public partial class McpToolRegistry
    {
        private const string DnlibOffsetNote = "注：dnlib 在写入 PE 文件之前不会重新计算 Instruction.Offset，因此 get_method_il 对新插入/修改过的指令可能显示 offset=0，这是正常现象；若需要真实偏移，请先通过 save_assembly 保存再重新加载。";

        internal void RegisterEditorTools(List<Tool> tools)
        {
            // ---- 重命名工具 ----
            tools.Add(MakeTool("rename_type",
                "重命名指定类型。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"new_name", new PropertySchema{ Type="string", Description="新的类型名称"}}
                },
                new List<string> {"assembly_path", "full_type_name", "new_name"}));

            tools.Add(MakeTool("rename_method",
                "重命名指定方法。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"new_name", new PropertySchema{ Type="string", Description="新的方法名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name", "new_name"}));

            tools.Add(MakeTool("rename_field",
                "重命名字段。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"field_name", new PropertySchema{ Type="string", Description="字段名"}},
                    {"new_name", new PropertySchema{ Type="string", Description="新的字段名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "field_name", "new_name"}));

            tools.Add(MakeTool("rename_property",
                "重命名属性。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"property_name", new PropertySchema{ Type="string", Description="属性名"}},
                    {"new_name", new PropertySchema{ Type="string", Description="新的属性名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "property_name", "new_name"}));

            tools.Add(MakeTool("rename_event",
                "重命名事件。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"event_name", new PropertySchema{ Type="string", Description="事件名"}},
                    {"new_name", new PropertySchema{ Type="string", Description="新的事件名"}}
                },
                new List<string> {"assembly_path", "full_type_name", "event_name", "new_name"}));

            // ---- 添加成员工具 ----
            tools.Add(MakeTool("add_field",
                "添加字段到指定类型。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"field_name", new PropertySchema{ Type="string", Description="字段名称"}},
                    {"field_type", new PropertySchema{ Type="string", Description="字段类型（可选，默认 System.String）"}},
                    {"is_static", new PropertySchema{ Type="boolean", Description="是否为静态字段（默认 false）"}},
                    {"is_public", new PropertySchema{ Type="boolean", Description="是否为公有（默认 true）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "field_name"}));

            tools.Add(MakeTool("add_method",
                "添加空方法到指定类型（仅含 ret 指令）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名称"}},
                    {"param_count", new PropertySchema{ Type="integer", Description="参数数量（默认 0）"}},
                    {"is_static", new PropertySchema{ Type="boolean", Description="是否为静态方法（默认 false）"}},
                    {"is_public", new PropertySchema{ Type="boolean", Description="是否为公有（默认 true）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name"}));

            // ---- 删除成员工具 ----
            tools.Add(MakeTool("remove_member",
                "删除指定成员（方法/字段/属性/事件）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"member_name", new PropertySchema{ Type="string", Description="成员名称"}},
                    {"member_type", new PropertySchema{ Type="string", Description="成员类型: method/field/property/event（默认 method）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "member_name"}));

            // ---- IL 编辑工具 ----
            tools.Add(MakeTool("edit_method_il",
                "替换方法的 IL 指令（支持范围）。不提供 start_offset/end_offset 则替换整个方法体；仅提供 start_offset 则从该偏移到方法末尾；同时提供两者则替换 [start_offset, end_offset) 区间。每行格式: \"OpCode Operand\" 或仅 \"OpCode\"。" + DnlibOffsetNote,
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"instructions", new PropertySchema{ Type="array", Items=new PropertySchema{Type="string"}, Description="IL 指令列表，如 [\"ldstr Hello\", \"ret\"]"}},
                    {"start_offset", new PropertySchema{ Type="integer", Description="替换范围的起始偏移量（可选，留空则从方法开头）"}},
                    {"end_offset", new PropertySchema{ Type="integer", Description="替换范围的结束偏移量（可选，留空则到方法末尾）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name", "instructions"}));

            tools.Add(MakeTool("insert_il_instruction",
                "在指定偏移位置插入一条或多条 IL 指令。提供 instruction 插入单条；提供 instructions 数组则批量插入。" + DnlibOffsetNote,
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"offset", new PropertySchema{ Type="integer", Description="插入位置的 IL 偏移量"}},
                    {"instruction", new PropertySchema{ Type="string", Description="单条指令文本，如 \"nop\" 或 \"ldstr Hello\"（与 instructions 二选一）"}},
                    {"instructions", new PropertySchema{ Type="array", Items=new PropertySchema{Type="string"}, Description="批量指令列表（与 instruction 二选一）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name", "offset"}));

            tools.Add(MakeTool("remove_il_instruction",
                "删除 IL 指令（支持范围/批量/单条）。提供 offset + end_offset 删除 [offset, end_offset) 范围；提供 offsets 数组删除多个指定偏移；仅提供 offset 删除单条指令。" + DnlibOffsetNote,
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"offset", new PropertySchema{ Type="integer", Description="要删除的 IL 偏移量（或范围起点，与 end_offset/offsets 配合时可选）"}},
                    {"end_offset", new PropertySchema{ Type="integer", Description="范围删除的结束偏移量（可选）"}},
                    {"offsets", new PropertySchema{ Type="array", Items=new PropertySchema{Type="integer"}, Description="批量删除的偏移量数组（可选）"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name"}));

            // ---- 保存工具 ----
            tools.Add(MakeTool("save_assembly",
                "将修改后的程序集保存到新路径（不覆盖原文件）。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"output_path", new PropertySchema{ Type="string", Description="输出文件路径"}}
                },
                new List<string> {"assembly_path", "output_path"}));

            _dispatchers.Add(DispatchEditor);
        }

        private bool DispatchEditor(string toolName, Dictionary<string, object> args, out object result)
        {
            switch (toolName)
            {
                case "rename_type":         result = HandleRenameType(args); return true;
                case "rename_method":       result = HandleRenameMethod(args); return true;
                case "rename_field":        result = HandleRenameField(args); return true;
                case "rename_property":     result = HandleRenameProperty(args); return true;
                case "rename_event":        result = HandleRenameEvent(args); return true;
                case "add_field":           result = HandleAddField(args); return true;
                case "add_method":          result = HandleAddMethod(args); return true;
                case "remove_member":       result = HandleRemoveMember(args); return true;
                case "edit_method_il":      result = HandleEditMethodIL(args); return true;
                case "insert_il_instruction": result = HandleInsertILInstruction(args); return true;
                case "remove_il_instruction": result = HandleRemoveILInstruction(args); return true;
                case "save_assembly":       result = HandleSaveAssembly(args); return true;
                default: result = null; return false;
            }
        }

        private object HandleRenameType(Dictionary<string, object> args)
        {
            return _editor.RenameType(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "new_name"));
        }

        private object HandleRenameMethod(Dictionary<string, object> args)
        {
            return _editor.RenameMethod(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"),
                GetRequiredArg<string>(args, "new_name"));
        }

        private object HandleRenameField(Dictionary<string, object> args)
        {
            return _editor.RenameField(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "field_name"),
                GetRequiredArg<string>(args, "new_name"));
        }

        private object HandleRenameProperty(Dictionary<string, object> args)
        {
            return _editor.RenameProperty(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "property_name"),
                GetRequiredArg<string>(args, "new_name"));
        }

        private object HandleRenameEvent(Dictionary<string, object> args)
        {
            return _editor.RenameEvent(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "event_name"),
                GetRequiredArg<string>(args, "new_name"));
        }

        private object HandleAddField(Dictionary<string, object> args)
        {
            return _editor.AddField(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "field_name"),
                GetOptionalArg<string>(args, "field_type"),
                GetOptionalArg<bool>(args, "is_static"),
                GetOptionalArg<bool>(args, "is_public"));
        }

        private object HandleAddMethod(Dictionary<string, object> args)
        {
            return _editor.AddMethod(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"),
                GetOptionalArg<int>(args, "param_count"),
                GetOptionalArg<bool>(args, "is_static"),
                GetOptionalArg<bool>(args, "is_public"));
        }

        private object HandleRemoveMember(Dictionary<string, object> args)
        {
            return _editor.RemoveMember(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "member_name"),
                GetOptionalArg<string>(args, "member_type") ?? "method");
        }

        private object HandleEditMethodIL(Dictionary<string, object> args)
        {
            var instrList = GetRequiredArg<object>(args, "instructions");
            var instructions = new List<string>();
            if (instrList is System.Collections.IList list)
            {
                foreach (var item in list)
                    instructions.Add(item.ToString());
            }
            else if (instrList is string str)
            {
                instructions.AddRange(str.Split('\n'));
            }

            int startOffset = GetOptionalArg<int>(args, "start_offset");
            int endOffset = GetOptionalArg<int>(args, "end_offset");
            // -1 作为 sentinel，表示未提供
            if (!args.ContainsKey("start_offset") || args["start_offset"] == null) startOffset = -1;
            if (!args.ContainsKey("end_offset") || args["end_offset"] == null) endOffset = -1;

            return _editor.EditMethodIL(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"),
                instructions,
                startOffset,
                endOffset);
        }

        private object HandleInsertILInstruction(Dictionary<string, object> args)
        {
            var assemblyPath = GetRequiredArg<string>(args, "assembly_path");
            var fullTypeName = GetRequiredArg<string>(args, "full_type_name");
            var methodName = GetRequiredArg<string>(args, "method_name");
            var offset = GetRequiredArg<int>(args, "offset");
            var instruction = GetOptionalArg<string>(args, "instruction");

            List<string> instructionList = null;
            var rawArr = GetOptionalArg<object>(args, "instructions");
            if (rawArr is System.Collections.IList list && list.Count > 0)
            {
                instructionList = new List<string>();
                foreach (var item in list)
                    instructionList.Add(item.ToString());
            }

            return _editor.InsertILInstruction(assemblyPath, fullTypeName, methodName, offset, instruction, instructionList);
        }

        private object HandleRemoveILInstruction(Dictionary<string, object> args)
        {
            var assemblyPath = GetRequiredArg<string>(args, "assembly_path");
            var fullTypeName = GetRequiredArg<string>(args, "full_type_name");
            var methodName = GetRequiredArg<string>(args, "method_name");

            bool hasOffset = args.ContainsKey("offset") && args["offset"] != null;
            int offset = hasOffset ? ConvertValue<int>(args["offset"]) : -1;

            int endOffset = -1;
            if (args.ContainsKey("end_offset") && args["end_offset"] != null)
                endOffset = ConvertValue<int>(args["end_offset"]);

            List<int> offsetList = null;
            var rawArr = GetOptionalArg<object>(args, "offsets");
            if (rawArr is System.Collections.IList list && list.Count > 0)
            {
                offsetList = new List<int>();
                foreach (var item in list)
                    offsetList.Add(ConvertValue<int>(item));
            }

            if (!hasOffset && endOffset < 0 && (offsetList == null || offsetList.Count == 0))
                throw new ArgumentException("At least one of 'offset', 'end_offset' or 'offsets' must be provided.");

            return _editor.RemoveILInstruction(assemblyPath, fullTypeName, methodName, offset, endOffset, offsetList);
        }

        private object HandleSaveAssembly(Dictionary<string, object> args)
        {
            return _editor.SaveAssembly(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "output_path"));
        }
    }
}

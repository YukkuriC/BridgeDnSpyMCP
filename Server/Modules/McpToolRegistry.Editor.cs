// 生成于 GLM-5V-Turbo

using System.Collections.Generic;
using BDSM.Services;
using BDSM.Server.Protocol;

namespace BDSM.Server
{
    /// <summary>程序集编辑模块 -- rename / add / remove / edit_il / save</summary>
    public partial class McpToolRegistry
    {
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
                "替换方法的全部 IL 指令。每行格式: \"OpCode Operand\" 或仅 \"OpCode\"。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"instructions", new PropertySchema{ Type="array", Items=new PropertySchema{Type="string"}, Description="IL 指令列表，如 [\"ldstr Hello\", \"ret\"]"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name", "instructions"}));

            tools.Add(MakeTool("insert_il_instruction",
                "在指定偏移位置插入一条 IL 指令。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"offset", new PropertySchema{ Type="integer", Description="插入位置的 IL 偏移量"}},
                    {"instruction", new PropertySchema{ Type="string", Description="要插入的指令文本，如 \"nop\" 或 \"ldstr Hello\""}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name", "offset", "instruction"}));

            tools.Add(MakeTool("remove_il_instruction",
                "删除指定偏移位置的一条 IL 指令。",
                new Dictionary<string, PropertySchema>
                {
                    {"assembly_path", new PropertySchema{ Type="string", Description="已加载的程序集路径"}},
                    {"full_type_name", new PropertySchema{ Type="string", Description="类型的全限定名"}},
                    {"method_name", new PropertySchema{ Type="string", Description="方法名"}},
                    {"offset", new PropertySchema{ Type="integer", Description="要删除的 IL 偏移量"}}
                },
                new List<string> {"assembly_path", "full_type_name", "method_name", "offset"}));

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

            return _editor.EditMethodIL(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"),
                instructions);
        }

        private object HandleInsertILInstruction(Dictionary<string, object> args)
        {
            return _editor.InsertILInstruction(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"),
                GetRequiredArg<int>(args, "offset"),
                GetRequiredArg<string>(args, "instruction"));
        }

        private object HandleRemoveILInstruction(Dictionary<string, object> args)
        {
            return _editor.RemoveILInstruction(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "full_type_name"),
                GetRequiredArg<string>(args, "method_name"),
                GetRequiredArg<int>(args, "offset"));
        }

        private object HandleSaveAssembly(Dictionary<string, object> args)
        {
            return _editor.SaveAssembly(
                GetRequiredArg<string>(args, "assembly_path"),
                GetRequiredArg<string>(args, "output_path"));
        }
    }
}

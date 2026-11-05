import * as monaco from "monaco-editor";
import {IActionProvider} from "@application";

export class BuiltinActionProvider implements IActionProvider {
    public provideActions(): monaco.editor.IActionDescriptor[] {
        return [
            {
                id: "netpad.action.transformToUpperOrLowercase",
                label: "Transform to Upper/Lower Case",
                keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyY],
                run: (editor) => {
                    const model = editor?.getModel();
                    const currentSelection = editor?.getSelection();

                    if (!editor || !model || !currentSelection) return;

                    const selectedValue = model.getValueInRange(currentSelection);

                    if (!selectedValue.trim()) return;

                    if (selectedValue === selectedValue.toLowerCase()) {
                        editor.trigger(null, "editor.action.transformToUppercase", null);
                    } else {
                        editor.trigger(null, "editor.action.transformToLowercase", null);
                    }
                }
            }
        ];
    }
}

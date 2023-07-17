import {CreateScriptDto, IScriptService, ISettingService} from "@domain";
import {KeyCode} from "@common";
import {Shortcut} from "./shortcut";
import {EditorUtil} from "../editor/editor-util";
import {ITextEditorService} from "../editor/text-editor-service";
import {Explorer, NamespacesPane, OutputPane} from "../../../windows/main/panes";
import {RunScriptEvent, TogglePaneEvent} from "@application"
import * as monaco from "monaco-editor";

export const BuiltinShortcuts = [
    new Shortcut("Command Palette")
        .withKey(KeyCode.F1)
        .hasAction(ctx => {
            const editor = ctx.container.get(ITextEditorService).active?.monaco;

            if (!editor) return;

            editor.focus();
            editor.trigger("", "editor.action.quickCommand", null);
        })
        .configurable(false)
        .enabled(),

    new Shortcut("Go to Script")
        .withCtrlKey()
        .withKey(KeyCode.KeyT)
        .hasAction(ctx => {
            const activeScriptId = ctx.session.active?.script.id;
            if (!activeScriptId) {
                return;
            }

            const editors = monaco.editor.getEditors();
            if (!editors.length) {
                return;
            }

            let editor = editors.find(e => {
                const model = e.getModel();
                return !model ? false : (EditorUtil.getScriptId(model) === activeScriptId);
            })

            if (!editor) {
                editor = editors.find(e => e.hasTextFocus() || e.hasWidgetFocus()) || editors[0];
            }

            editor.focus();
            editor.getAction("builtin.actions.goToScript")?.run();
        })
        .configurable(false)
        .enabled(),

    new Shortcut("New")
        .withCtrlKey()
        .withKey(KeyCode.KeyN)
        .hasAction((ctx) => ctx.container.get(IScriptService).create(new CreateScriptDto()))
        .configurable()
        .enabled(),

    new Shortcut("Close")
        .withCtrlKey()
        .withKey(KeyCode.KeyW)
        .hasAction((ctx) => {
            if (ctx.session.active) ctx.session.close(ctx.session.active.script.id);
        })
        .configurable()
        .enabled(),

    new Shortcut("Save")
        .withCtrlKey()
        .withKey(KeyCode.KeyS)
        .hasAction((ctx) => {
            if (ctx.session.active) ctx.container.get(IScriptService).save(ctx.session.active.script.id);
        })
        .enabled(),

    new Shortcut("Save All")
        .withCtrlKey()
        .withShiftKey()
        .withKey(KeyCode.KeyS)
        .hasAction(async (ctx) => {
            const scriptService = ctx.container.get(IScriptService);
            for (const environment of ctx.session.environments.filter(e => e.script.isDirty)) {
                await scriptService.save(environment.script.id);
            }
        })
        .enabled(),

    new Shortcut("Run")
        .withKey(KeyCode.F5)
        .firesEvent(RunScriptEvent)
        .configurable()
        .enabled(),

    new Shortcut("Script Properties")
        .withKey(KeyCode.F4)
        .hasAction((ctx) => {
            if (ctx.session.active) {
                ctx.container.get(IScriptService).openConfigWindow(ctx.session.active.script.id, null);
            }
        })
        .configurable()
        .enabled(),

    new Shortcut("Output")
        .withCtrlKey()
        .withKey(KeyCode.KeyR)
        .firesEvent(() => new TogglePaneEvent(OutputPane))
        .configurable()
        .enabled(),

    new Shortcut("Switch to Last Active Script")
        .withCtrlKey()
        .withKey(KeyCode.Tab)
        .hasAction((ctx) => ctx.session.activateLastActive())
        .configurable()
        .enabled(),

    new Shortcut("Settings")
        .withKey(KeyCode.F12)
        .hasAction((ctx) => ctx.container.get(ISettingService).openSettingsWindow(null))
        .configurable()
        .enabled(),

    new Shortcut("Explorer")
        .withAltKey()
        .withKey(KeyCode.KeyE)
        .firesEvent(() => new TogglePaneEvent(Explorer))
        .configurable()
        .enabled(),

    new Shortcut("Namespaces")
        .withAltKey()
        .withKey(KeyCode.KeyN)
        .firesEvent(() => new TogglePaneEvent(NamespacesPane))
        .configurable()
        .enabled(),

    new Shortcut("Reload")
        .withCtrlKey()
        .withShiftKey()
        .withKey(KeyCode.KeyR)
        .hasAction(() => window.location.reload())
        .configurable()
        .enabled(),
];

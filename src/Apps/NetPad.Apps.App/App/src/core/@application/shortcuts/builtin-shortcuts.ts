import {KeyCode} from "@common";
import {CreateScriptDto, IScriptService, ISettingsService} from "@application";
import {Shortcut} from "./shortcut";
import {ITextEditorService} from "../editor/itext-editor-service";

export enum ShortcutIds {
    openCommandPalette = "shortcut.commandpalette.open",
    quickOpenDocument = "shortcut.documents.quickopen",
    openLastActiveDocument = "shortcut.documents.switchtolastactive",
    newDocument = "shortcut.documents.new",
    closeDocument = "shortcut.documents.close",
    saveDocument = "shortcut.documents.save",
    saveAllDocuments = "shortcut.documents.saveall",
    runDocument = "shortcut.documents.run",
    openDocumentProperties = "shortcut.documents.properties",
    openSettings = "shortcut.settings.open",
    openOutput = "shortcut.output.open",
    openExplorer = "shortcut.explorer.open",
    openNamespaces = "shortcut.namespaces.open",
    reloadWindow = "shortcut.window.reload",
}

export const BuiltinShortcuts = [
    new Shortcut(ShortcutIds.openCommandPalette, "Command Palette")
        .withKey(KeyCode.F1)
        .hasAction(ctx => {
            const editor = ctx.container.get(ITextEditorService).active?.monaco;

            if (!editor) return;

            editor.focus();
            editor.trigger("", "editor.action.quickCommand", null);
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.quickOpenDocument, "Go to Script")
        .withCtrlKey()
        .withKey(KeyCode.KeyT)
        .hasAction(ctx => {
            const editor = ctx.container.get(ITextEditorService).active?.monaco;

            if (!editor) return;

            editor.focus();
            editor.trigger("", "netpad.action.goToScript", null);
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.openLastActiveDocument, "Switch to Last Active Script")
        .withCtrlKey()
        .withKey(KeyCode.Tab)
        .hasAction((ctx) => ctx.session.activateLastActive())
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.newDocument, "New")
        .withCtrlKey()
        .withKey(KeyCode.KeyN)
        .hasAction((ctx) => ctx.container.get(IScriptService).create(new CreateScriptDto()))
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.closeDocument, "Close")
        .withCtrlKey()
        .withKey(KeyCode.KeyW)
        .hasAction((ctx) => {
            if (ctx.session.active) ctx.session.close(ctx.session.active.script.id);
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.saveDocument, "Save")
        .withCtrlKey()
        .withKey(KeyCode.KeyS)
        .hasAction((ctx) => {
            if (ctx.session.active) ctx.container.get(IScriptService).save(ctx.session.active.script.id);
        })
        .captureDefaultKeyCombo()
        .enabled(),

    new Shortcut(ShortcutIds.saveAllDocuments, "Save All")
        .withCtrlKey()
        .withShiftKey()
        .withKey(KeyCode.KeyS)
        .hasAction(async (ctx) => {
            const scriptService = ctx.container.get(IScriptService);
            for (const environment of ctx.session.environments.filter(e => e.script.isDirty)) {
                await scriptService.save(environment.script.id);
            }
        })
        .captureDefaultKeyCombo()
        .enabled(),

    new Shortcut(ShortcutIds.runDocument, "Run")
        .withKey(KeyCode.F5)
        .firesEvent(async () => new (await import("@application/events/action-events")).RunScriptEvent())
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.openDocumentProperties, "Script Properties")
        .withKey(KeyCode.F4)
        .hasAction((ctx) => {
            if (ctx.session.active) {
                ctx.container.get(IScriptService).openConfigWindow(ctx.session.active.script.id, null);
            }
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.openSettings, "Settings")
        .withKey(KeyCode.F12)
        .hasAction((ctx) => ctx.container.get(ISettingsService).openSettingsWindow(null))
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.openOutput, "Output")
        .withCtrlKey()
        .withKey(KeyCode.KeyR)
        .firesEvent(async () => {
            const TogglePaneEvent = (await import("@application/events/action-events")).TogglePaneEvent;
            const OutputPane = (await import("../../../windows/main/panes")).OutputPane;

            return new TogglePaneEvent(OutputPane);
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.openExplorer, "Explorer")
        .withAltKey()
        .withKey(KeyCode.KeyE)
        .firesEvent(async () => {
            const TogglePaneEvent = (await import("@application/events/action-events")).TogglePaneEvent;
            const Explorer = (await import("../../../windows/main/panes")).Explorer;

            return new TogglePaneEvent(Explorer);
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.openNamespaces, "Namespaces")
        .withAltKey()
        .withKey(KeyCode.KeyN)
        .firesEvent(async () => {
            const TogglePaneEvent = (await import("@application/events/action-events")).TogglePaneEvent;
            const NamespacesPane = (await import("../../../windows/main/panes")).NamespacesPane;

            return new TogglePaneEvent(NamespacesPane);
        })
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),

    new Shortcut(ShortcutIds.reloadWindow, "Reload")
        .withCtrlKey()
        .withShiftKey()
        .withKey(KeyCode.KeyR)
        .hasAction(() => window.location.reload())
        .captureDefaultKeyCombo()
        .configurable()
        .enabled(),
];

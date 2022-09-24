import {IScriptService, ISettingService, RunScriptEvent} from "@domain";
import {KeyCode} from "@common";
import {Shortcut} from "./shortcut";
import {TogglePaneEvent} from "@application";
import {Explorer, NamespacesPane} from "../../../windows/main/panes";

export const BuiltinShortcuts = [
    new Shortcut("New")
        .withCtrlKey()
        .withKey(KeyCode.KeyN)
        .hasAction((ctx) => ctx.container.get(IScriptService).create())
        .configurable()
        .enabled(),

    new Shortcut("Close")
        .withCtrlKey()
        .withKey(KeyCode.KeyW)
        .hasAction((ctx) => ctx.session.close(ctx.session.active.script.id))
        .configurable()
        .enabled(),

    new Shortcut("Save")
        .withCtrlKey()
        .withKey(KeyCode.KeyS)
        .hasAction((ctx) => ctx.container.get(IScriptService).save(ctx.session.active.script.id))
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
        .hasAction((ctx) => ctx.container.get(IScriptService).openConfigWindow(ctx.session.active.script.id, null))
        .configurable()
        .enabled(),

    new Shortcut("Settings")
        .withKey(KeyCode.F12)
        .hasAction((ctx) => ctx.container.get(ISettingService).openSettingsWindow(null))
        .configurable()
        .enabled(),

    new Shortcut("Switch to Last Active Script")
        .withCtrlKey()
        .withKey(KeyCode.Tab)
        .hasAction((ctx) => ctx.session.activateLastActive())
        .configurable()
        .enabled(),

    new Shortcut("Explorer")
        .withAltKey()
        .withKey(KeyCode.KeyE)
        .firesEvent(() => new TogglePaneEvent(Explorer))
        .configurable()
        .enabled(),

    new Shortcut("Namespaces Pane")
        .withAltKey()
        .withKey(KeyCode.KeyN)
        .firesEvent(() => new TogglePaneEvent(NamespacesPane))
        .configurable()
        .enabled(),
];

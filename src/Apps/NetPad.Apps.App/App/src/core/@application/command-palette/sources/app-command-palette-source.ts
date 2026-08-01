import {IContainer} from "aurelia";
import {CommandContext} from "@application/commands/command";
import {ICommandRegistry} from "@application/commands/icommand-registry";
import {IKeybindingManager} from "@application/keybindings/ikeybinding-manager";
import {MonacoEditorUtil} from "@application/editor/monaco/monaco-editor-util";
import {KeybindingCaps} from "@application/keybindings/keybinding-caps";
import {IPaletteSource} from "../ipalette-source";
import {PaletteMode} from "../palette-grammar";
import {PaletteGroup} from "../palette-item";

/**
 * The commands a window can run, from the command registry.
 */
export class AppCommandPaletteSource implements IPaletteSource {
    public readonly mode = PaletteMode.Commands;
    public readonly order = 0;

    constructor(
        @ICommandRegistry private readonly commandRegistry: ICommandRegistry,
        @IKeybindingManager private readonly keybindingManager: IKeybindingManager,
        @IContainer private readonly container: IContainer) {
    }

    public getGroups(): PaletteGroup[] {
        const context = new CommandContext(this.container);

        const items = this.commandRegistry.commands
            .filter(command => command.isEnabled(context))
            .map(command => ({
                id: command.id,
                title: command.title,
                icon: command.icon,
                keys: this.keysFor(command.id, command.monacoCommandId),
                run: () => this.commandRegistry.execute(command.id),
            }));

        return items.length ? [{label: "Application", items}] : [];
    }

    /**
     * The keys that run the command. A command NetPad has not bound but the editor has is
     * reached by the editor's own key binding.
     */
    private keysFor(commandId: string, monacoCommandId?: string): KeybindingCaps | undefined {
        const keyCombo = this.keybindingManager.getKeybinding(commandId)?.keyCombo;
        if (keyCombo?.isBound) return keyCombo.asCaps();

        if (!monacoCommandId) return undefined;

        return MonacoEditorUtil.getKeybindingCaps(monacoCommandId);
    }
}

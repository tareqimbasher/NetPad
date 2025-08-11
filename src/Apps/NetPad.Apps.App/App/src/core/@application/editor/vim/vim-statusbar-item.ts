import {ILogger, resolve} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import * as monaco from "monaco-editor";
import {EditorVimMode, initVimMode, VimMode} from "monaco-vim";
import {MonacoVimStatusbarOverride} from "./monaco-vim-statusbar-override";
import {CreateScriptDto, IScriptService, ISession, IShortcutManager, Settings, ShortcutIds} from "@application";

export class VimStatusbarItem {
    private readonly element = resolve(HTMLElement);
    private readonly scriptService = resolve(IScriptService);
    private readonly session = resolve(ISession);
    private readonly settings = resolve(Settings);
    private readonly shortcutManager = resolve(IShortcutManager);
    private readonly logger = resolve(ILogger).scopeTo(nameof(VimStatusbarItem));

    private editorVimModes = new Map<monaco.editor.ICodeEditor, EditorVimMode | undefined>();

    public get tooltipText(): string {
        const shortcut = this.shortcutManager.getShortcut(ShortcutIds.vimModeToggle);
        const keyCombo = shortcut?.keyCombo.asString;

        return `Vim mode (${keyCombo})\n\nBasic commands:\n`
            + ":n[ew] = new script\n"
            + ":w[rite] = save\n"
            + ":wq = save and close script\n"
            + ":q[uit] = close script and prompt for unsaved changes\n"
            + ":qq = close script and throw away unsaved changes\n"
            + ":ren[ame] = rename script";
    }

    public attached() {
        this.defineCustomVimCommands();
        monaco.editor.onDidCreateEditor(editor => this.onEditorCreated(editor));
    }

    private defineCustomVimCommands() {
        VimMode.Vim.defineEx("new", "n", () => this.scriptService.create(new CreateScriptDto()));
        VimMode.Vim.defineEx("write", "w", () => this.session.active && this.scriptService.save(this.session.active.script.id));
        VimMode.Vim.defineEx("wq", null, async () => {
            const script = this.session.active?.script;
            if (!script) {
                return;
            }
            const saved = await this.scriptService.save(this.session.active.script.id);
            if (saved) {
                this.session.close(this.session.active.script.id, false);
            }
        });
        VimMode.Vim.defineEx("quit", "q", () => {
            if (this.session.active) {
                this.session.close(this.session.active.script.id, false);
            }
        });
        VimMode.Vim.defineEx("qq", null, () => {
            if (this.session.active) {
                this.session.close(this.session.active.script.id, true);
            }
        });
        VimMode.Vim.defineEx("rename", "ren", () => this.session.active && this.scriptService.openRenamePrompt(this.session.active.script));
    }

    private onEditorCreated(editor: monaco.editor.ICodeEditor) {
        this.editorVimModes.set(editor, undefined);

        editor.onDidDispose(() => {
            this.disableVimOnEditor(editor);
            this.editorVimModes.delete(editor);
        });

        this.checkVimModeStatus();
    }

    @watch((vm: VimStatusbarItem) => vm.settings.editor.vim.enabled)
    private checkVimModeStatus() {
        if (this.settings.editor.vim.enabled) {
            for (const editor of this.editorVimModes.keys()) {
                this.enableVimOnEditor(editor);
            }
        } else {
            for (const editor of this.editorVimModes.keys()) {
                this.disableVimOnEditor(editor);
            }
        }
    }

    private enableVimOnEditor(editor: monaco.editor.ICodeEditor) {
        let vimMode = this.editorVimModes.get(editor);
        if (vimMode) {
            this.logger.warn(`Vim mode is already enabled on editor with ID: ${editor.getId()}`);
            return;
        }

        vimMode = initVimMode(editor, this.element, MonacoVimStatusbarOverride);
        this.editorVimModes.set(editor, vimMode);
    }

    private disableVimOnEditor(editor: monaco.editor.ICodeEditor) {
        const vimMode = this.editorVimModes.get(editor);
        if (vimMode) {
            vimMode.dispose();
            this.editorVimModes.set(editor, undefined);
        }
    }
}

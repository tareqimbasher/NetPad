import * as monaco from "monaco-editor";
/* eslint-disable @typescript-eslint/ban-ts-comment */
// @ts-ignore
import {IQuickInputService} from "monaco-editor/esm/vs/platform/quickinput/common/quickInput";
// @ts-ignore
import {IKeybindingService} from "monaco-editor/esm/vs/platform/keybinding/common/keybinding";
// @ts-ignore
import {StandaloneServices} from "monaco-editor/esm/vs/editor/standalone/browser/standaloneServices";
/* eslint-enable @typescript-eslint/ban-ts-comment */
import {IDisposable} from "@common";
import {Settings} from "@application";
import {AppTheme} from "@application/themes/app-theme";
import {KeybindingCaps} from "@application/keybindings/keybinding-caps";
import {MonacoThemeManager} from "./monaco-theme-manager";
import {parseMonacoKeybindingLabel} from "./monaco-keybinding-label";

export class MonacoEditorUtil {
    public static constructModelUri(scriptId: string): monaco.Uri {
        return monaco.Uri.from({
            scheme: "inmemory",     // This is what monaco sets 'scheme' when uri is auto-generated
            authority: "model",     // This is what monaco sets 'authority' when uri is auto-generated
            path: `/${scriptId}`    // Must start with a '/'
        });
    }

    public static getScriptId(textModel: monaco.editor.ITextModel): string {
        return textModel.uri.path.substring(1);
    }

    public static getQuickInputService(): IQuickInputService {
        return StandaloneServices.get(IQuickInputService);
    }

    public static getKeybindingService(): IKeybindingService {
        return StandaloneServices.get(IKeybindingService);
    }

    /**
     * The key combination that currently reaches an editor action, as strokes of key caps. Returns
     * undefined when nothing reaches it, including when the app has taken that combination over,
     * or while the editor environment is still being set up.
     */
    public static getKeybindingCaps(commandId: string): KeybindingCaps | undefined {
        try {
            const keybindingService = this.getKeybindingService();
            const label = keybindingService.lookupKeybinding(commandId)?.getLabel();
            if (label) return parseMonacoKeybindingLabel(label);

            // Actions contributed to an editor are keybound under an id scoped to that editor
            // instance, so the action's own id finds nothing. Every editor gets the same actions.
            for (const editor of monaco.editor.getEditors()) {
                const scoped = keybindingService.lookupKeybinding(`${editor.getId()}:${commandId}`)?.getLabel();
                if (scoped) return parseMonacoKeybindingLabel(scoped);
            }

            return undefined;
        } catch {
            return undefined;
        }
    }

    /**
     * Registers a callback invoked whenever the editor's effective keybindings change and returns
     * an {@link IDisposable} that can be used to unsubscribe.
     */
    public static onKeybindingsChanged(callback: () => void): IDisposable {
        try {
            return this.getKeybindingService().onDidUpdateKeybindings(callback);
        } catch {
            return {dispose: () => undefined};
        }
    }

    public static async updateOptions(editor: monaco.editor.IStandaloneCodeEditor, settings: Settings) {
        const monacoOptions = JSON.parse(JSON.stringify(settings.editor.monacoOptions)) as monaco.editor.IStandaloneEditorConstructionOptions & Record<string, unknown>;
        let theme = monacoOptions.theme;

        if (!theme) {
            theme = AppTheme.resolveGround(settings.appearance.mode) === "light"
                ? "netpad-light-theme"
                : "netpad-dark-theme";
            monacoOptions.theme = theme;
        }

        // Default options (overridable by user customizations)
        monacoOptions.cursorBlinking ??= "smooth";
        monacoOptions.lineNumbers ??= "on";
        monacoOptions.wordWrap ??= "off";
        monacoOptions.mouseWheelZoom ??= true;
        monacoOptions.renderLineHighlight ??= "all";
        monacoOptions.minimap ??= {
            enabled: true,
        }

        editor.updateOptions(monacoOptions);

        await MonacoThemeManager.setTheme(editor, monacoOptions.theme ?? "", monacoOptions["themeCustomizations"]!);
    }

    /**
     * Creates an abort signal from a Cancellation Token and a timeout.
     * @param timeout Signal will be aborted after specified timeout (in milliseconds) even if token is not cancelled.
     * @param token A cancellation token that when cancelled will abort the signal. If none provided, the signal will be aborted based off timeout alone.
     */
    public static abortSignalFrom(timeout: number, token?: monaco.CancellationToken): AbortSignal {
        const abortController = new AbortController();

        if (timeout === undefined || timeout === null || timeout < 0) {
            timeout = 10000;
        }

        if (token) {
            const cts = new monaco.CancellationTokenSource(token);
            token = cts.token;

            const timeoutHandle = setTimeout(() => cts.cancel(), timeout);

            token.onCancellationRequested(() => {
                clearTimeout(timeoutHandle);
                abortController.abort();
            });
        } else {
            setTimeout(() => abortController.abort(), timeout);
        }

        return abortController.signal;
    }
}

import * as monaco from "monaco-editor";
/* eslint-disable @typescript-eslint/ban-ts-comment */
// @ts-ignore
import {IQuickInputService} from "monaco-editor/esm/vs/platform/quickinput/common/quickInput";
// @ts-ignore
import {IKeybindingService} from "monaco-editor/esm/vs/platform/keybinding/common/keybinding";
// @ts-ignore
import {StandaloneServices} from "monaco-editor/esm/vs/editor/standalone/browser/standaloneServices";
/* eslint-enable @typescript-eslint/ban-ts-comment */
import {Settings} from "@application";
import {MonacoThemeManager} from "./monaco-theme-manager";

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

    public static async updateOptions(editor: monaco.editor.IStandaloneCodeEditor, settings: Settings) {
        const monacoOptions = JSON.parse(JSON.stringify(settings.editor.monacoOptions));
        let theme = monacoOptions.theme;

        if (!theme) {
            theme = settings.appearance.theme === "Light" ? "netpad-light-theme" : "netpad-dark-theme";
            monacoOptions.theme = theme;
        }

        editor.updateOptions(monacoOptions);

        await MonacoThemeManager.setTheme(editor, monacoOptions.theme, monacoOptions["themeCustomizations"]);
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

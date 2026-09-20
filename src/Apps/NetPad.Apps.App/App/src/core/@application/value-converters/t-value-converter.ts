import {ITranslationService, LANGUAGE_CHANGED_SIGNAL} from "@application/i18n/itranslation-service";

/**
 * Translation value converter. Used in templates as `key | t`, e.g. `${'menu.file.new' | t}` or
 * `title.bind="'titlebar.close' | t"`.
 *
 * Declares its dependency on LANGUAGE_CHANGED_SIGNAL via `signals`: after the language changes,
 * TranslationService fires that signal through ISignaler and Aurelia re-evaluates all `| t`
 * bindings in this window, refreshing the UI without a manual page reload.
 *
 * Cross-window note: on desktop, every OS window is a separate renderer process with its own
 * ISignaler singleton; a signal dispatched in one window never reaches the others. That is why
 * TranslationService re-dispatches the signal in each window when it receives the
 * SettingsUpdatedEvent broadcast by the backend, so every window's `| t` bindings refresh.
 */
export class TValueConverter {
    // Must be an instance field, not static: when a binding is attached (astBind in the runtime)
    // the framework resolves the converter *instance* from the DI container. Instances cannot see
    // static properties, so a static `signals` would read as undefined, the signal listener would
    // never be attached, and dispatchSignal would do nothing for all `| t` bindings.
    public readonly signals = [LANGUAGE_CHANGED_SIGNAL];

    constructor(@ITranslationService private readonly translation: ITranslationService) {
    }

    public toView(key?: string, params?: Record<string, string | number>): string {
        if (!key) {
            return "";
        }

        return this.translation.t(key, params);
    }
}

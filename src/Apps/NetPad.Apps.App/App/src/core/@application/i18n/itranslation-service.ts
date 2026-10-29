import {DI, IDisposable} from "aurelia";

/**
 * The name of the language-changed signal. After the language changes, TranslationService fires
 * this signal through ISignaler, causing all bindings that use TValueConverter (i.e. `| t` in
 * templates) to be re-evaluated and the UI to refresh without a page reload.
 *
 * Note: on desktop, every OS window is a separate renderer process with its own ISignaler
 * singleton, so cross-window synchronization cannot rely on a single global signal. Instead,
 * TranslationService listens for the SettingsUpdatedEvent broadcast by the backend and re-dispatches
 * the signal in each window.
 */
export const LANGUAGE_CHANGED_SIGNAL = "language-changed";

/**
 * A supported language.
 */
export interface LanguageInfo {
    /** The language code, e.g. "en", "zh-CN", "ja" */
    code: string;
    /** The native name of the language, used in the language selector, e.g. "English" */
    displayName: string;
}

/**
 * Provides app-wide translation: t(), language switching and change subscriptions.
 */
export interface ITranslationService {
    /** The current language code */
    readonly currentLanguage: string;

    /** All available languages */
    readonly availableLanguages: LanguageInfo[];

    /**
     * Translates the given key.
     * @param key A dot-separated key, e.g. "menu.file.new"
     * @param params Optional interpolation parameters, referenced as {name} in the value
     * @returns The translated text. If the key is missing in both the current and fallback
     * language, the key itself is returned (which makes missing translations easy to spot).
     */
    t(key: string, params?: Record<string, string | number>): string;

    /**
     * Switches the language of the current window only, immediately firing the refresh signal
     * and change callbacks. Does not persist.
     * Persistence is the caller's responsibility (e.g. saving settings); other windows sync by
     * listening to the SettingsUpdatedEvent broadcast by the backend. No-op if the language is unchanged.
     */
    applyLanguage(language: string): void;

    /**
     * Initializes the current language only (no persistence, no signal), used at app startup to
     * set the language from saved settings.
     */
    initialize(language?: string): void;

    /**
     * Subscribes to language changes. Returns a handle to unsubscribe.
     */
    onLanguageChanged(callback: (language: string) => void): IDisposable;

    /**
     * Subscribes to language change events broadcast by the backend, used for cross-window
     * synchronization on desktop.
     * Must be called after the IPC gateway (SignalRIpcGateway) has started, otherwise
     * this.connection does not exist yet and subscribing throws. That is why this is not done in
     * the constructor; main.ts calls it after app.start().
     */
    subscribeToServerEvents(): void;
}

export const ITranslationService = DI.createInterface<ITranslationService>();

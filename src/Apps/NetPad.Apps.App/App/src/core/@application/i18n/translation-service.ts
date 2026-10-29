import {IDisposable} from "@aurelia/kernel";
// ISignaler must be imported from the "aurelia" main package: in dev mode webpack aliases each
// @aurelia/* package to its own separately-bundled index.dev.mjs, each with its own token objects;
// the framework registers the Signaler under the token aggregated in the "aurelia" main package.
// Importing from "@aurelia/runtime" would yield the unregistered token and container construction
// throws AUR0012. All framework tokens in this project (ILogger/IContainer/DI etc.) follow the
// convention of importing from "aurelia".
import {ISignaler} from "aurelia";
import {IEventBus, SettingsUpdatedEvent} from "@application";
import en from "./en.json";
import zhCN from "./zh-CN.json";
import ja from "./ja.json";
import {ITranslationService, LanguageInfo, LANGUAGE_CHANGED_SIGNAL} from "./itranslation-service";

const FALLBACK_LANGUAGE = "en";

// All language resources are imported statically and bundled by webpack, avoiding the
// compatibility risks of runtime lazy-loading. To add a language, register it here and
// append it to availableLanguages.
const RESOURCES: Record<string, Record<string, string>> = {
    "en": en,
    "zh-CN": zhCN,
    "ja": ja,
};

export class TranslationService implements ITranslationService {
    public currentLanguage: string = FALLBACK_LANGUAGE;
    private readonly _onLanguageChangedCallbacks = new Set<(language: string) => void>();
    private _eventSubscription: IDisposable | null = null;
    private _serverEventsSubscribed = false;

    public readonly availableLanguages: LanguageInfo[] = [
        {code: "en", displayName: "English"},
        {code: "zh-CN", displayName: "中文"},
        {code: "ja", displayName: "日本語"},
    ];

    constructor(
        @IEventBus private readonly eventBus: IEventBus,
        @ISignaler private readonly signaler: ISignaler
    ) {
        // Note: do not subscribe to SettingsUpdatedEvent in the constructor. At that point
        // SignalRIpcGateway has not started yet, this.connection is undefined, and subscribing
        // throws "Cannot read properties of undefined (reading 'on')". Subscription is done by
        // subscribeToServerEvents() after app.start(), once the gateway is connected.
    }

    // Subscribes to language change events broadcast by the backend, used for cross-window
    // synchronization on desktop. Must be called after app.start() (SignalRIpcGateway started,
    // this.connection created), otherwise subscribing throws
    // "Cannot read properties of undefined (reading 'on')". Called by main.ts after startup;
    // guarded so repeated calls are safe.
    public subscribeToServerEvents(): void {
        if (this._serverEventsSubscribed) {
            return;
        }
        this._serverEventsSubscribed = true;

        this._eventSubscription = this.eventBus.subscribeToServer(SettingsUpdatedEvent, msg => {
            const lang = msg?.settings?.language;
            if (lang) {
                this.applyLanguage(lang);
            }
        });
    }

    public t(key: string, params?: Record<string, string | number>): string {
        const current = RESOURCES[this.currentLanguage];
        let value = (current && current[key]) ?? RESOURCES[FALLBACK_LANGUAGE][key] ?? key;

        if (params) {
            for (const [name, val] of Object.entries(params)) {
                value = value.replace(new RegExp(`\\{${name}\\}`, "g"), String(val));
            }
        }

        return value;
    }

    public initialize(language?: string): void {
        if (language && RESOURCES[language]) {
            this.currentLanguage = language;
        }
    }

    // Switches only the current language and fires the refresh signal and callbacks, without
    // persisting. Shared by in-window switching and cross-window event handling to avoid
    // duplicate writes and event loops. No-op when the language is unchanged, so unrelated
    // settings saves do not trigger side effects like menu rebuilds.
    public applyLanguage(language: string): void {
        if (!RESOURCES[language]) {
            language = FALLBACK_LANGUAGE;
        }

        if (this.currentLanguage === language) {
            return;
        }

        this.currentLanguage = language;

        // Refresh this window immediately: fire the ISignaler signal so all `| t` bindings re-evaluate.
        this.signaler.dispatchSignal(LANGUAGE_CHANGED_SIGNAL);

        for (const callback of this._onLanguageChangedCallbacks) {
            try {
                callback(language);
            } catch (err) {
                console.error("A language-changed callback threw:", err);
            }
        }
    }

    public onLanguageChanged(callback: (language: string) => void): IDisposable {
        this._onLanguageChangedCallbacks.add(callback);
        return {
            dispose: () => {
                this._onLanguageChangedCallbacks.delete(callback);
            }
        };
    }
}

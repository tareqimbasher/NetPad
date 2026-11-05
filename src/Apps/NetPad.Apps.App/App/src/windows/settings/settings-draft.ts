import {Settings} from "@application/api";
import {mergeSavedSettings} from "./settings-changes";

/**
 * The settings the user is editing, and the saved settings they were edited from.
 */
export class SettingsDraft {
    public readonly edited: Settings;

    /** The saved settings the edits started from. A value still equal to its base is one the user hasn't changed. */
    private base: Settings;
    private baseBeforeSave?: Settings;

    constructor(saved: Settings) {
        this.edited = SettingsDraft.copy(saved);
        this.base = SettingsDraft.copy(saved);
    }

    /**
     * Returns the settings to send for a save. Until the save fails, the edits it sends count as saved, so the
     * values the backend stores in their place (a default, or a capped number) replace them when they arrive.
     */
    public beginSave(): Settings {
        this.baseBeforeSave = this.base;
        // Set before the request goes out: the saved settings can arrive before the request returns.
        this.base = SettingsDraft.copy(this.edited);

        const toSave = SettingsDraft.copy(this.edited);
        toSave.results.maxSerializationDepth = serializationLimit(toSave.results.maxSerializationDepth);
        toSave.results.maxCollectionSerializeLength = serializationLimit(toSave.results.maxCollectionSerializeLength);
        return toSave;
    }

    /** Puts back the base the failed save replaced, unless settings saved since have replaced it already. */
    public saveFailed(): void {
        if (this.baseBeforeSave) {
            this.base = this.baseBeforeSave;
        }

        this.baseBeforeSave = undefined;
    }

    /** Takes in settings saved here or elsewhere, keeping the values the user has changed since. */
    public syncSaved(saved: Settings): void {
        mergeSavedSettings(
            this.base,
            this.edited as unknown as Record<string, unknown>,
            SettingsDraft.copy(saved) as unknown as Record<string, unknown>);

        this.base = SettingsDraft.copy(saved);
        this.baseBeforeSave = undefined;
    }

    private static copy(settings: Settings): Settings {
        return Settings.fromJS(JSON.parse(JSON.stringify(settings)));
    }
}

/**
 * A serialization limit as the backend reads it: a positive whole number as it is, anything else (an empty
 * field among them) as 0, which the backend stores as the default.
 */
export function serializationLimit(value: unknown): number {
    const number = Number(value);
    return Number.isInteger(number) && number > 0 ? number : 0;
}

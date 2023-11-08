import {DI} from "aurelia";
import {Shortcut} from "./shortcut";

export interface IShortcutManager {
    /**
     * Initializes the ShortcutManager and starts listening for keyboard events.
     */
    initialize(): void;

    /**
     * Adds a shortcut to the shortcut registry.
     * @param shortcut The shortcut to register.
     */
    registerShortcut(shortcut: Shortcut): void;

    /**
     * Removes a shortcut from the shortcut registry.
     * @param shortcut The shortcut to unregister.
     */
    unregisterShortcut(shortcut: Shortcut): void;

    /**
     * Finds a shortcut by its ID, if one exists.
     * @param id The id of the shortcut to get.
     */
    getShortcut(id: string): Shortcut | undefined;

    /**
     * Finds a shortcut by its name, if one exists.
     * @param name The name of the shortcut to get.
     */
    getShortcutByName(name: string): Shortcut | undefined;

    /**
     * Executes a shortcut.
     * @param shortcut
     */
    executeShortcut(shortcut: Shortcut): Promise<void>;
}

export const IShortcutManager = DI.createInterface<IShortcutManager>();

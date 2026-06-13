import {DI} from "aurelia";
import {IDisposable} from "@common";
import {IMenuItem} from "./imenu-item";

export interface IMainMenuService {
    items: ReadonlyArray<IMenuItem>;

    /**
     * Resolves once dynamic items have been populated for the first time.
     * Consumers that snapshot the menu structure (e.g. the OS-native menu bar bootstrappers) should
     * await this before reading {@link items}, otherwise dynamic entries will be missing on the
     * very first render and only appear after the next change event.
     */
    readonly initialized: Promise<void>;

    clickMenuItem(item: IMenuItem | string): Promise<void>;

    /**
     * Registers a callback invoked whenever the menu structure backing {@link items} changes
     * (recent-scripts list updates, enabled/disabled state toggles, etc.). Consumers that snapshot
     * the menu should rebuild from {@link items} when this fires. This is the single notification
     * for every change path, so callers don't need to know *why* the menu changed. Dispose the
     * returned handle to unsubscribe.
     */
    onChanged(callback: () => void): IDisposable;
}

export const IMainMenuService = DI.createInterface<IMainMenuService>();

import {DI} from "aurelia";

export const ISystemService = DI.createInterface<ISystemService>();

/**
 * Provides access to capabilities of the system the app is running on that are implemented
 * differently by each shell.
 */
export interface ISystemService {
    /**
     * Opens a URL in the system-configured default browser.
     * @param url The URL to open.
     */
    openUrlInBrowser(url: string): void;
}

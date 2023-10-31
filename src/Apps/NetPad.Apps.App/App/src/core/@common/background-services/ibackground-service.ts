import {DI} from "aurelia";

export const IBackgroundService = DI.createInterface<IBackgroundService>();

/**
 * A background service that runs when the app starts, and stops when the app stops.
 */
export interface IBackgroundService {
    start(): Promise<void>;

    stop(): void;
}

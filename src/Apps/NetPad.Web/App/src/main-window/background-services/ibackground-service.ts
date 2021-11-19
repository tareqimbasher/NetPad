import {DI} from "aurelia";

export const IBackgroundService = DI.createInterface<IBackgroundService>();

export interface IBackgroundService {
    start(): Promise<void>;
    stop(): void;
}

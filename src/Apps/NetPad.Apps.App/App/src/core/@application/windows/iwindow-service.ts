import {DI} from "aurelia";
import {IWindowApiClient} from "@application";
import {WindowState} from "./window-state";

export interface IWindowService extends IWindowApiClient {
    getState(): Promise<WindowState>;

    maximize(): Promise<void>;

    minimize(): Promise<void>;

    toggleFullScreen(): Promise<void>;

    toggleAlwaysOnTop(): Promise<void>;

    toggleDeveloperTools(): Promise<void>;
}

export const IWindowService = DI.createInterface<IWindowService>();

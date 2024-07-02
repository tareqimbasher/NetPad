import {IWindowService, WindowApiClient, WindowState} from "@application";
import {PlatformNotSupportedError} from "@common";

export class BrowserWindowService extends WindowApiClient implements IWindowService {
    public getState(): Promise<WindowState> {
        throw new PlatformNotSupportedError();
    }

    public maximize(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public minimize(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public toggleDeveloperTools(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public toggleAlwaysOnTop(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public toggleFullScreen(): Promise<void> {
        throw new PlatformNotSupportedError();
    }
}

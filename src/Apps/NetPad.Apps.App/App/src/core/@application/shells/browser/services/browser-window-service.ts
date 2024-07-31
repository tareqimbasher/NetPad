import {IWindowService, WindowApiClient, WindowState} from "@application";
import {PlatformNotSupportedError} from "@common";

export class BrowserWindowService extends WindowApiClient implements IWindowService {
    public getState(): Promise<WindowState> {
        throw new PlatformNotSupportedError();
    }

    public close(): Promise<void> {
        window.close();
        return Promise.resolve();
    }

    public maximize(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public minimize(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public zoomIn(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public zoomOut(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public resetZoom(): Promise<void> {
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

import {IWindowService, WindowApiClient, WindowState, WindowViewStatus} from "@application";
import {PlatformNotSupportedError} from "@common";
import {resolve} from "aurelia";
import {ITranslationService} from "@application/i18n/itranslation-service";

export class BrowserWindowService extends WindowApiClient implements IWindowService {
    private static zoomFactor = 1;
    private readonly translation: ITranslationService = resolve(ITranslationService);

    public getState(): Promise<WindowState> {
        return Promise.resolve(new WindowState(WindowViewStatus.Unknown, false));
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
        this.setZoomFactor(BrowserWindowService.zoomFactor + 0.1);
        return Promise.resolve(undefined);
    }

    public zoomOut(): Promise<void> {
        this.setZoomFactor(BrowserWindowService.zoomFactor - 0.1);
        return Promise.resolve(undefined);
    }

    public resetZoom(): Promise<void> {
        this.setZoomFactor(1);
        return Promise.resolve(undefined);
    }

    public toggleDeveloperTools(): Promise<void> {
        alert(this.translation.t("ui.use-browser-devtools-shortcut"));
        return Promise.resolve();
    }

    public toggleAlwaysOnTop(): Promise<void> {
        throw new PlatformNotSupportedError();
    }

    public async toggleFullScreen(): Promise<void> {
        if (document.documentElement.requestFullscreen) {
            await document.documentElement.requestFullscreen();
        }
    }

    private setZoomFactor(zoomFactor: number) {
        document.body.style.zoom = zoomFactor.toString();
        BrowserWindowService.zoomFactor = zoomFactor;
    }
}

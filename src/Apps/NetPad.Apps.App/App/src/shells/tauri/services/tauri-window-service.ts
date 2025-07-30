import {IWindowService, WindowApiClient, WindowState, WindowViewStatus} from "@application";
import {Window} from "@tauri-apps/api/window"
import {invoke} from "@tauri-apps/api/core"

export class TauriWindowService extends WindowApiClient implements IWindowService {
    private static isAlwaysOnTop = false;
    private static zoomFactor = 1;

    public async getState(): Promise<WindowState> {
        const window = Window.getCurrent();

        let viewStatus: WindowViewStatus;

        if (await window.isMaximized()) {
            viewStatus = WindowViewStatus.Maximized;
        }
        else if (await window.isMinimized()) {
            viewStatus = WindowViewStatus.Minimized;
        }
        else {
            viewStatus = WindowViewStatus.UnMaximized;
        }

        return new WindowState(viewStatus, TauriWindowService.isAlwaysOnTop);
    }

    public close(): Promise<void> {
        return Window.getCurrent().close();
    }

    public async maximize(): Promise<void> {
        const window = Window.getCurrent();

        if (await window.isMaximized()) {
            return window.unmaximize();
        }

        return window.maximize();
    }

    public async minimize(): Promise<void> {
        const window = Window.getCurrent();

        if (await window.isMinimized()) {
            return window.unminimize();
        }

        return window.minimize();
    }

    public zoomIn(): Promise<void> {
        this.setZoomFactor(TauriWindowService.zoomFactor + 0.1);
        return Promise.resolve(undefined);
    }

    public zoomOut(): Promise<void> {
        this.setZoomFactor(TauriWindowService.zoomFactor - 0.1);
        return Promise.resolve(undefined);
    }

    public resetZoom(): Promise<void> {
        this.setZoomFactor(1);
        return Promise.resolve(undefined);
    }

    private setZoomFactor(zoomFactor: number) {
        document.body.style.zoom = zoomFactor;
        TauriWindowService.zoomFactor = zoomFactor;
    }

    public toggleDeveloperTools(): Promise<void> {
        return invoke("toggle_devtools");
    }

    public toggleAlwaysOnTop(): Promise<void> {
        TauriWindowService.isAlwaysOnTop = !TauriWindowService.isAlwaysOnTop;
        return Window.getCurrent().setAlwaysOnTop(TauriWindowService.isAlwaysOnTop);
    }

    public async toggleFullScreen(): Promise<void> {
        const window = Window.getCurrent();
        return window.setFullscreen(!(await window.isFullscreen()));
    }
}

declare global {
    export interface CSSStyleDeclaration {
        // 'zoom' is newly standardized in Firefox and is already supported in Chrome/Webkit, not yet added to TypeScript
        // https://developer.mozilla.org/en-US/docs/Web/CSS/zoom
        zoom: number;
    }
}

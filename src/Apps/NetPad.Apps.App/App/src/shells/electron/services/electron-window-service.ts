import {ipcRenderer} from "electron";
import {IHttpClient} from "@aurelia/fetch-client";
import {ChannelInfo, IWindowService, IWindowState, WindowApiClient, WindowState, WindowViewStatus} from "@application";
import {electronConstants} from "../electron-shared";
import {ElectronIpcGateway} from "./electron-ipc-gateway";

/**
 * IWindowService implementation that sends window control events to Electron's main process.
 */
export class ElectronWindowService extends WindowApiClient implements IWindowService {
    constructor(private readonly electronIpcGateway: ElectronIpcGateway, baseUrl?: string, @IHttpClient http?: IHttpClient) {
        super(baseUrl, http);
    }

    public async getState(): Promise<WindowState> {
        const state = await ipcRenderer.invoke(electronConstants.ipcEventNames.getWindowState) as IWindowState;

        if (!state) {
            return new WindowState(WindowViewStatus.Unknown, false);
        }

        return new WindowState(state.viewStatus, state.isAlwaysOnTop);
    }

    public close(): Promise<void> {
        window.close();
        return Promise.resolve();
    }

    maximize(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.maximize));
    }

    minimize(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.minimize));
    }

    zoomIn(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.zoomIn));
    }

    zoomOut(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.zoomOut));
    }

    resetZoom(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.resetZoom));
    }

    toggleDeveloperTools(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.toggleDeveloperTools));
    }

    toggleAlwaysOnTop(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.toggleAlwaysOnTop));
    }

    toggleFullScreen(): Promise<void> {
        return this.electronIpcGateway.send(new ChannelInfo(electronConstants.ipcEventNames.toggleFullScreen));
    }
}

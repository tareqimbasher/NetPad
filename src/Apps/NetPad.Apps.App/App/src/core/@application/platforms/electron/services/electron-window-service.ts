import {ipcRenderer} from "electron";
import {IWindowService, WindowApiClient, WindowState} from "@application";
import {ElectronIpcEventNames} from "../electron-ipc-event-names";

export class ElectronWindowService extends WindowApiClient implements IWindowService {
    public async getState(): Promise<WindowState> {
        const state = await ipcRenderer.invoke(ElectronIpcEventNames.getWindowState) as WindowState;
        return new WindowState(state.viewStatus, state.isAlwaysOnTop);
    }

    maximize(): Promise<void> {
        return ipcRenderer.invoke(ElectronIpcEventNames.maximize);
    }

    minimize(): Promise<void> {
        return ipcRenderer.invoke(ElectronIpcEventNames.minimize);
    }

    toggleDeveloperTools(): Promise<void> {
        return ipcRenderer.invoke(ElectronIpcEventNames.toggleDeveloperTools);
    }

    toggleAlwaysOnTop(): Promise<void> {
        return ipcRenderer.invoke(ElectronIpcEventNames.toggleAlwaysOnTop);
    }

    toggleFullScreen(): Promise<void> {
        return ipcRenderer.invoke(ElectronIpcEventNames.toggleFullScreen);
    }
}

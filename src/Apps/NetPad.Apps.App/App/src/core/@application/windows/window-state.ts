export enum WindowViewStatus {
    "Unknown" = "Unknown",
    "Minimized" = "Minimized",
    "UnMaximized" = "UnMaximized",
    "Maximized" = "Maximized",
}

export interface IWindowState {
    viewStatus: WindowViewStatus;
    isAlwaysOnTop: boolean;
}

export class WindowState implements IWindowState {
    constructor(public viewStatus: WindowViewStatus, public isAlwaysOnTop: boolean) {
    }

    public get isMinimized(): boolean {
        return this.viewStatus === WindowViewStatus.Minimized;
    }

    public get isMaximized(): boolean {
        return this.viewStatus === WindowViewStatus.Maximized;
    }
}

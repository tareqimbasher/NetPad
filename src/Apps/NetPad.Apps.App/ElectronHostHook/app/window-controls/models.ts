export enum WindowViewStatus {
    "Unknown" = "Unknown",
    "Minimized" = "Minimized",
    "UnMaximized" = "UnMaximized",
    "Maximized" = "Maximized",
}

export class WindowState {
    constructor(public viewStatus: WindowViewStatus, public isAlwaysOnTop: boolean) {
    }
}

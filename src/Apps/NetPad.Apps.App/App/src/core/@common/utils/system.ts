import {WindowParams} from "@application/windows/window-params";

export class System {
    /**
     * Opens a URL in system-configured default browser.
     * @param url The URL to open.
     */
    public static openUrlInBrowser(url: string): void {
        const shell = WindowParams.shell;
        if (shell === "electron") {
            /* eslint-disable @typescript-eslint/no-var-requires */
            const {shell} = require("electron");
            const _ = shell.openExternal(url);
            /* eslint-enable @typescript-eslint/no-var-requires */
        } else if (shell == "tauri") {
            /* eslint-disable @typescript-eslint/no-var-requires */
            const open = require("@tauri-apps/plugin-shell").open;
            const _ = open(url);
            /* eslint-enable @typescript-eslint/no-var-requires */
        } else {
            window.open(url, "_blank");
        }
    }

    public static downloadFile(fileName: string, mimeType = "text/plain", base64: string) {
        const downloadLink = document.createElement("A") as HTMLAnchorElement;
        try {
            downloadLink.download = fileName;
            downloadLink.href = `data:${mimeType};base64,${base64}`;
            downloadLink.target = '_blank';
            downloadLink.click();
        } finally {
            downloadLink.remove();
        }
    }

    public static downloadTextAsFile(fileName: string, mimeType = "text/plain", text: string) {
        const downloadLink = document.createElement("A") as HTMLAnchorElement;
        try {
            downloadLink.download = fileName;
            downloadLink.href = `data:${mimeType};charset=utf-8,${encodeURIComponent(text)}`;
            downloadLink.target = '_blank';
            downloadLink.click();
        } finally {
            downloadLink.remove();
        }
    }
}

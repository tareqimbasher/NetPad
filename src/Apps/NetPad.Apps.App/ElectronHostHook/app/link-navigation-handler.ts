import {app, shell} from "electron";

/**
 * Opens a link that the user clicks on in an external browser instead of navigating to that link inside
 * the Electron window.
 */
export class LinkNavigationHandler {
    public static init() {
        app.on("browser-window-created", (ev, window) => {
            window.webContents.on("will-navigate", (event, urlStr) => {
                try {
                    const url = new URL(urlStr);

                    if (url.hostname === "localhost") {
                        return;
                    }

                    const _ = shell.openExternal(urlStr);
                    event.preventDefault();
                } catch (ex) {
                    console.error(ex);
                }
            });

            window.webContents.setWindowOpenHandler(({url}) => {
                const _ = shell.openExternal(url);
                return {action: 'deny'};
            });
        });
    }
}

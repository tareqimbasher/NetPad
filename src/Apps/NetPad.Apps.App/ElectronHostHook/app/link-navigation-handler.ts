import {app, shell} from "electron";

export class LinkNavigationHandler {
    public static init() {
        app.on("browser-window-created", (ev, window) => {
            window.webContents.on("will-navigate", (event, urlStr) => {
                try {
                    const url = new URL(urlStr);

                    if (url.hostname === "localhost") {
                        return;
                    }

                    shell.openExternal(urlStr);
                    event.preventDefault();
                } catch (ex) {
                    console.error(ex);
                }
            });

            window.webContents.setWindowOpenHandler(({url}) => {
                shell.openExternal(url);
                return {action: 'deny'};
            });
        });
    }
}

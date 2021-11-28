import Aurelia, {ColorOptions, ConsoleSink, LoggerConfiguration, LogLevel, Registration} from 'aurelia';
// import "bootstrap";
// import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';
import {ISettingService, Settings} from "@domain";

const startupOptions = new URLSearchParams(window.location.search);

const app = Aurelia.register(
    LoggerConfiguration.create({
        colorOptions: ColorOptions.colors,
        level: LogLevel.trace,
        sinks: [ConsoleSink],
    }),
    Registration.instance(URLSearchParams, startupOptions),
    Registration.instance(String, window.location.origin),
    Registration.cachedCallback<Settings>(Settings, (c) => {
        const settings = new Settings();
        c.get(ISettingService).get().then(s => settings.init(s));
        return settings;
    })
);

const win = startupOptions.get("win");
if (win === "main") {
    const mainWindow = require("./windows/main/main");
    mainWindow.register(app);
} else if (win === "settings") {
    const settingsWindow = require("./windows/settings/main");
    settingsWindow.register(app);
} else if (win === "script-config") {
    const scriptConfigWindow = require("./windows/script-config/main");
    scriptConfigWindow.register(app);
}

app.start();

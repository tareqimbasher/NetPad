import Aurelia, {ColorOptions, ConsoleSink, LoggerConfiguration, LogLevel, Registration} from 'aurelia';
// import "bootstrap";
// import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';
import {
    EventBus,
    IEventBus,
    IIpcGateway,
    ISession,
    ISettingService,
    Session,
    Settings,
    SettingService,
    SignalRIpcGateway
} from "@domain";
import {ExternalLinkCustomAttribute} from "@application";

const startupOptions = new URLSearchParams(window.location.search);

const app = Aurelia.register(
    Registration.singleton(IIpcGateway, SignalRIpcGateway),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.instance(URLSearchParams, startupOptions),
    Registration.instance(String, window.location.origin),
    Registration.singleton(ISettingService, SettingService),
    Registration.cachedCallback<Settings>(Settings, (c) => {
        const settings = new Settings();
        c.get(ISettingService).get().then(s => settings.init(s));
        return settings;
    }),
    LoggerConfiguration.create({
        colorOptions: ColorOptions.colors,
        level: LogLevel.trace,
        sinks: [ConsoleSink],
    }),
    ExternalLinkCustomAttribute
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

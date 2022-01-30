import Aurelia, {ColorOptions, ConsoleSink, ILogger, LoggerConfiguration, LogLevel, Registration} from 'aurelia';
// import "bootstrap";
import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';
import {
    IEventBus,
    IIpcGateway,
    ISession,
    ISettingService,
    Session,
    Settings,
    SettingService,
} from "@domain";
import {
    EventBus,
    SignalRIpcGateway,
    DateTimeValueConverter,
    ExternalLinkCustomAttribute,
    SanitizeHtmlValueConverter,
    TextToHtmlValueConverter,
    SettingsBackgroundService
} from "@application";
import {IBackgroundService} from "@common";

const startupOptions = new URLSearchParams(window.location.search);

const app = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, startupOptions),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IIpcGateway, SignalRIpcGateway),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.singleton(ISettingService, SettingService),
    Registration.transient(IBackgroundService, SettingsBackgroundService),
    LoggerConfiguration.create({
        colorOptions: ColorOptions.colors,
        level: LogLevel.trace,
        sinks: [ConsoleSink],
    }),
    ExternalLinkCustomAttribute,
    DateTimeValueConverter,
    TextToHtmlValueConverter,
    SanitizeHtmlValueConverter,
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

app.container.get(ISettingService).get()
    .then(settings => app.container.get(Settings).init(settings))
    .then(async () => {
        const backgroundServices = app.container.getAll(IBackgroundService);

        const logger = app.container.get(ILogger);
        logger.info(`Starting ${backgroundServices.length} background services`);

        for (const backgroundService of backgroundServices) {
            try {
                await backgroundService.start();
            } catch (ex) {
                logger.error(`Error starting background service ${backgroundService.constructor.name}. ${ex.toString()}`);
            }
        }

    })
    .then(() => app.start());

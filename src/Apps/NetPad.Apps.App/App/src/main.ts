import Aurelia, {ColorOptions, ConsoleSink, ILogger, LoggerConfiguration, LogLevel, Registration} from 'aurelia';
import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';
import {IEventBus, IIpcGateway, ISession, ISettingService, Session, Settings, SettingService,} from "@domain";
import {
    DateTimeValueConverter,
    WebDialogBackgroundService,
    EventBus,
    ExternalLinkCustomAttribute,
    IWindowBootstrap,
    SanitizeHtmlValueConverter,
    SettingsBackgroundService,
    SignalRIpcGateway,
    TextToHtmlValueConverter,
    WebWindowBackgroundService,
    PlatformsCustomAttribute
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
    Registration.transient(IBackgroundService, WebDialogBackgroundService),
    Registration.transient(IBackgroundService, WebWindowBackgroundService),
    LoggerConfiguration.create({
        colorOptions: ColorOptions.colors,
        level: LogLevel.trace,
        sinks: [ConsoleSink],
    }),
    ExternalLinkCustomAttribute,
    PlatformsCustomAttribute,
    DateTimeValueConverter,
    TextToHtmlValueConverter,
    SanitizeHtmlValueConverter
);


const winOpt = startupOptions.get("win");
let win: any;

if (winOpt === "main")
    win = require("./windows/main/main");
else if (winOpt === "settings")
    win = require("./windows/settings/main");
else if (winOpt === "script-config")
    win = require("./windows/script-config/main");

const bootstrapper = new win.Bootstrapper() as IWindowBootstrap;
bootstrapper.registerServices(app);

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
    .then(() => {
        app
            .app(bootstrapper.getEntry())
            .start();
    })
;

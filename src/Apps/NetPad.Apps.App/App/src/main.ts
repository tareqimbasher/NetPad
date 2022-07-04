import Aurelia, {
    AppTask,
    ColorOptions,
    ConsoleSink, IContainer,
    ILogger,
    LoggerConfiguration,
    LogLevel,
    Registration
} from 'aurelia';
import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';
import {IEventBus, IIpcGateway, ISession, ISettingService, Session, Settings, SettingService,} from "@domain";
import {
    EventBus,
    IWindowBootstrap,
    SignalRIpcGateway,
    WebDialogBackgroundService,
    SettingsBackgroundService,
    WebWindowBackgroundService,
    ContextMenu,
    ExternalLinkCustomAttribute,
    PlatformsCustomAttribute,
    DateTimeValueConverter,
    SanitizeHtmlValueConverter,
    TextToHtmlValueConverter,
    YesNoValueConverter,
    TakeValueConverter,
    SortValueConverter
} from "@application";
import {AppMutationObserver, IBackgroundService, System} from "@common";

const startupOptions = new URLSearchParams(window.location.search);

const app = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, startupOptions),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IIpcGateway, SignalRIpcGateway),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.singleton(ISettingService, SettingService),
    Registration.singleton(AppMutationObserver, AppMutationObserver),
    Registration.transient(IBackgroundService, SettingsBackgroundService),
    Registration.transient(IBackgroundService, WebDialogBackgroundService),
    Registration.transient(IBackgroundService, WebWindowBackgroundService),
    LoggerConfiguration.create({
        colorOptions: ColorOptions.colors,
        level: LogLevel.debug,
        sinks: [ConsoleSink],
    }),

    // Custom Attributes
    ExternalLinkCustomAttribute,
    PlatformsCustomAttribute,

    // Value Converters
    DateTimeValueConverter,
    TakeValueConverter,
    SortValueConverter,
    TextToHtmlValueConverter,
    SanitizeHtmlValueConverter,
    YesNoValueConverter,

    // Custom elements
    ContextMenu,


    AppTask.beforeActivate(IContainer, async container => {
        const backgroundServices = container.getAll(IBackgroundService);

        const logger = container.get(ILogger);
        logger.info(`Starting ${backgroundServices.length} background services`);

        for (const backgroundService of backgroundServices) {
            try {
                await backgroundService.start();
            } catch (ex) {
                logger.error(`Error starting background service ${backgroundService.constructor.name}. ${ex.toString()}`);
            }
        }
    })
);

// Determine which window we need to bootstrap and use
let winOpt = startupOptions.get("win");
if (!winOpt && !System.isRunningInElectron())
    winOpt = "main";

let win: any;

if (winOpt === "main")
    win = require("./windows/main/main");
else if (winOpt === "settings")
    win = require("./windows/settings/main");
else if (winOpt === "script-config")
    win = require("./windows/script-config/main");

const bootstrapper = new win.Bootstrapper() as IWindowBootstrap;
bootstrapper.registerServices(app);

// Load Settings and then start the app
app.container.get(ISettingService).get()
    .then(settings => Object.assign(app.container.get(Settings), settings))
    .then(() => {
        app
            .app(bootstrapper.getEntry())
            .start();
    })
;

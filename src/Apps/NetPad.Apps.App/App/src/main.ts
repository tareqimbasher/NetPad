import Aurelia, {AppTask, ColorOptions, IContainer, ILogger, LogLevel, Registration} from 'aurelia';
import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import {
    AppService,
    IAppService,
    IEventBus,
    IIpcGateway,
    ISession,
    ISettingService,
    Session,
    Settings,
    SettingService
} from "@domain";
import {
    ConsoleLogSink,
    ContextMenu,
    DateTimeValueConverter,
    Env,
    EventBus,
    ExternalLinkCustomAttribute,
    FindTextBox,
    IWindowBootstrapperConstructor,
    LogConfig,
    PlatformsCustomAttribute,
    RemoteLogSink,
    SanitizeHtmlValueConverter,
    SettingsBackgroundService,
    SignalRIpcGateway,
    SortValueConverter,
    TakeValueConverter,
    TextToHtmlValueConverter,
    YesNoValueConverter
} from "@application";
import {AppMutationObserver, IBackgroundService} from "@common";
import {WebApp} from "@application/apps/web-app";

const startupOptions = new URLSearchParams(window.location.search);

// Register common dependencies shared for all windows
const app = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, startupOptions),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IAppService, AppService),
    Registration.singleton(IIpcGateway, SignalRIpcGateway),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.singleton(ISettingService, SettingService),
    Registration.singleton(AppMutationObserver, AppMutationObserver),
    Registration.transient(IBackgroundService, SettingsBackgroundService),
    LogConfig.register({
        colorOptions: ColorOptions.colors,
        level: Env.Environment === "PRD" ? LogLevel.info : LogLevel.debug,
        sinks: Env.RemoteLoggingEnabled ? [ConsoleLogSink, RemoteLogSink] : [ConsoleLogSink],
        rules: [
            {
                // Suppress Aurelia's own debug messages when evaluating HTML case expressions
                loggerRegex: new RegExp("^Case-#"),
                logLevel: LogLevel.none
            }
        ]
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

    // Custom elements that we want available everywhere
    ContextMenu,
    FindTextBox,

    // Tasks that run before the app is activated
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

if (!Env.isRunningInElectron()) {
    WebApp.configure(app);
}

// Load app settings
const settings = await app.container.get(ISettingService).get();
app.container.get(Settings).init(settings.toJSON());

// Determine which window we need to bootstrap and use
let windowName = startupOptions.get("win");
if (!windowName && !Env.isRunningInElectron())
    windowName = "main";

let bootstrapperCtor: IWindowBootstrapperConstructor;

/* eslint-disable @typescript-eslint/no-var-requires */
if (windowName === "main")
    bootstrapperCtor = require("./windows/main/main").Bootstrapper;
else if (windowName === "settings")
    bootstrapperCtor = require("./windows/settings/main").Bootstrapper;
else if (windowName === "script-config")
    bootstrapperCtor = require("./windows/script-config/main").Bootstrapper;
else if (windowName === "data-connection")
    bootstrapperCtor = require("./windows/data-connection/main").Bootstrapper;
else
    throw new Error(`Unrecognized window: ${windowName}`);
/* eslint-enable @typescript-eslint/no-var-requires */

const bootstrapper = new bootstrapperCtor(app.container.get(ILogger));
bootstrapper.registerServices(app);

// Start the app
app.app(bootstrapper.getEntry()).start();

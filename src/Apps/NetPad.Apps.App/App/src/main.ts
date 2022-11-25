import Aurelia, {
    AppTask,
    ColorOptions,
    ConsoleSink,
    IContainer,
    ILogger,
    LoggerConfiguration,
    LogLevel,
    Registration
} from 'aurelia';
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
    ContextMenu,
    DateTimeValueConverter,
    Env,
    EventBus,
    ExternalLinkCustomAttribute,
    IWindowBootstrapperConstructor,
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

console.log("Running environment: " + Env.Environment);

let loadingApp: Aurelia | undefined;

if ((!startupOptions.get("win") || startupOptions.get("win") === "main") && !Env.isRunningInElectron()) {
    loadingApp = new Aurelia();
    const loadingScreen = require("./loading-screen/loading-screen");
    loadingApp.app(loadingScreen.LoadingScreen).start();
}

// Register common dependencies
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
    LoggerConfiguration.create({
        colorOptions: ColorOptions.colors,
        level: Env.Environment === "PRD" ? LogLevel.info : LogLevel.debug,
        sinks: Env.RemoteLoggingEnabled ? [ConsoleSink, RemoteLogSink] : [ConsoleSink],
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

    // Tasks
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
let winOpt = startupOptions.get("win");
if (!winOpt && !Env.isRunningInElectron())
    winOpt = "main";

let bootstrapperCtor: IWindowBootstrapperConstructor;

/* eslint-disable @typescript-eslint/no-var-requires */
if (!winOpt || winOpt === "main")
    bootstrapperCtor = require("./windows/main/main").Bootstrapper;
else if (winOpt === "settings")
    bootstrapperCtor = require("./windows/settings/main").Bootstrapper;
else if (winOpt === "script-config")
    bootstrapperCtor = require("./windows/script-config/main").Bootstrapper;
else if (winOpt === "data-connection")
    bootstrapperCtor = require("./windows/data-connection/main").Bootstrapper;
else
    throw new Error(`Unrecognized window parameter: ${winOpt}`);
/* eslint-enable @typescript-eslint/no-var-requires */

const bootstrapper = new bootstrapperCtor(app.container.get(ILogger));
bootstrapper.registerServices(app);

// Start the app
app.app(bootstrapper.getEntry()).start();
const start = app.app(bootstrapper.getEntry()).start() as Promise<void>;

if (loadingApp) {
    start.then(() => {
        if (!loadingApp) {
            console.warn("LoadingApp was expected to not be null or undefined.");
        }

        loadingApp?.stop(true);
        document.body.classList.remove("loading-screen");
    });
}

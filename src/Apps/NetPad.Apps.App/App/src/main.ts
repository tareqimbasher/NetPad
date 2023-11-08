import Aurelia, {AppTask, ColorOptions, IContainer, ILogger, LogLevel, Registration} from 'aurelia';
import {DialogDefaultConfiguration} from "@aurelia/dialog";
import "bootstrap";
import "./styles/main.scss";
import "@common/globals";
import {AppMutationObserver, IBackgroundService} from "@common";
import {
    AppService,
    Env,
    IAppService,
    IEventBus,
    IIpcGateway,
    ISession,
    ISettingsService,
    Session,
    Settings,
    SettingsService,
} from "@domain";
import {
    ConsoleLogSink,
    ContextMenu,
    DateTimeValueConverter,
    EventBus,
    ExternalLinkCustomAttribute,
    FindTextBox,
    LangLogoValueConverter,
    LogConfig,
    PlatformsCustomAttribute,
    RemoteLogSink,
    SanitizeHtmlValueConverter,
    SignalRIpcGateway,
    SortValueConverter,
    TakeValueConverter,
    TextToHtmlValueConverter,
    TooltipCustomAttribute,
    TruncateValueConverter,
    YesNoValueConverter
} from "@application";
import * as appTasks from "./main.tasks";
import {AppLifeCycle} from "./main.app-lifecycle";
import {IPlatform} from "@application/platforms/iplatform";
import {SettingsBackgroundService} from "@application/background-services/settings-background-service";

// Register common dependencies shared for all windows
const builder = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, new URLSearchParams(window.location.search)),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IAppService, AppService),
    Registration.singleton(IIpcGateway, SignalRIpcGateway),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.singleton(ISettingsService, SettingsService),
    Registration.singleton(AppMutationObserver, AppMutationObserver),
    Registration.singleton(IBackgroundService, SettingsBackgroundService),
    LogConfig.register({
        colorOptions: ColorOptions.colors,
        level: Env.isProduction ? LogLevel.info : LogLevel.debug,
        sinks: Env.RemoteLoggingEnabled ? [ConsoleLogSink, RemoteLogSink] : [ConsoleLogSink],
        rules: [
            {
                loggerRegex: new RegExp(/AppLifeCycle/),
                logLevel: Env.isProduction ? LogLevel.warn : LogLevel.debug
            },
            ...(Env.isDebug
                // These guys can get a bit chatty at debug level. Should move to .env
                ? [
                    {
                        // Aurelia's own debug messages when evaluating HTML case expressions
                        loggerRegex: new RegExp(/^Case-#/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/.\.ComponentLifecycle/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/ShortcutManager/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/ViewerHost/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/ContextMenu/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/SignalRIpcGateway/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/ElectronIpcGateway/),
                        logLevel: LogLevel.warn
                    },
                    {
                        loggerRegex: new RegExp(/ElectronEventSync/),
                        logLevel: LogLevel.warn
                    },
                ]
                : []),

        ]
    }),
    DialogDefaultConfiguration.customize((config) => {
        config.lock = true;
    }),


    // Global Custom Attributes
    ExternalLinkCustomAttribute,
    PlatformsCustomAttribute,
    TooltipCustomAttribute,

    // Global Value Converters
    DateTimeValueConverter,
    TakeValueConverter,
    SortValueConverter,
    TextToHtmlValueConverter,
    SanitizeHtmlValueConverter,
    YesNoValueConverter,
    TruncateValueConverter,
    LangLogoValueConverter,

    // Global Custom Elements
    ContextMenu,
    FindTextBox,
);

const logger = builder.container.get(ILogger).scopeTo(nameof(AppLifeCycle));

// Configure app lifecycle actions
const appLifeCycle = new AppLifeCycle(logger, builder.container.get(IEventBus));
builder.register(
    AppTask.creating(IContainer, async (container) => appLifeCycle.creating(container)),
    AppTask.hydrating(IContainer, async (container) => appLifeCycle.hydrating(container)),
    AppTask.hydrated(IContainer, async (container) => appLifeCycle.hydrated(container)),
    AppTask.activating(IContainer, async (container) => appLifeCycle.activating(container)),
    AppTask.activated(IContainer, async (container) => appLifeCycle.activated(container)),
    AppTask.deactivating(IContainer, async (container) => appLifeCycle.deactivating(container)),
    AppTask.deactivated(IContainer, async (container) => appLifeCycle.deactivated(container)),
);

// Configure the proper platform
const platformType = Env.isRunningInElectron()
    ? (await import("@application/platforms/electron/electron-platform")).ElectronPlatform
    : (await import("@application/platforms/browser/browser-platform")).BrowserPlatform;

const platform = new platformType() as IPlatform;
logger.debug(`Configuring platform: ${platform.constructor.name}`);
platform.configure(builder);

// Load app settings
const settings = await builder.container.get(ISettingsService).get();
builder.container.get(Settings).init(settings.toJSON());

// Start the app
const entryPoint = appTasks.configureAndGetAppEntryPoint(builder);

const app = builder.app(entryPoint);

await app.start();

window.addEventListener("beforeunload", (e) => app.stop(true));

logger.debug("App started");

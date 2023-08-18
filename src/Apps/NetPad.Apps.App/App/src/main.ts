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
    ISettingService,
    IWindowService,
    Session,
    Settings,
    SettingService,
    WindowService
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
    SettingsBackgroundService,
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

// Register common dependencies shared for all windows
const builder = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, new URLSearchParams(window.location.search)),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IAppService, AppService),
    Registration.singleton(IWindowService, WindowService),
    Registration.singleton(IIpcGateway, SignalRIpcGateway),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.singleton(ISettingService, SettingService),
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
            {
                loggerRegex: new RegExp(/SignalRIpcGateway/),
                logLevel: Env.isProduction ? LogLevel.warn : LogLevel.debug
            },
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
                loggerRegex: new RegExp(/ContextMenu/),
                logLevel: LogLevel.warn
            },
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
const appLifeCycle = new AppLifeCycle(logger);
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
    ? (await import("@application/platforms/electron-platform")).ElectronPlatform
    : (await import("@application/platforms/web-platform")).WebPlatform;

const platform = new platformType() as IPlatform;
platform.configure(builder);

// Load app settings
const settings = await builder.container.get(ISettingService).get();
builder.container.get(Settings).init(settings.toJSON());

// Start the app
const entryPoint = appTasks.configureAndGetAppEntryPoint(builder);

const app = builder.app(entryPoint);

await app.start();

window.addEventListener("beforeunload", (e) => app.stop(true));

logger.debug("App started");

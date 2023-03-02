import Aurelia, {
    AppTask,
    ColorOptions,
    DialogDefaultConfiguration,
    IContainer,
    ILogger,
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
    Env,
    EventBus,
    ExternalLinkCustomAttribute,
    FindTextBox,
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
import * as appTasks from "./main.tasks";
import {AppLifeCycle} from "./main.app-lifecycle";

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
        level: Env.Environment === "PRD" ? LogLevel.info : LogLevel.debug,
        sinks: Env.RemoteLoggingEnabled ? [ConsoleLogSink, RemoteLogSink] : [ConsoleLogSink],
        rules: [
            {
                // Suppress Aurelia's own debug messages when evaluating HTML case expressions
                loggerRegex: new RegExp("^Case-#"),
                logLevel: LogLevel.none
            },
            {
                loggerRegex: new RegExp("ShortcutManager"),
                logLevel: LogLevel.none
            }
        ]
    }),
    DialogDefaultConfiguration.customize((config) => {
        config.lock = true;
    }),


    // Global Custom Attributes
    ExternalLinkCustomAttribute,
    PlatformsCustomAttribute,

    // Global Value Converters
    DateTimeValueConverter,
    TakeValueConverter,
    SortValueConverter,
    TextToHtmlValueConverter,
    SanitizeHtmlValueConverter,
    YesNoValueConverter,

    // Global Custom Elements
    ContextMenu,
    FindTextBox,
);

const logger = builder.container.get(ILogger).scopeTo(nameof(AppLifeCycle));

const appLifeCycle = new AppLifeCycle(logger);

builder.register(
    // Tasks that run at specific points in the app's lifecycle
    AppTask.beforeCreate(IContainer, async (container) => appLifeCycle.beforeCreate(container)),
    AppTask.hydrating(IContainer, async (container) => appLifeCycle.hydrating(container)),
    AppTask.hydrated(IContainer, async (container) => appLifeCycle.hydrated(container)),
    AppTask.beforeActivate(IContainer, async (container) => appLifeCycle.beforeActivate(container)),
    AppTask.afterActivate(IContainer, async (container) => appLifeCycle.afterActivate(container)),
    AppTask.beforeDeactivate(IContainer, async (container) => appLifeCycle.beforeDeactivate(container)),
    AppTask.afterDeactivate(IContainer, async (container) => appLifeCycle.afterDeactivate(container)),
)

if (!Env.isRunningInElectron()) {
    WebApp.configure(builder);
}

// Load app settings
const settings = await builder.container.get(ISettingService).get();
builder.container.get(Settings).init(settings.toJSON());

// Start the app
const entryPoint = appTasks.configureAndGetAppEntryPoint(builder);

const app = builder.app(entryPoint);

logger.debug("Starting app...");

await app.start();

logger.debug("App started");

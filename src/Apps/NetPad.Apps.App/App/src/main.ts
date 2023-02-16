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

const startupOptions = new URLSearchParams(window.location.search);

// Register common dependencies shared for all windows
const app = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, startupOptions),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IAppService, AppService),
    Registration.singleton(IWindowService, WindowService),
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

    // Global custom elements that we want available everywhere without needing
    // to require (import) them in our HTML or JS
    ContextMenu,
    FindTextBox,

    // Tasks that run at specific points in the app's lifecycle
    AppTask.beforeActivate(IContainer, appTasks.configureFetchClient),
    AppTask.beforeActivate(IContainer, appTasks.startBackgroundServices),
    AppTask.afterActivate(IContainer, container => container.get(ILogger).debug("App activated"))
);

if (!Env.isRunningInElectron()) {
    WebApp.configure(app);
}

// Load app settings
const settings = await app.container.get(ISettingService).get();
app.container.get(Settings).init(settings.toJSON());

// Start the app
const entryPoint = appTasks.configureAppEntryPoint(app);
app.app(entryPoint).start();

import Aurelia, {AppTask, ILogger, LogLevel, Registration} from 'aurelia';
import {CustomElementType} from "@aurelia/runtime-html";
import {DialogDefaultConfiguration} from "@aurelia/dialog";
import "bootstrap";
import "./styles/main.scss";
import "@common/globals";
import {AppMutationObserver} from "@common";
import {
    ConsoleLogSink,
    ContextMenu,
    DateTimeValueConverter,
    Env,
    ExternalLinkCustomAttribute,
    IAppService,
    IBackgroundService,
    IEventBus,
    ISession,
    ISettingsService,
    LangLogoValueConverter,
    LogConfig,
    PlatformsCustomAttribute,
    RemoteLogSink,
    SanitizeHtmlValueConverter,
    Settings,
    SortValueConverter,
    TakeValueConverter,
    TextToHtmlValueConverter,
    TooltipCustomAttribute,
    TruncateValueConverter,
    YesNoValueConverter,
} from "@application";
import * as appTasks from "./main.tasks";
import {AppLifeCycle} from "./main.app-lifecycle";
import {SettingsBackgroundService} from "@application/background-services/settings-background-service";
import {AppService} from "@application/app/app-service";
import {SettingsService} from "@application/configuration/settings-service";
import {Session} from "@application/sessions/session";
import {EventBus} from "@application/events/event-bus";
import {FindTextBox} from "@application/find-text-box/find-text-box";

// Register common dependencies shared for all windows/apps
const builder = Aurelia.register(
    Registration.instance(String, window.location.origin),
    Registration.instance(URLSearchParams, new URLSearchParams(window.location.search)),
    Registration.singleton(AppLifeCycle, AppLifeCycle),
    Registration.instance(Settings, new Settings()),
    Registration.singleton(IAppService, AppService),
    Registration.singleton(IEventBus, EventBus),
    Registration.singleton(ISession, Session),
    Registration.singleton(ISettingsService, SettingsService),
    Registration.singleton(AppMutationObserver, AppMutationObserver),
    Registration.singleton(IBackgroundService, SettingsBackgroundService),
    DialogDefaultConfiguration.customize((config) => {
        config.lock = true;
    }),

    LogConfig.register({
        colorOptions: "colors",
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

    // Register app lifecycle actions
    AppTask.creating(AppLifeCycle, async (appLifeCycle) => appLifeCycle.creating()),
    AppTask.hydrating(AppLifeCycle, async (appLifeCycle) => appLifeCycle.hydrating()),
    AppTask.hydrated(AppLifeCycle, async (appLifeCycle) => appLifeCycle.hydrated()),
    AppTask.activating(AppLifeCycle, async (appLifeCycle) => appLifeCycle.activating()),
    AppTask.activated(AppLifeCycle, async (appLifeCycle) => appLifeCycle.activated()),
    AppTask.deactivating(AppLifeCycle, async (appLifeCycle) => appLifeCycle.deactivating()),
    AppTask.deactivated(AppLifeCycle, async (appLifeCycle) => appLifeCycle.deactivated()),
);

const logger = builder.container.get(ILogger).scopeTo(nameof(AppLifeCycle));

// Configure the proper platform
const platform = await appTasks.configureAndGetPlatform(builder);
logger.debug(`Configured platform: ${platform.constructor.name}`);

// Load app settings
const settings = await builder.container.get(ISettingsService).get();
builder.container.get(Settings).init(settings.toJSON());

// Start the app
const entryPoint = await appTasks.configureAndGetAppEntryPoint(builder);
const app = builder.app(entryPoint as CustomElementType);
window.addEventListener("unload", () => app.stop(true));
await app.start();
logger.debug("App started");

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
    ShellsCustomAttribute,
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
import * as appActions from "./app-actions";
import {AppLifeCycle} from "./app-life-cycle";
import {SettingsBackgroundService} from "@application/background-services/settings-background-service";
import {AppService} from "@application/app/app-service";
import {SettingsService} from "@application/configuration/settings-service";
import {Session} from "@application/sessions/session";
import {EventBus} from "@application/events/event-bus";
import {FindTextBox} from "@application/find-text-box/find-text-box";

// Register common dependencies shared across entire application (all windows)
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

    LogConfig.register({
        colorOptions: "colors",
        level: Env.isProduction ? LogLevel.info : LogLevel.debug,
        sinks: Env.RemoteLoggingEnabled ? [ConsoleLogSink, RemoteLogSink] : [ConsoleLogSink],
        rules: appActions.logRules
    }),

    // Globally registered custom attributes
    ExternalLinkCustomAttribute,
    ShellsCustomAttribute,
    TooltipCustomAttribute,

    // Globally registered value converters
    DateTimeValueConverter,
    LangLogoValueConverter,
    SortValueConverter,
    SanitizeHtmlValueConverter,
    TakeValueConverter,
    TextToHtmlValueConverter,
    TruncateValueConverter,
    YesNoValueConverter,

    // Globally registered custom elements
    ContextMenu,
    FindTextBox,

    DialogDefaultConfiguration.customize((config) => {
        config.lock = true;
    }),

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

// Configure the proper shell
const shell = await appActions.configureAndGetShell(builder);
logger.debug(`Configured for shell: ${shell.constructor.name}`);

// Start the app
await appActions.loadAppSettings(builder);

const entryPoint = await appActions.configureAndGetAppEntryPoint(builder);
const app = builder.app(entryPoint as CustomElementType);

window.addEventListener("unload", () => app.stop(true));

await app.start();
logger.debug("App started");

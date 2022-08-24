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
import {IEventBus, IIpcGateway, ISession, ISettingService, Session, Settings, SettingService} from "@domain";
import {
    ContextMenu,
    DateTimeValueConverter,
    EventBus,
    ExternalLinkCustomAttribute,
    IWindowBootstrapperConstructor,
    PlatformsCustomAttribute,
    SanitizeHtmlValueConverter,
    SettingsBackgroundService,
    SignalRIpcGateway,
    SortValueConverter,
    TakeValueConverter,
    TextToHtmlValueConverter,
    YesNoValueConverter
} from "@application";
import {AppMutationObserver, IBackgroundService, System} from "@common";

const startupOptions = new URLSearchParams(window.location.search);

// Register common dependencies
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

// Load app settings
const settings = await app.container.get(ISettingService).get();
app.container.get(Settings).init(settings.toJSON());

// Determine which window we need to bootstrap and use
let winOpt = startupOptions.get("win");
if (!winOpt && !System.isRunningInElectron())
    winOpt = "main";

let bootstrapperCtor: IWindowBootstrapperConstructor;

/* eslint-disable @typescript-eslint/no-var-requires */
if (winOpt === "main")
    bootstrapperCtor = require("./windows/main/main").Bootstrapper;
else if (winOpt === "settings")
    bootstrapperCtor = require("./windows/settings/main").Bootstrapper;
else if (winOpt === "script-config")
    bootstrapperCtor = require("./windows/script-config/main").Bootstrapper;
else if (winOpt === "data-connection")
    bootstrapperCtor = require("./windows/data-connection/main").Bootstrapper;
/* eslint-enable @typescript-eslint/no-var-requires */

const bootstrapper = new bootstrapperCtor(app.container.get(ILogger));
bootstrapper.registerServices(app);

// Start the app
app.app(bootstrapper.getEntry()).start();

import {IAurelia, IContainer, ILogger, LogLevel} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {
    Env,
    IBackgroundService,
    ILoggerRule,
    ISettingsService,
    IWindowBootstrapperConstructor,
    Settings
} from "@application";
import {IPlatform} from "@application/platforms/iplatform";

/**
 * Loads main app settings.
 */
export const loadAppSettings = async (builder: IAurelia) => {
    const settings = builder.container.get(Settings);
    const settingsService = builder.container.get(ISettingsService);
    const latestSettings = await settingsService.get();

    settings.init(latestSettings.toJSON());
}

/**
 * Selects and configures the proper platform.
 */
export const configureAndGetPlatform = async (builder: IAurelia) => {
    const platformType = Env.isRunningInElectron()
        ? (await import("@application/platforms/electron/electron-platform")).ElectronPlatform
        : (await import("@application/platforms/browser/browser-platform")).BrowserPlatform;

    const platform = new platformType() as IPlatform;

    platform.configure(builder);

    return platform;
}

/**
 * Selects and configures the correct app entry point. An entry point is a view-model representing the window
 * that will be the entry point for the Aurelia app.
 */
export const configureAndGetAppEntryPoint = async (builder: IAurelia) => {
    const startupOptions = builder.container.get(URLSearchParams);

    // Determine which window needs to be bootstrapped using the 'win' query parameter of the current window
    let windowName = startupOptions.get("win");

    if (!windowName && !Env.isRunningInElectron()) {
        windowName = "main";
    }

    let bootstrapperCtor: IWindowBootstrapperConstructor;

    if (windowName === "main")
        bootstrapperCtor = (await import("./windows/main/main-window-bootstrapper")).MainWindowBootstrapper;
    else if (windowName === "script-config")
        bootstrapperCtor = (await import("./windows/script-config/script-config-window-bootstrapper")).ScriptConfigWindowBootstrapper;
    else if (windowName === "data-connection")
        bootstrapperCtor = (await import("./windows/data-connection/data-connection-window-bootstrapper")).DataConnectionWindowBootstrapper;
    else if (windowName === "settings")
        bootstrapperCtor = (await import("./windows/settings/settings-window-bootstrapper")).SettingsWindowBootstrapper;
    else if (windowName === "output")
        bootstrapperCtor = (await import("./windows/output/output-window-bootstrapper")).OutputWindowBootstrapper;
    else if (windowName === "code")
        bootstrapperCtor = (await import("./windows/code/code-window-bootstrapper")).CodeWindowBootstrapper;
    else
        throw new Error(`Unrecognized window: ${windowName}`);

    const bootstrapper = new bootstrapperCtor(builder.container.get(ILogger));

    bootstrapper.registerServices(builder);

    return bootstrapper.getEntry();
}

export const startBackgroundServices = async (container: IContainer) => {
    const backgroundServices = container.getAll(IBackgroundService);

    const logger = container.get(ILogger);
    logger.debug(`Starting ${backgroundServices.length} background services`, backgroundServices.map(x => x.constructor.name));

    for (const backgroundService of backgroundServices) {
        try {
            await backgroundService.start();
        } catch (ex) {
            if (ex instanceof Error)
                logger.error(`Error starting background service ${backgroundService.constructor.name}. ${ex.toString()}`);
        }
    }
}

export const stopBackgroundServices = async (container: IContainer) => {
    const backgroundServices = container.getAll(IBackgroundService);

    const logger = container.get(ILogger);
    logger.debug(`Stopping ${backgroundServices.length} background services`, backgroundServices.map(x => x.constructor.name));

    for (const backgroundService of backgroundServices) {
        try {
            backgroundService.stop();

            const dispose = backgroundService["dispose" as keyof typeof backgroundService];
            if (typeof dispose === "function") {
                backgroundService["dispose" as keyof typeof backgroundService]();
            }
        } catch (ex) {
            if (ex instanceof Error)
                logger.error(`Error stopping background service ${backgroundService.constructor.name}. ${ex.toString()}`);
        }
    }
};

export const configureFetchClient = (container: IContainer) => {
    const client = container.get(IHttpClient);
    const logger = container.get(ILogger).scopeTo("http-client");

    const isAbortError = (error: unknown) => error instanceof Error && error.name?.startsWith("AbortError");

    client.configure(config =>
        config
            .useStandardConfiguration()
            .withInterceptor({
                requestError(error: unknown): Request | Response | Promise<Request | Response> {
                    if (!isAbortError(error)) logger.error("Request Error", error);
                    throw error;
                },
                responseError(error: unknown, request?: Request): Response | Promise<Response> {
                    if (!isAbortError(error)) logger.error("Response Error", error);
                    throw error;
                }
            })
    );
}

export const logRules: ILoggerRule[] = [
    {
        loggerRegex: new RegExp(/AppLifeCycle/),
        logLevel: Env.isProduction ? LogLevel.warn : LogLevel.debug
    },
    ...(Env.isDebug ? [
            // When in dev mode these loggers can get a bit chatty, increase their min level.
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
            {
                // Aurelia's own debug messages when evaluating HTML case expressions
                loggerRegex: new RegExp(/^Case-#/),
                logLevel: LogLevel.warn
            },
        ] : []
    ),
];

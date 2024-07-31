import {Constructable, IAurelia, IContainer, ILogger, LogLevel} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {
    Env,
    IBackgroundService,
    ILoggerRule,
    ISettingsService,
    IWindowBootstrapperConstructor,
    Settings
} from "@application";
import {IShell} from "@application/shells/ishell";
import {WindowParams} from "@application/windows/window-params";
import {WindowId} from "@application/windows/window-id";

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
 * Selects and configures the proper shell.
 */
export const configureAndGetShell = async (builder: IAurelia) => {
    const windowParams = builder.container.get(WindowParams);

    let shellType: Constructable<IShell>;

    if (windowParams.shell === "electron") {
        shellType = (await import("@application/shells/electron/electron-shell")).ElectronShell;
    } else if (windowParams.shell === "tauri") {
        shellType = (await import("@application/shells/tauri/tauri-shell")).TauriShell;
    } else {
        shellType = (await import("@application/shells/browser/browser-shell")).BrowserShell;
    }

    const shell = new shellType() as IShell;

    shell.configure(builder);

    return shell;
}

/**
 * Selects and configures the correct app entry point. An entry point is a view-model representing the window
 * that will be the entry point for the Aurelia app.
 */
export const configureAndGetAppEntryPoint = async (builder: IAurelia) => {
    const windowParams = builder.container.get(WindowParams);

    const windowId = windowParams.window;

    let bootstrapperCtor: IWindowBootstrapperConstructor;

    if (windowId === WindowId.Main)
        bootstrapperCtor = (await import("./windows/main/main-window-bootstrapper")).MainWindowBootstrapper;
    else if (windowId === WindowId.Settings)
        bootstrapperCtor = (await import("./windows/settings/settings-window-bootstrapper")).SettingsWindowBootstrapper;
    else if (windowId === WindowId.ScriptConfig)
        bootstrapperCtor = (await import("./windows/script-config/script-config-window-bootstrapper")).ScriptConfigWindowBootstrapper;
    else if (windowId === WindowId.DataConnection)
        bootstrapperCtor = (await import("./windows/data-connection/data-connection-window-bootstrapper")).DataConnectionWindowBootstrapper;
    else if (windowId === WindowId.Output)
        bootstrapperCtor = (await import("./windows/output/output-window-bootstrapper")).OutputWindowBootstrapper;
    else if (windowId === WindowId.Code)
        bootstrapperCtor = (await import("./windows/code/code-window-bootstrapper")).CodeWindowBootstrapper;
    else
        throw new Error(`Unrecognized window: ${windowId}`);

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
        } catch (ex) {
            if (ex instanceof Error)
                logger.error(`Error stopping background service ${backgroundService.constructor.name}. ${ex.toString()}`);
        }

        try {
            const dispose = backgroundService["dispose" as keyof typeof backgroundService];
            if (typeof dispose === "function") {
                backgroundService["dispose" as keyof typeof backgroundService]();
            }
        } catch (ex) {
            if (ex instanceof Error)
                logger.error(`Error disposing background service ${backgroundService.constructor.name}. ${ex.toString()}`);
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

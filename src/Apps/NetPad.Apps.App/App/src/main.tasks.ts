import {IAurelia, IContainer, ILogger} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {Env, IBackgroundService, IWindowBootstrapperConstructor} from "@application";

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

export const configureAndGetAppEntryPoint = (builder: IAurelia) => {
    const startupOptions = builder.container.get(URLSearchParams);

    // Determine which window we need to bootstrap and use
    let windowName = startupOptions.get("win");
    if (!windowName && !Env.isRunningInElectron())
        windowName = "main";

    let bootstrapperCtor: IWindowBootstrapperConstructor;

    /* eslint-disable @typescript-eslint/no-var-requires */
    if (windowName === "main")
        bootstrapperCtor = require("./windows/main/main").Bootstrapper;
    else if (windowName === "output")
        bootstrapperCtor = require("./windows/output/main").Bootstrapper;
    else if (windowName === "code")
        bootstrapperCtor = require("./windows/code/main").Bootstrapper;
    else if (windowName === "settings")
        bootstrapperCtor = require("./windows/settings/main").Bootstrapper;
    else if (windowName === "script-config")
        bootstrapperCtor = require("./windows/script-config/main").Bootstrapper;
    else if (windowName === "data-connection")
        bootstrapperCtor = require("./windows/data-connection/main").Bootstrapper;
    else
        throw new Error(`Unrecognized window: ${windowName}`);
    /* eslint-enable @typescript-eslint/no-var-requires */

    const bootstrapper = new bootstrapperCtor(builder.container.get(ILogger));
    bootstrapper.registerServices(builder);

    return bootstrapper.getEntry();
}

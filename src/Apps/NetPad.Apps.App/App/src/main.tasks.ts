import {IAurelia, IContainer, ILogger} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {IBackgroundService} from "@common";
import {Env, IWindowBootstrapperConstructor} from "@application";

export const configureFetchClient = (container: IContainer) => {
    const client = container.get(IHttpClient);
    const logger = container.get(ILogger).scopeTo("http-client");

    const shouldLogErrors = (error: Error | undefined) => !error || !error.name.startsWith("AbortError");

    client.configure(config =>
        config
            .useStandardConfiguration()
            .withInterceptor({
                requestError(error: unknown): Request | Response | Promise<Request | Response> {
                    if (shouldLogErrors(error as Error)) logger.error(error);
                    throw error;
                },
                responseError(error: unknown, request?: Request): Response | Promise<Response> {
                    if (shouldLogErrors(error as Error)) logger.error(error);
                    throw error;
                }
            })
    );
}

export const startBackgroundServices = async (container: IContainer) => {
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
}

export const configureAppEntryPoint = (app: IAurelia) => {
    const startupOptions = app.container.get(URLSearchParams);

    // Determine which window we need to bootstrap and use
    let windowName = startupOptions.get("win");
    if (!windowName && !Env.isRunningInElectron())
        windowName = "main";

    let bootstrapperCtor: IWindowBootstrapperConstructor;

    /* eslint-disable @typescript-eslint/no-var-requires */
    if (windowName === "main")
        bootstrapperCtor = require("./windows/main/main").Bootstrapper;
    else if (windowName === "settings")
        bootstrapperCtor = require("./windows/settings/main").Bootstrapper;
    else if (windowName === "script-config")
        bootstrapperCtor = require("./windows/script-config/main").Bootstrapper;
    else if (windowName === "data-connection")
        bootstrapperCtor = require("./windows/data-connection/main").Bootstrapper;
    else
        throw new Error(`Unrecognized window: ${windowName}`);
    /* eslint-enable @typescript-eslint/no-var-requires */

    const bootstrapper = new bootstrapperCtor(app.container.get(ILogger));
    bootstrapper.registerServices(app);

    return bootstrapper.getEntry();
}

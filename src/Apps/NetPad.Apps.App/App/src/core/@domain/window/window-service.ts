import {DI, IHttpClient} from "aurelia";
import {WindowApiClient, IWindowApiClient} from "@domain";

export interface IWindowService extends IWindowApiClient {
    openDeveloperTools(windowId?: string, signal?: AbortSignal | undefined): Promise<void>;
}

export const IWindowService = DI.createInterface<IWindowService>();

export class WindowService extends WindowApiClient implements IWindowService {
    constructor(
        private readonly startupOptions: URLSearchParams,
        baseUrl?: string,
        @IHttpClient http?: IHttpClient
    ) {
        super(baseUrl, http);
    }

    public override openDeveloperTools(windowId?: string, signal?: AbortSignal | undefined): Promise<void> {
        return super.openDeveloperTools(windowId ?? this.startupOptions.get("winId") ?? "", signal);
    }
}

import {DI} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {ISettingsApiClient, Settings, SettingsApiClient} from "@application";

export interface ISettingsService extends ISettingsApiClient {
    toggleTheme(): Promise<void>;
}

export const ISettingsService = DI.createInterface<ISettingsService>();

export class SettingsService extends SettingsApiClient implements ISettingsService {
    constructor(readonly settings: Settings,
                baseUrl: string,
                @IHttpClient http: IHttpClient) {
        super(baseUrl, http);
    }

    public async toggleTheme(): Promise<void> {
        const clone = this.settings.clone();
        clone.appearance.theme = clone.appearance.theme === "Light" ? "Dark" : "Light";
        await this.update(clone);
    }
}

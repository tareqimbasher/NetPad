import {DI, IHttpClient} from "aurelia";
import {ISettingsApiClient, Settings, SettingsApiClient} from "@domain";

export interface ISettingService extends ISettingsApiClient {
    toggleTheme(): Promise<void>;
}

export const ISettingService = DI.createInterface<ISettingService>();

export class SettingService extends SettingsApiClient implements ISettingService {
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

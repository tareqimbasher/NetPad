import {IHttpClient} from "@aurelia/fetch-client";
import {ISettingsService, Settings, SettingsApiClient} from "@application";
import {AppTheme} from "@application/themes/app-theme";

export class SettingsService extends SettingsApiClient implements ISettingsService {
    constructor(readonly settings: Settings,
                baseUrl: string,
                @IHttpClient http: IHttpClient) {
        super(baseUrl, http);
    }

    public async toggleTheme(): Promise<void> {
        const clone = this.settings.clone();
        clone.appearance.mode = AppTheme.resolveGround(clone.appearance.mode) === "light" ? "Dark" : "Light";
        await this.update(clone);
    }
}

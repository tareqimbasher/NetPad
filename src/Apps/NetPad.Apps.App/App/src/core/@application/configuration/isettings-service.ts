import {DI} from "aurelia";
import {ISettingsApiClient} from "@application";

export interface ISettingsService extends ISettingsApiClient {
    toggleTheme(): Promise<void>;
}

export const ISettingsService = DI.createInterface<ISettingsService>();

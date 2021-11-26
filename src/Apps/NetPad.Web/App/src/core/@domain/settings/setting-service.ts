import {DI} from "aurelia";
import {ISettingsApiClient, SettingsApiClient} from "@domain";

export interface ISettingService extends ISettingsApiClient {}

export const ISettingService = DI.createInterface<ISettingService>();

export class SettingService extends SettingsApiClient implements ISettingService {
}

import {DI} from "aurelia";
import {ISettingsService, SettingsService} from "@domain";

export interface ISettingsManager extends ISettingsService {}

export const ISettingsManager = DI.createInterface<ISettingsManager>();

export class SettingsManager extends SettingsService implements ISettingsManager {
}

import {DI} from "aurelia";
import {WindowApiClient, IWindowApiClient} from "@domain";

export interface IWindowService extends IWindowApiClient {}

export const IWindowService = DI.createInterface<IWindowService>();

export class WindowService extends WindowApiClient implements IWindowService {
}

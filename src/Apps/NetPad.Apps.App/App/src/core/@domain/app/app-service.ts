import {DI} from "aurelia";
import {AppApiClient, IAppApiClient} from "@domain";

export interface IAppService extends IAppApiClient {}

export const IAppService = DI.createInterface<IAppService>();

export class AppService extends AppApiClient implements IAppService {
}

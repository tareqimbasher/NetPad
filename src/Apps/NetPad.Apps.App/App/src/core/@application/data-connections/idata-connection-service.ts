import {DI} from "aurelia";
import {IDataConnectionsApiClient} from "@application";

export interface IDataConnectionService extends IDataConnectionsApiClient {}

export const IDataConnectionService = DI.createInterface<IDataConnectionService>();

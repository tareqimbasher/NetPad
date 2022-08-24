import {DI} from "aurelia";
import {DataConnectionsApiClient, IDataConnectionsApiClient} from "@domain";

export interface IDataConnectionService extends IDataConnectionsApiClient {}

export const IDataConnectionService = DI.createInterface<IDataConnectionService>();

export class DataConnectionService extends DataConnectionsApiClient implements IDataConnectionService {}

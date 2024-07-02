import {DI} from "aurelia";
import {AssembliesApiClient, IAssembliesApiClient} from "@application";

export interface IAssemblyService extends IAssembliesApiClient {}

export const IAssemblyService = DI.createInterface<IAssemblyService>();

export class AssemblyService extends AssembliesApiClient implements IAssemblyService {}

import {DI} from "aurelia";
import {IAssembliesApiClient} from "@application";

export interface IAssemblyService extends IAssembliesApiClient {}

export const IAssemblyService = DI.createInterface<IAssemblyService>();

import {IPackagesApiClient} from "@application";
import {DI} from "aurelia";

export interface IPackageService extends IPackagesApiClient {}

export const IPackageService = DI.createInterface<IPackageService>();

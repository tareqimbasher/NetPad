import {IPackagesApiClient} from "@application";
import {DI} from "aurelia";

export const IPackageService = DI.createInterface<IPackageService>();

export interface IPackageService extends IPackagesApiClient {}

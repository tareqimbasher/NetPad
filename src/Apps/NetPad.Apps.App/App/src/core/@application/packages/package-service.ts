import {IPackagesApiClient, PackagesApiClient} from "@application";
import {DI} from "aurelia";

export interface IPackageService extends IPackagesApiClient {}

export const IPackageService = DI.createInterface<IPackageService>();

export class PackageService extends PackagesApiClient implements IPackageService {}

import { observable } from '@aurelia/runtime';
import {IPackageService, PackageMetadata} from "@domain";
import {System} from "@common";

export class PackageManagement {
    @observable public term: string;
    public searchResults: PackageMetadata[] = [];

    constructor(@IPackageService readonly packageService: IPackageService) {
    }

    public async termChanged(newValue: string, oldValue: string) {
        if (newValue === oldValue) return;

        this.searchResults = await this.packageService.search(
            this.term,
            0,
            10,
            false);
    }
}

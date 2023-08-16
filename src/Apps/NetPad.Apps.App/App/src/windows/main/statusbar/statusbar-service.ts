import {DI} from "aurelia";
import {IStatusbarItem} from "./istatusbar-item";

export const IStatusbarService = DI.createInterface<IStatusbarService>();

export interface IStatusbarService {
    items: ReadonlyArray<IStatusbarItem>;
    addItem(item: IStatusbarItem): void;
    removeItem(item: IStatusbarItem): void;
}

export class StatusbarService implements IStatusbarService {
    private _items: IStatusbarItem[] = [];

    public get items(): ReadonlyArray<IStatusbarItem> {
        return this._items;
    }

    public addItem(item: IStatusbarItem) {
        this._items.push(item);
    }

    public removeItem(item: IStatusbarItem) {
        const ix = this._items.indexOf(item);
        if (ix >= 0) this._items.splice(ix, 1);
    }
}

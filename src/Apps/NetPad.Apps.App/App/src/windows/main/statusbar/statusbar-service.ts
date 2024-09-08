import {Constructable, DI} from "aurelia";
import {IStatusbarItem, IStatusbarItemNew} from "./istatusbar-item";

export const IStatusbarService = DI.createInterface<IStatusbarService>();

export interface IStatusbarService {
    items: ReadonlyArray<IStatusbarItem>;
    addItem(item: IStatusbarItem): void;
    removeItem(item: IStatusbarItem): void;

    itemsNew: ReadonlyArray<Constructable<IStatusbarItemNew>>;
    addItemNew(item: Constructable<IStatusbarItemNew>): void;
    removeItemNew(item: Constructable<IStatusbarItemNew>): void;
}

export class StatusbarService implements IStatusbarService {
    private _items: IStatusbarItem[] = [];
    private _itemsNew: Constructable<IStatusbarItemNew>[] = [];

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




    public get itemsNew(): ReadonlyArray<Constructable<IStatusbarItemNew>> {
        return this._itemsNew;
    }

    public addItemNew(item: Constructable<IStatusbarItemNew>) {
        this._itemsNew.push(item);
    }

    public removeItemNew(item: Constructable<IStatusbarItemNew>) {
        const ix = this._itemsNew.indexOf(item);
        if (ix >= 0) this._itemsNew.splice(ix, 1);
    }
}

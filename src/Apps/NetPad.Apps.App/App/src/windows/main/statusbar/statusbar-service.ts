import {Constructable, DI, ILogger, resolve} from "aurelia";

export interface IStatusbarItem {
    component: Constructable,
    position: "left" | "right";
}

export const IStatusbarService = DI.createInterface<IStatusbarService>();

export interface IStatusbarService {
    items: ReadonlyArray<IStatusbarItem>;
    addItem(component: Constructable, position: "left" | "right"): void;
    removeItem(component: Constructable): void;
}

export class StatusbarService implements IStatusbarService {
    private _items: IStatusbarItem[] = [];
    private logger = resolve(ILogger).scopeTo(nameof(StatusbarService));

    public get items(): ReadonlyArray<IStatusbarItem> {
        return this._items;
    }

    public addItem(component: Constructable, position: "left" | "right") {
        this._items.push({
            component: component,
            position: position,
        });
    }

    public removeItem(component: Constructable) {
        const ix = this._items.findIndex(x => x.component === component);
        if (ix >= 0) {
            this._items.splice(ix, 1);
        }
        else {
            this.logger.warn("Could not remove item. Item not found: ", component);
        }
    }
}

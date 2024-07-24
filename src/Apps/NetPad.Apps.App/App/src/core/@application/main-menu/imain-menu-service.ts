import {DI} from "aurelia";
import {IMenuItem} from "./imenu-item";

export interface IMainMenuService {
    items: ReadonlyArray<IMenuItem>;

    addItem(item: IMenuItem): void;

    removeItem(item: IMenuItem): void;

    clickMenuItem(item: IMenuItem): Promise<void>;

    clickMenuItem(id: string): Promise<void>;
}

export const IMainMenuService = DI.createInterface<IMainMenuService>();

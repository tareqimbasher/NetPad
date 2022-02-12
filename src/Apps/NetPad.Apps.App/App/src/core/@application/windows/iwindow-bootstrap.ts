import {Aurelia} from "aurelia";

export interface IWindowBootstrap {
    registerServices(app: Aurelia): void;
    getEntry(): any;
}

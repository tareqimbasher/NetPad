import {Aurelia} from "aurelia";
import {Index} from "./index";
import {IWindowBootstrap} from "@application";

export class Bootstrapper implements IWindowBootstrap {
    getEntry = () => Index;

    registerServices(app: Aurelia): void {
    }
}

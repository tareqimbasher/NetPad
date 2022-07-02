import {Aurelia} from "aurelia";
import {Window} from "./window";
import {IWindowBootstrap} from "@application";

export class Bootstrapper implements IWindowBootstrap {
    getEntry = () => Window;

    registerServices(app: Aurelia): void {
    }
}

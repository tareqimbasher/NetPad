import {Aurelia} from "aurelia";
import {Index} from "./index";

export function register(app: Aurelia): void {
    app.app(Index);
}

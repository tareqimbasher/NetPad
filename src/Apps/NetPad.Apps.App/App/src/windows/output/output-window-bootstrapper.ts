import {
    IPaneManager,
    IWindowBootstrapper,
    PaneHost,
    PaneManager
} from "@application";
import {Window} from "./window";
import {Aurelia, Registration} from "aurelia";
import {PaneToolbar} from "@application/panes/pane-toolbar";

export class OutputWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            PaneHost,
            PaneToolbar,
        );
    }
}

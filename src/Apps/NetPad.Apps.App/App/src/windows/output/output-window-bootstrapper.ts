import {
    IPaneManager,
    IShortcutManager,
    IWindowBootstrapper,
    PaneHost,
    PaneManager,
    ShortcutManager
} from "@application";
import {Window} from "./window";
import {Aurelia, Registration} from "aurelia";
import {ExcelService, IExcelService} from "@application/data/excel-service";
import {PaneToolbar} from "@application/panes/pane-toolbar";

export class OutputWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IExcelService, ExcelService),
            PaneHost,
            PaneToolbar,
        );
    }
}

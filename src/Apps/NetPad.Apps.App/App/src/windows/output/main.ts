import {IWindowBootstrapper} from "@application";
import {Window} from "./window";
import {Aurelia, Registration} from "aurelia";
import {ExcelService, IExcelService} from "@application/data/excel-service";

export class Bootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IExcelService, ExcelService)
        );
    }
}

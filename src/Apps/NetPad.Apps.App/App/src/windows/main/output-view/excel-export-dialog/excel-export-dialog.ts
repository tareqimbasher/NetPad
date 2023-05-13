import {DialogBase} from "@application/dialogs/dialog-base";
import {IDialogDom} from "@aurelia/dialog";
import {ILogger} from "aurelia";
import {IExcelExportOptions} from "@application/data/excel-service";

interface IColorOption {
    text?: string,
    color?: string,
    isSelector?: boolean
}

interface IColorOptions {
    selected?: IColorOption,
    options: IColorOption[]
}

export class ExcelExportDialog extends DialogBase {
    public showAdvancedOptions = false
    public options: IExcelExportOptions = {
        includeCode: true,
        sheetPerOutputItem: true,
        includeNonTabularData: true,
    };

    public headerBackgroundOptions: IColorOptions = {
        selected: undefined,
        options: [
            {text: "None", color: undefined},
            {isSelector: true, color: "#3da3da"},
        ]
    }

    public headerForegroundOptions: IColorOptions = {
        selected: undefined,
        options: [
            {text: "None", color: undefined},
            {isSelector: true, color: "#ffffff"},
        ]
    }

    constructor(@IDialogDom dialogDom: IDialogDom,
                @ILogger logger: ILogger) {
        super(dialogDom, logger);

        this.headerBackgroundOptions.selected = this.headerBackgroundOptions.options[1];
        this.headerForegroundOptions.selected = this.headerForegroundOptions.options[1];
    }

    public async export() {
        if (this.headerBackgroundOptions.selected?.color)
            this.options.headerBackgroundColor = this.headerBackgroundOptions.selected.color;

        if (this.headerForegroundOptions.selected?.color)
            this.options.headerForegroundColor = this.headerForegroundOptions.selected.color;

        await this.ok(this.options);
    }
}

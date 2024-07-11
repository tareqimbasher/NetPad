import {watch} from "@aurelia/runtime-html";
import {KeyCode, System, Util} from "@common";
import {ChannelInfo, IIpcGateway, ScriptStatus} from "@application";
import {ExcelExportDialog} from "../excel-export/excel-export-dialog";
import {ExcelService, IExcelExportOptions} from "../excel-export/excel-service";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {OutputViewBase} from "../output-view-base";

export class ResultsView extends OutputViewBase {
    private txtUserInput: HTMLInputElement;

    constructor(@IIpcGateway private readonly ipcGateway: IIpcGateway,
                private readonly dialogUtil: DialogUtil,
    ) {
        super();
    }

    public override attached() {
        super.attached();

        const userInputKeyHandler = async (ev: KeyboardEvent) => {
            if (ev.code !== KeyCode.Enter) {
                return;
            }

            const inputRequest = this.model.inputRequest;
            if (!inputRequest) {
                return;
            }

            await this.ipcGateway.send(
                new ChannelInfo("Respond"),
                inputRequest.commandId,
                inputRequest.userInput);

            this.model.inputRequest = null;
        };

        this.txtUserInput.addEventListener("keydown", userInputKeyHandler);
        this.addDisposable(() => this.txtUserInput.removeEventListener("keydown", userInputKeyHandler));
    }

    @watch<ResultsView>(vm => vm.model.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        this.model.inputRequest = null;

        if (oldStatus !== "Running" && newStatus === "Running") {
            this.model.resultsDumpContainer.clearOutput(true);
        }
    }

    private async exportOutputToExcel() {
        if (!this.model) {
            return;
        }

        const dumpContainer = this.model.resultsDumpContainer;

        const groups = Array.from(dumpContainer.element.querySelectorAll(".group"));

        if (!groups.length) {
            alert("There is no output to export.");
            return;
        }

        const result = await this.dialogUtil.toggle(ExcelExportDialog);
        if (result.status !== "ok") return;
        const exportOptions = result.value as IExcelExportOptions;

        const elementsToExport: Element[] = [];
        for (const group of Array.from(dumpContainer.element.querySelectorAll(".group"))) {
            const table = group.querySelector(":scope > table");
            if (table) {
                elementsToExport.push(table);
                continue;
            }

            if (!exportOptions.includeNonTabularData) continue;

            const title = group.querySelector(":scope > .title");
            if (title && group.childElementCount > 1 && group.lastElementChild) {
                elementsToExport.push(group.lastElementChild)
            } else if (!title) {
                elementsToExport.push(group);
            }
        }

        const workbook = ExcelService.export(elementsToExport, exportOptions);

        if (exportOptions.includeCode) {
            const worksheet = workbook.addWorksheet("Code");
            worksheet.getRow(1).height = 2000;
            worksheet.getColumn(1).width = 300;
            const firstCell = worksheet.getCell(1, 1);
            firstCell.alignment = {vertical: "top", horizontal: "left"};
            firstCell.value = this.model.environment.script.code;
        }

        const buffer = (await workbook.xlsx.writeBuffer()) as Buffer;
        System.downloadFile(
            `${this.model.environment.script.name}_${Util.dateToFormattedString(new Date(), "yyyy-MM-dd_HH-mm-ss")}.xlsx`,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            buffer.toString('base64')
        );
    }

    private async exportOutputToHtml() {
        if (!this.model) {
            return;
        }

        const name = `${this.model.environment.script.name}_${Util.dateToFormattedString(new Date(), "yyyy-MM-dd_HH-mm-ss")}`

        const metas = [...new Set<string>(
            Array.from(document.head.querySelectorAll("meta"))
                .map(s => s.outerHTML)
        )].join("\n");

        const styles = [...new Set(
            Array.from(document.head.querySelectorAll("style"))
                .map(s => s.outerHTML)
                .filter(x => x.indexOf("output-pane") >= 0)
        )].join("\n");

        const bodyContents = document.createRange().createContextualFragment(this.dumpContainerWrapper.outerHTML);
        bodyContents.querySelectorAll("i[class*=icon]").forEach(x => x.remove());

        const html = `<!DOCTYPE html>
<html lang="en">
<head>
<title>${name}</title>
${metas}
${styles}
</head>
<body>
<output-pane>${bodyContents.firstElementChild?.outerHTML}</output-pane>
</body></html>`;

        System.downloadTextAsFile(`${name}.html`, "text/html", html);
    }
}

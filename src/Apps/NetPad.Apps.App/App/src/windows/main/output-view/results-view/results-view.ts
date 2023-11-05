import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {DialogDeactivationStatuses} from "@aurelia/dialog";
import {ResultsPaneViewSettings} from "./results-view-settings";
import {
    ChannelInfo,
    HtmlScriptOutput,
    IEventBus,
    IIpcGateway,
    ISession,
    PromptUserForInputCommand,
    ScriptOutputEmittedEvent,
    ScriptStatus,
    Settings
} from "@domain";
import {ResultControls} from "./result-controls";
import {OutputViewBase} from "../output-view-base";
import {IExcelExportOptions, IExcelService} from "@application/data/excel-service";
import {KeyCode, System, Util} from "@common";
import {ExcelExportDialog} from "../excel-export-dialog/excel-export-dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class ResultsView extends OutputViewBase {
    public resultsViewSettings: ResultsPaneViewSettings;
    private resultControls: ResultControls;

    private userInputVisible = false;
    private txtUserInput: HTMLInputElement;
    private userInputKeyHandler?: (ev: KeyboardEvent) => void;

    constructor(private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @IExcelService private readonly excelService: IExcelService,
                private readonly dialogUtil: DialogUtil,
                @IEventBus private readonly eventBus: IEventBus,
                @IIpcGateway private readonly ipcGateway: IIpcGateway,
                @ILogger logger: ILogger
    ) {
        super(logger);
        this.resultsViewSettings = new ResultsPaneViewSettings(this.settings.results.textWrap);

        const rvs = this.resultsViewSettings;
        const self = this;

        this.toolbarActions = [
            {
                label: "Export",
                actions: [
                    {
                        icon: "excel-file-icon text-green",
                        label: "Export to Excel",
                        clicked: async () => await this.exportOutputToExcel()
                    },
                    {
                        icon: "html-icon text-orange",
                        label: "Export to HTML",
                        clicked: async () => await this.exportOutputToHtml()
                    }
                ]
            },
            {
                label: "Format",
                actions: [
                    {
                        label: "Collapse to Level 1",
                        clicked: async () => this.resultControls.collapseAll(1)
                    },
                    {
                        label: "Collapse to Level 2",
                        clicked: async () => this.resultControls.collapseAll(2)
                    },
                    {
                        label: "Collapse to Level 3",
                        clicked: async () => this.resultControls.collapseAll(3)
                    },
                    {
                        label: "Expand to Level 1",
                        clicked: async () => this.resultControls.expandAll(1)
                    },
                    {
                        label: "Expand to Level 2",
                        clicked: async () => this.resultControls.expandAll(2)
                    },
                    {
                        label: "Expand to Level 3",
                        clicked: async () => this.resultControls.expandAll(3)
                    }
                ]
            },
            {
                label: "Collapse All",
                icon: "tree-collapse-all-icon",
                clicked: async () => this.resultControls.collapseAll()
            },
            {
                label: "Expand All",
                icon: "tree-expand-all-icon",
                clicked: async () => this.resultControls.expandAll()
            },
            {
                label: "Text Wrap",
                icon: "text-wrap-icon",
                active: this.resultsViewSettings.textWrap,
                clicked: async function () {
                    rvs.textWrap = !rvs.textWrap;
                    this.active = rvs.textWrap;
                },
            },
            {
                label: "Scroll on Output",
                icon: "scroll-on-output-icon",
                active: this.scrollOnOutput,
                clicked: async function () {
                    self.scrollOnOutput = !self.scrollOnOutput;
                    this.active = self.scrollOnOutput;
                },
            },
            {
                label: "Clear",
                icon: "clear-output-icon",
                clicked: async () => this.clearOutput(),
            },
        ];
    }

    public attached() {
        this.resultControls = new ResultControls(this.outputElement);
        this.addDisposable(() => this.resultControls.dispose());

        this.addDisposable(
            this.eventBus.subscribeToServer(ScriptOutputEmittedEvent, msg => {
                if (msg.scriptId !== this.environment.script.id || !msg.output)
                    return;

                const output = JSON.parse(msg.output) as HtmlScriptOutput;
                this.appendOutput(output);
            })
        );

        this.addDisposable(
            this.eventBus.subscribeToServer(PromptUserForInputCommand, cmd => {
                if (cmd.scriptId !== this.environment.script.id)
                    return;

                this.showUserInput(cmd.id);
            })
        );

        this.addDisposable(() => this.stopUserInput());
    }

    protected override beforeAppendOutputHtml(documentFragment: DocumentFragment) {
        super.beforeAppendOutputHtml(documentFragment);
        this.resultControls.bind(documentFragment);
    }

    protected override beforeClearOutput() {
        super.beforeClearOutput();
        this.resultControls.dispose();
    }

    @watch<ResultsView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearOutput(true);

        if (newStatus !== "Running") {
            this.stopUserInput();
        }
    }

    private showUserInput(commandId: string) {
        this.userInputKeyHandler = async (ev: KeyboardEvent) => {
            if (ev.code === KeyCode.Enter) {
                await this.ipcGateway.send(new ChannelInfo("Respond"), commandId, this.txtUserInput.value);
                this.stopUserInput();
            }
        };

        this.txtUserInput.value = "";
        this.txtUserInput.addEventListener("keydown", this.userInputKeyHandler);
        this.userInputVisible = true;
        setTimeout(() => this.txtUserInput.focus(), 100);
    }

    private stopUserInput() {
        this.userInputVisible = false;
        this.txtUserInput.value = "";

        if (this.userInputKeyHandler) {
            this.txtUserInput.removeEventListener("keydown", this.userInputKeyHandler);
            this.userInputKeyHandler = undefined;
        }
    }

    private async exportOutputToExcel() {
        const groups = Array.from(this.outputElement.querySelectorAll(".group"));

        if (!groups.length) {
            alert("There is no output to export.");
            return;
        }

        const result = await this.dialogUtil.toggle(ExcelExportDialog);
        if (result.status !== DialogDeactivationStatuses.Ok) return;
        const exportOptions = result.value as IExcelExportOptions;

        const elementsToExport: Element[] = [];
        for (const group of Array.from(this.outputElement.querySelectorAll(".group"))) {
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

        const workbook = this.excelService.export(elementsToExport, exportOptions);

        if (exportOptions.includeCode) {
            const worksheet = workbook.addWorksheet("Code");
            worksheet.getRow(1).height = 2000;
            worksheet.getColumn(1).width = 300;
            const firstCell = worksheet.getCell(1, 1);
            firstCell.alignment = {vertical: "top", horizontal: "left"};
            firstCell.value = this.environment.script.code;
        }

        const buffer = (await workbook.xlsx.writeBuffer()) as Buffer;
        System.downloadFile(
            `${this.environment.script.name}_${Util.dateToString(new Date(), "yyyy-MM-dd_HH-mm-ss")}.xlsx`,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            buffer.toString('base64')
        );
    }

    private async exportOutputToHtml() {
        const name = `${this.environment.script.name}_${Util.dateToString(new Date(), "yyyy-MM-dd_HH-mm-ss")}`

        const metas = [...new Set<string>(
            Array.from(document.head.querySelectorAll("meta"))
                .map(s => s.outerHTML)
        )].join("\n");

        const styles = [...new Set(
            Array.from(document.head.querySelectorAll("style"))
                .map(s => s.outerHTML)
                .filter(x => x.indexOf("output-view") >= 0)
        )].join("\n");

        const bodyContents = document.createRange().createContextualFragment(this.outputElement.outerHTML);
        bodyContents.querySelectorAll("i[class*=icon]").forEach(x => x.remove());

        const html = `<!DOCTYPE html>
<html>
<head>
<title>${name}</title>
${metas}
${styles}
</head>
<body>
<output-view>${bodyContents.firstElementChild?.outerHTML}</output-view>
</body></html>`;

        System.downloadTextAsFile(`${name}.html`, "text/html", html);
    }
}

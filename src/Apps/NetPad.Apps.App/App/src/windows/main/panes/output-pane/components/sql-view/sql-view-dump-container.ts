import {DumpContainer} from "../dump-container";
import {Settings} from "@application";
import {Colorizer} from "./colorizer";

interface ILoggerInfo {
    loggerName: string;
    count: number;
    active?: boolean;
}

export class SqlViewDumpContainer extends DumpContainer {
    private colorize = true;

    private enabledLoggers: Set<string> = new Set<string>();
    private loggers = new Map<string, ILoggerInfo>([
        ["Default", {loggerName: "", count: 0}],
        ["All", {loggerName: "", count: 0}],
        ["Command", {loggerName: "Microsoft.EntityFrameworkCore.Database.Command", count: 0}],
        ["Connection", {loggerName: "Microsoft.EntityFrameworkCore.Database.Connection", count: 0}],
        ["Query", {loggerName: "Microsoft.EntityFrameworkCore.Query", count: 0}],
        ["ChangeTracking", {loggerName: "Microsoft.EntityFrameworkCore.ChangeTracking", count: 0}],
        ["Model", {loggerName: "Microsoft.EntityFrameworkCore.Model", count: 0}],
        ["Infrastructure", {loggerName: "Microsoft.EntityFrameworkCore.Infrastructure", count: 0}],
    ]);

    constructor(settings: Settings) {
        super(settings);

        // Set default log option
        this.toggleLogger("Default");
    }


    public get logLength(): number {
        return this.loggers.get("All")?.count ?? 0;
    }

    protected override beforeClearOutput() {
        for (const [_, logger] of this.loggers) {
            logger.count = 0;
        }
    }

    protected override mutateHtmlBeforeAppend(html: string): string {
        if (this.colorize) {
            return Colorizer.colorize(html);
        }

        return html;
    }

    protected override beforeAppendHtml(documentFragment: DocumentFragment) {
        super.beforeAppendHtml(documentFragment);

        const groups = Array.from(documentFragment.querySelectorAll(".group.text")) as HTMLElement[];

        this.updateLoggerCounts(groups);

        if (this.colorize) {
            for (const group of groups) {
                const childNodes = Array.from(group.childNodes);
                for (let iChildNode = 0; iChildNode < childNodes.length; iChildNode++) {
                    const childNode = childNodes[iChildNode];

                    if (childNode.nodeType !== Node.TEXT_NODE) {
                        continue;
                    }

                    const span = document.createElement("span");
                    span.classList.add("not-special");
                    span.innerHTML = (childNode as Text).data;

                    childNode.replaceWith(span);
                }
            }
        }
    }

    private updateLoggerCounts(groups: HTMLElement[]) {
        // Update the overall log count
        this.loggers.get("All")!.count += groups.length;

        // Update the individual log counts
        for (const group of groups) {
            const text = group.textContent;
            if (!text) continue;

            if (text.indexOf("Executing DbCommand") >= 0
                && text.indexOf("(Microsoft.EntityFrameworkCore.Database.Command)") >= 0) {
                group.classList.add("log-by-default");
                this.loggers.get("Default")!.count += groups.length;
            }

            this.loggers.forEach((logger, label) => {
                if (!logger.loggerName) {
                    return;
                }

                if (text.indexOf(logger.loggerName) >= 0) {
                    group.classList.add("log-by-" + label.toLowerCase());
                    logger.count++;
                }
            });
        }
    }

    private toggleLogger(label: string) {
        if (label === "Default") {
            this.enabledLoggers.clear();

            this.loggers.forEach(logger => logger.active = false);
            this.loggers.get("Default")!.active = true;

            this.element.classList.add("show-logs-by-default");
        } else {
            this.loggers.get("Default")!.active = false;
            this.element.classList.remove("show-logs-by-default");

            if (label === "All") {
                this.enabledLoggers.clear();

                [...this.loggers.keys()].slice(2).forEach(key => {
                    this.loggers.get(key)!.active = true;
                    this.enabledLoggers.add(key);
                });
            } else {
                if (this.enabledLoggers.has(label)) {
                    this.enabledLoggers.delete(label);
                } else {
                    this.enabledLoggers.add(label);
                }

                this.loggers.get(label)!.active = !this.loggers.get(label)!.active;
            }
        }

        this.loggers.forEach((logger, label) => {
            if (!logger.loggerName) {
                return;
            }

            const cl = "show-logs-by-" + label.toLowerCase();

            if (this.enabledLoggers.has(label)) {
                this.element.classList.add(cl);
            } else {
                this.element.classList.remove(cl);
            }
        });
    }
}

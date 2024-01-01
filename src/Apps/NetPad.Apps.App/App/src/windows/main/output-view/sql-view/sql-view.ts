import {HtmlSqlScriptOutput, IEventBus, ScriptOutputEmittedEvent, ScriptStatus} from "@domain";
import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {OutputViewBase} from "../output-view-base";
import {FindTextBoxOptions} from "@application";
import {Colorizer} from "./colorizer";

export class SqlView extends OutputViewBase {
    private textWrap: boolean;
    private colorize: boolean;
    private logLength = 0;

    private enabledLoggers: Set<string> = new Set<string>();
    private loggers = new Map<string, string>([
        // [label, loggerName]
        ["Command", "Microsoft.EntityFrameworkCore.Database.Command"],
        ["Connection", "Microsoft.EntityFrameworkCore.Database.Connection"],
        ["Query", "Microsoft.EntityFrameworkCore.Query"],
        ["ChangeTracking", "Microsoft.EntityFrameworkCore.ChangeTracking"],
        ["Model", "Microsoft.EntityFrameworkCore.Model"],
        ["Infrastructure", "Microsoft.EntityFrameworkCore.Infrastructure"],
    ]);

    constructor(@IEventBus private readonly eventBus: IEventBus, @ILogger logger: ILogger) {
        super(logger);
        this.colorize = true;

        const self = this;
        this.toolbarActions = [
            {
                title: "Show/Hide EF Core logs.",
                label: "Log",
                actions: [
                    {
                        get label() {
                            return `Default <span class="float-end ms-5 badge">${this["count"]}</span>`;
                        },
                        title: 'Show "Executing Command" logs only.',
                        count: 0,
                        active: self.enabledLoggers.size === 0,
                        get icon() {
                            return this.active ? "check-icon" : "invisible check-icon";
                        },
                        clicked: async function () {
                            self.toggleLogger(null);
                        }
                    },
                    {
                        isDivider: true
                    },
                    {
                        label: "All",
                        title: "Show all log types.",
                        icon: "invisible check-icon",
                        clicked: async function () {
                            self.loggers.forEach((val, key) => {
                                if (!self.enabledLoggers.has(key)) {
                                    self.toggleLogger(key);
                                }
                            });
                        }
                    },
                    ...Array.from(self.loggers.entries()).map(x => {
                        return {
                            get label() {
                                return `${x[0]} <span class="float-end ms-5 badge">${this["count"]}</span>`;
                            },
                            title: `Show ${x[0]} logs.`,
                            count: 0,
                            active: self.enabledLoggers.has(x[0]),
                            get icon() {
                                return this.active ? "check-icon" : "invisible check-icon";
                            },
                            clicked: async function () {
                                self.toggleLogger(x[0]);
                            }
                        }
                    })
                ]
            },
            {
                label: "Color output - turn off for better 'Find Text' (CTRL + F) accuracy",
                get icon() {
                    return this.active ? "theme-icon text-orange" : "theme-icon";
                },
                active: this.colorize,
                clicked: async function () {
                    self.colorize = !self.colorize;
                    this.active = self.colorize;
                },
            },
            {
                label: "Text Wrap",
                icon: "text-wrap-icon",
                active: this.textWrap,
                clicked: async function () {
                    self.textWrap = !self.textWrap;
                    this.active = self.textWrap;
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

    public override bound() {
        super.bound();

        this.findTextBoxOptions = new FindTextBoxOptions(
            this.outputElement,
            ".text, .sql-keyword, .query-time, .query-params, .logger-name, .not-special");
    }

    public attached() {
        // Set default log option
        this.toggleLogger(null);

        this.addDisposable(
            this.eventBus.subscribeToServer(ScriptOutputEmittedEvent, msg => {
                if (msg.scriptId !== this.environment.script.id || !msg.output)
                    return;

                if (msg.outputType !== nameof(HtmlSqlScriptOutput)) {
                    return;
                }

                const output = msg.output as HtmlSqlScriptOutput;

                if (output.body && this.colorize) {
                    output.body = Colorizer.colorize(output.body);
                }

                this.appendOutput(output);
            })
        );
    }

    @watch<SqlView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearOutput(true);
    }

    protected override beforeClearOutput() {
        this.logLength = 0;
        this.toolbarActions[0].label = "Log";
        this.toolbarActions[0].actions!.forEach(a => a["count"] = 0);
    }

    protected override beforeAppendOutputHtml(documentFragment: DocumentFragment) {
        super.beforeAppendOutputHtml(documentFragment);

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
        this.logLength += groups.length;
        this.toolbarActions[0].label = `Log (${this.logLength})`;

        // Update the individual log counts
        const logActions = this.toolbarActions[0].actions!;
        for (const group of groups) {
            const text = group.textContent;
            if (!text) continue;

            if (text.indexOf("Executing DbCommand") >= 0
                && text.indexOf("(Microsoft.EntityFrameworkCore.Database.Command)") >= 0) {
                group.classList.add("log-by-default");
                (logActions[0]["count"] as number) += 1;
            }

            this.loggers.forEach((logger, label) => {
                if (text.indexOf(logger) >= 0) {
                    group.classList.add("log-by-" + label.toLowerCase());

                    const action = logActions.find(a => a.label?.startsWith(label));
                    if (action) {
                        (action["count"] as number) += 1;
                    }
                }
            });
        }
    }

    private toggleLogger(label: string | null) {
        const logActions = this.toolbarActions[0].actions!;

        if (label) {
            if (this.enabledLoggers.has(label)) {
                this.enabledLoggers.delete(label);
            } else {
                this.enabledLoggers.add(label);
            }
        }

        if (!label || this.enabledLoggers.size === 0) {
            this.enabledLoggers.clear();
            logActions[0].active = true;
            logActions.slice(2).forEach(x => x.active = false);
        } else {
            logActions[0].active = false;
            logActions.slice(2).forEach(a => a.active = this.enabledLoggers.has(a.label!.split(' ')[0]));
        }

        this.loggers.forEach((logger, label) => {
            const cl = "show-logs-by-" + label.toLowerCase();

            if (this.enabledLoggers.has(label)) {
                this.outputElement.classList.add(cl);
            } else {
                this.outputElement.classList.remove(cl);
            }
        });

        if (this.enabledLoggers.size === 0) {
            this.outputElement.classList.add("show-logs-by-default");
        } else {
            this.outputElement.classList.remove("show-logs-by-default");
        }
    }
}


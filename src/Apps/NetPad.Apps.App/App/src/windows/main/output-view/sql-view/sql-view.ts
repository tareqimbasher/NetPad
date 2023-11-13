import {HtmlSqlScriptOutput, IEventBus, ScriptOutputEmittedEvent, ScriptStatus} from "@domain";
import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {OutputViewBase} from "../output-view-base";
import {FindTextBoxOptions} from "@application";

export class SqlView extends OutputViewBase {
    private textWrap: boolean;
    private colorize: boolean;

    constructor(@IEventBus private readonly eventBus: IEventBus, @ILogger logger: ILogger) {
        super(logger);
        this.colorize = true;

        const self = this;
        this.toolbarActions = [
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
                clicked: async function (){
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
            ".text, .sql-keyword, .query-time, .query-params, .not-special");
    }

    public attached() {
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

    protected override beforeAppendOutputHtml(documentFragment: DocumentFragment) {
        super.beforeAppendOutputHtml(documentFragment);

        if (this.colorize) {
            const groups = Array.from(documentFragment.querySelectorAll(".group.text")) as HTMLElement[];

            for (const group of groups) {
                // Since we know each group is just text for SQL View, we want to add a new line at the end
                // of each one so when base class combines this text with the previous displayed text it is
                // separated by an extra empty line that makes output easier to read
                group.append(document.createElement("br"));

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

    @watch<SqlView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearOutput(true);
    }
}

class Colorizer {
    public static colorize(html: string): string {
        // Date/time
        const spaceSplitParts = html.split("&nbsp;");
        spaceSplitParts[1] = `<span class="query-time">${spaceSplitParts[1]}`;
        spaceSplitParts[2] = `${spaceSplitParts[2]}</span>`;
        html = spaceSplitParts.join("&nbsp;");

        // Parameters
        const paramNames: string[] = [];
        const paramParts = html.split("[Parameters=[");

        if (paramParts.length > 1 || !paramParts[1].startsWith("]")) {
            // paramParts[1] looks like: @__p_1='1000', @__p_0='2'], CommandType='Text', CommandTimeout='30']...
            const params = paramParts[1].split("]")[0].split(",");

            if (params.length > 0 && !!params[0]) {
                for (const param of params) {
                    const parts = param.split("=");
                    paramNames.push(parts[0]);
                }

                html = paramParts
                    .join(`[Parameters=[<span class="query-params">`)
                    .split("],&nbsp;CommandType")
                    .join("</span>],&nbsp;CommandType");

                for (const paramName of paramNames) {
                    html = html.replaceAll(paramName, `<span class="query-params">${paramName}</span>`)
                }
            }
        }

        // Keywords
        for (const keyword of this.keywords) {
            html = this.replaceKeyword(html, keyword[0], keyword.length > 1 ? keyword[1] : undefined);
        }

        return html;
    }

    private static replaceKeyword(text: string, keyword: string, additionalClasses?: string) {
        const searchValue = `&nbsp;${keyword.replaceAll(" ", "&nbsp;")}`;

        if (text.indexOf(searchValue) >= 0)
            text = text.replaceAll(searchValue, `&nbsp;<span class="sql-keyword ${additionalClasses || ""}">${keyword}</span>`);

        return text;
    }

    private static keywords = [
        ["ADD"],
        ["ADD CONSTRAINT"],
        ["ALL"],
        ["ALTER COLUMN"],
        ["ALTER TABLE"],
        ["ALTER"],
        ["AND"],
        ["ANY"],
        ["ASC"],
        ["AS"],
        ["BACKUP DATABASE"],
        ["BETWEEN"],
        ["CASE"],
        ["CHECK"],
        ["COLUMN"],
        ["CONSTRAINT"],
        ["CREATE DATABASE"],
        ["CREATE INDEX"],
        ["CREATE OR REPLACE VIEW"],
        ["CREATE TABLE"],
        ["CREATE PROCEDURE"],
        ["CREATE UNIQUE INDEX"],
        ["CREATE VIEW"],
        ["CREATE"],
        ["DATABASE"],
        ["DEFAULT"],
        ["DELETE", "sql-delete"],
        ["DESC"],
        ["DISTINCT"],
        ["DROP COLUMN"],
        ["DROP CONSTRAINT"],
        ["DROP DATABASE"],
        ["DROP DEFAULT"],
        ["DROP INDEX"],
        ["DROP TABLE"],
        ["DROP VIEW"],
        ["DROP"],
        ["EXEC"],
        ["EXISTS"],
        ["FOREIGN KEY"],
        ["FROM"],
        ["FULL OUTER JOIN"],
        ["GROUP BY"],
        ["HAVING"],
        ["INDEX"],
        ["INNER JOIN"],
        ["INSERT INTO"],
        ["INSERT INTO SELECT"],
        ["IN"],
        ["IS NULL"],
        ["IS NOT NULL"],
        ["JOIN"],
        ["LEFT JOIN"],
        ["LIKE"],
        ["LIMIT"],
        ["NOT NULL"],
        ["NOT"],
        ["ORDER BY"],
        ["OR"],
        ["OUTER JOIN"],
        ["PRIMARY KEY"],
        ["PROCEDURE"],
        ["RIGHT JOIN"],
        ["ROWNUM"],
        ["SELECT DISTINCT", "sql-select"],
        ["SELECT INTO", "sql-select"],
        ["SELECT TOP", "sql-select"],
        ["SELECT", "sql-select"],
        ["SET"],
        ["TABLE"],
        ["TOP"],
        ["TRUNCATE TABLE"],
        ["UNION ALL"],
        ["UNION"],
        ["UNIQUE"],
        ["UPDATE", "sql-update"],
        ["VALUES"],
        ["VIEW"],
        ["WHERE"]
    ];
}

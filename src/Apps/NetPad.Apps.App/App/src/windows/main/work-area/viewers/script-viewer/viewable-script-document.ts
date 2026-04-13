import {
    IEventBus,
    ScriptCodeUpdatedEvent,
    ScriptConfigPropertyChangedEvent,
    ScriptEnvironment,
    ScriptKind,
} from "@application";
import {IViewableObjectCommands} from "../viewable-object";
import {ViewableTextDocument} from "../viewable-text-document";
import {TextLanguage} from "@application/editor/text-language";
import {TextDocument} from "@application/editor/text-document";

export interface IViewableScriptDocumentCommands extends IViewableObjectCommands {
    updateCode: (newCode: string) => Promise<void>
    run: () => Promise<void>;
    stop: () => Promise<void>;
    openProperties: () => Promise<void>;
}

export class ViewableScriptDocument extends ViewableTextDocument {
    constructor(
        public readonly environment: ScriptEnvironment,
        protected override readonly commands: IViewableScriptDocumentCommands,
        private readonly eventBus: IEventBus
    ) {
        super(
            environment.script.id,
            environment.script.name,
            ViewableScriptDocument.getLanguageFromScriptKind(environment.script.config.kind),
            environment.script.code,
            commands,
        );

        this.addDisposable(
            eventBus.subscribeToServer(ScriptCodeUpdatedEvent, ev => {
                if (ev.scriptId !== this.environment.script.id) {
                    return;
                }

                this.textDocument.setText("server", ev.newCode ?? "");
            })
        );

        this.addDisposable(
            eventBus.subscribeToServer(ScriptConfigPropertyChangedEvent, ev => {
                if (ev.scriptId !== this.environment.script.id || ev.propertyName !== "Kind") {
                    return;
                }

                if (ev.newValue == "Program") this.textDocument.changeLanguage("csharp");
                else if (ev.newValue == "SQL") this.textDocument.changeLanguage("sql");
            })
        );
    }

    private static getLanguageFromScriptKind(kind: ScriptKind): TextLanguage {
        if (kind == "Program") return "csharp";
        else if (kind === "SQL") return "sql";
        else throw new Error("Unhandled script kind: " + kind);
    }

    public override get name() {
        return this.environment.script.name;
    }

    public override get isDirty() {
        return this.environment.script.isDirty;
    }

    public get script() {
        return this.environment.script;
    }

    public override get textDocument(): TextDocument {
        const alreadyCreated = this._textDocument !== null && this._textDocument !== undefined;
        const textDocument = super.textDocument;

        if (!alreadyCreated) {
            this.addDisposable(
                textDocument.onChange(async (setter) => {
                    if (setter === "server") return;
                    await this.textChanged();
                }));
        }

        return textDocument;
    }

    private async textChanged() {
        const code = this.textDocument.text;
        this.script.code = code;

        await this.commands.updateCode(code);
    }

    public run(): Promise<void> {
        return this.commands.run();
    }

    public stop(): Promise<void> {
        return this.commands.stop();
    }

    public openProperties(): Promise<void> {
        return this.commands.openProperties();
    }
}

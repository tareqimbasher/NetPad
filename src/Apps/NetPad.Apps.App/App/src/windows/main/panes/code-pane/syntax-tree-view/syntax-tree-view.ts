import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import * as monaco from "monaco-editor";
import {
    ICodeService,
    IEventBus,
    ISession,
    ISyntaxNodeOrTokenSlim,
    LinePositionSpan,
    Pane,
    SyntaxNodeOrTokenSlim,
    ViewModelBase
} from "@application";
import {LeakyMap, Util} from "@common";
import {ITextEditorService} from "@application/editor/itext-editor-service";
import {ScriptCodeUpdatedEvent} from "@application/scripts/script-code-updated-event";

interface ISyntaxNodeOrTokenViewModel extends ISyntaxNodeOrTokenSlim {
    collapsed?: boolean;
}

interface ICacheItem {
    tree: SyntaxNodeOrTokenSlim | null;
    code: string;
    semanticModel: any | null;
}

export class SyntaxTreeView extends ViewModelBase {
    private current: ISyntaxNodeOrTokenViewModel | null = null;
    private cache = new LeakyMap<string, ICacheItem>(60 * 1000);
    private decoratorCollection?: monaco.editor.IEditorDecorationsCollection;
    private error?: string;
    private showCharSpans = false;
    private semanticModel: any | null = null;

    @bindable public pane: Pane;

    constructor(@ISession private readonly session: ISession,
                @ICodeService private readonly codeService: ICodeService,
                @IEventBus private readonly eventBus: IEventBus,
                @ITextEditorService private readonly textEditorService: ITextEditorService,
                @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        this.loadSyntaxTree();
        this.loadSemanticModel();
        this.addDisposable(this.eventBus.subscribe(ScriptCodeUpdatedEvent, () => {
            this.loadSyntaxTree();
            this.loadSemanticModel();
        }));
    }

    private onMouseLeave() {
        if (this.decoratorCollection) {
            setTimeout(() => this.decoratorCollection?.clear(), 120);
        }
    }

    private expand(nodeOrToken: ISyntaxNodeOrTokenViewModel, recursive: boolean) {
        if (!nodeOrToken) {
            return;
        }

        nodeOrToken.collapsed = false;
        if (recursive) {
            nodeOrToken.children.forEach(c => this.expand(c, recursive));
        }
    }

    private collapse(nodeOrToken: ISyntaxNodeOrTokenViewModel, recursive: boolean) {
        if (!nodeOrToken) {
            return;
        }

        nodeOrToken.collapsed = true;
        if (recursive) {
            nodeOrToken.children.forEach(c => this.collapse(c, recursive));
        }
    }

    private highlightInEditor(nodeOrToken: SyntaxNodeOrTokenSlim) {
        const editor = this.textEditorService.active?.monaco;

        if (!editor) {
            this.logger.warn("No active editor found");
            return;
        }

        if (this.decoratorCollection) {
            this.decoratorCollection.clear();
        }

        this.decoratorCollection = editor.createDecorationsCollection([
            {
                range: this.spanToRange(nodeOrToken.span),
                options: {
                    inlineClassName: "focused-text"
                }
            }
        ]);
    }

    private revealInEditor(nodeOrToken: SyntaxNodeOrTokenSlim) {
        const editor = this.textEditorService.active?.monaco;

        if (!editor) {
            this.logger.warn("No active editor found");
            return;
        }

        editor.revealRangeInCenter(this.spanToRange(nodeOrToken.span));
    }

    private selectInEditor(nodeOrToken: SyntaxNodeOrTokenSlim) {
        const editor = this.textEditorService.active?.monaco;

        if (!editor) {
            this.logger.warn("No active editor found");
            return;
        }

        editor.setSelection(this.spanToRange(nodeOrToken.span));
        editor.focus();
        this.decoratorCollection?.clear();
    }

    private spanToRange(span: LinePositionSpan) {
        return {
            startLineNumber: span.start.line + 1,
            startColumn: span.start.character + 1,
            endLineNumber: span.end.line + 1,
            endColumn: span.end.character + 1
        };
    }

    @watch<SyntaxTreeView>(vm => vm.pane.isOpen)
    private paneViewModeChanged() {
        this.loadSyntaxTree();
        this.loadSemanticModel();
    }

    @watch<SyntaxTreeView>(vm => vm.session.active)
    private activeScriptChanged() {
        this.loadSyntaxTree();
        this.loadSemanticModel();
    }

    private loadSyntaxTree = Util.debounce(this, async () => {
            if (!this.pane.isOpen) {
                return;
            }

            this.error = undefined;

            const script = this.session.active?.script;
            if (!script) {
                this.setCurrent(null);
                return;
            }

            const code = script.code;

            if (!code || !code.trim()) {
                this.setCurrent(null);
                return;
            }

            const cached = this.cache.get(script.id);
            if (cached && cached.code === code) {
                this.setCurrent(cached.tree);
                return;
            }

            let current: SyntaxNodeOrTokenSlim | null;

            try {
                current = await this.codeService.getSyntaxTree(script.id);
            } catch (ex) {
                this.logger.error("Error getting syntax tree", ex);
                this.setCurrent(null);
                this.error = "Could not load syntax tree. The tree might be too deep."
                return;
            }

            this.cache.set(script.id, {
                tree: current,
                code: code,
                semanticModel: this.semanticModel
            });

            this.setCurrent(current);
        },
        500,
        true);

    private loadSemanticModel = Util.debounce(this, async () => {
            if (!this.pane.isOpen) {
                return;
            }

            this.error = undefined;

            const script = this.session.active?.script;
            if (!script) {
                this.semanticModel = null;
                return;
            }

            const code = script.code;

            if (!code || !code.trim()) {
                this.semanticModel = null;
                return;
            }

            const cached = this.cache.get(script.id);
            if (cached && cached.code === code) {
                this.semanticModel = cached.semanticModel;
                return;
            }

            let semanticModel: any | null;

            try {
                semanticModel = await this.codeService.getSemanticModel(script.id);
            } catch (ex) {
                this.logger.error("Error getting semantic model", ex);
                this.semanticModel = null;
                this.error = "Could not load semantic model. The model might be too complex."
                return;
            }

            this.cache.set(script.id, {
                tree: this.current,
                code: code,
                semanticModel: semanticModel
            });

            this.semanticModel = semanticModel;
        },
        500,
        true);

    private setCurrent(current: SyntaxNodeOrTokenSlim | null) {
        // Defer rendering of syntax tree to not block UI
        setTimeout(() => this.current = current, 1);
    }
}

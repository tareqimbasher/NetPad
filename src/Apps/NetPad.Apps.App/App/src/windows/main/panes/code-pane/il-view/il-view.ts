import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    ApiException,
    ICodeService,
    IEventBus,
    ISession,
    Pane,
    ScriptConfigPropertyChangedEvent,
    ScriptPropertyChangedEvent,
    ViewModelBase
} from "@application";
import {Util} from "@common";
import {ScriptCodeUpdatedEvent} from "@application/scripts/script-code-updated-event";
import {ProblemDetails} from "@application/api-problem-details";
import "highlight.js/styles/monokai.min.css";
import hljs from "highlight.js/lib/common";
import {observable} from "@aurelia/runtime";

interface ICacheItem {
    il: string | null;
    code: string;
}

export class IlView extends ViewModelBase {
    private current: string | null;
    private error?: string;

    @bindable public pane: Pane;
    @bindable public isActive: boolean;
    @observable private includeAssemblyHeaders: boolean;

    constructor(@ISession private readonly session: ISession,
                @ICodeService private readonly codeService: ICodeService,
                @IEventBus private readonly eventBus: IEventBus,
                @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        this.loadIL();
        this.addDisposable(this.eventBus.subscribe(ScriptCodeUpdatedEvent, () => this.loadIL()));
        this.addDisposable(this.eventBus.subscribeToServer(ScriptPropertyChangedEvent, () => this.loadIL()));
        this.addDisposable(this.eventBus.subscribeToServer(ScriptConfigPropertyChangedEvent, () => this.loadIL()));
    }


    @watch<IlView>(vm => vm.pane.isOpen)
    private paneViewModeChanged() {
        this.loadIL();
    }

    @watch<IlView>(vm => vm.session.active)
    private activeScriptChanged() {
        this.loadIL();
    }

    private isActiveChanged() {
        this.loadIL();
    }

    includeAssemblyHeadersChanged() {
        this.loadIL();
    }

    private loadIL = Util.debounce(this, async () => {
            if (!this.pane.isOpen || !this.isActive) {
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

            let current: string | null;

            try {
                current = await this.codeService.getIntermediateLanguage(
                    script.id,
                    this.includeAssemblyHeaders);

                // Reduce chance of a race condition
                if (this.session.active?.script.id !== script.id) {
                    return;
                }
            } catch (ex) {
                if (ex instanceof ApiException) {
                    const problem = ProblemDetails.fromJS(JSON.parse(ex.response));
                    this.error = problem.detail;
                }

                if (!this.error) {
                    this.error = "Could not load IL code. Check logs for more info.";
                }

                current = null;
            }

            this.setCurrent(current);
        },
        500,
        true);

    private setCurrent(current: string | null) {
        if (!current) {
            this.current = current;
            return;
        }

        // Defer rendering of syntax tree to not block UI
        setTimeout(() => {
            this.current = hljs.highlightAuto(current).value;
        }, 1);
    }
}

import {PLATFORM} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    HtmlErrorScriptOutput,
    HtmlRawScriptOutput,
    HtmlResultsScriptOutput,
    HtmlSqlScriptOutput,
    IEventBus,
    ISession,
    IShortcutManager,
    IWindowService,
    KeyCombo,
    Pane,
    PromptUserForInputCommand,
    ScriptEnvironment,
    ScriptOutputEmittedEvent,
    Settings,
    ShortcutIds
} from "@application";
import {AppWindows} from "@application/windows/app-windows";
import {OutputModel} from "./output-model";
import {DisposableCollection, KeyCode} from "@common";
import {FindTextBox} from "@application/find-text-box/find-text-box";

export class OutputPane extends Pane {
    public outputModels = new Map<string, OutputModel>();
    private current?: OutputModel;
    private disposables = new DisposableCollection();
    private findTextBox: FindTextBox;
    private activeTab = "Results";
    private tabs = [
        {
            name: "Results",
            keyBinding: new KeyCombo().withAltKey().withKey(KeyCode.Digit1),
        },
        {
            name: "SQL",
            keyBinding: new KeyCombo().withAltKey().withKey(KeyCode.Digit2),
        },
    ]

    constructor(
        private readonly element: Element,
        @ISession public readonly session: ISession,
        @IWindowService private readonly windowService: IWindowService,
        @IEventBus private eventBus: IEventBus,
        @IShortcutManager shortcutManager: IShortcutManager,
        private readonly appWindows: AppWindows,
        private readonly settings: Settings
    ) {
        super("Output", "output-icon", false);
        this.hasShortcut(shortcutManager.getShortcut(ShortcutIds.openOutput));
    }

    public bound() {
        this.setCurrentOutputModel(this.session.active);
    }

    public attached() {
        this.listenForOutputMessages();

        if (!this.isWindow) {
            this.listenForExternalOutputWindowMessages();
        }

        const tabKeysHandler = (ev: Event) => {
            const match = this.tabs.find(t => t.keyBinding.matches(ev as KeyboardEvent));
            if (match) {
                this.activeTab = match.name;
            }
        };

        this.element.addEventListener("keydown", tabKeysHandler);
        this.disposables.add(() => this.element.removeEventListener("keydown", tabKeysHandler));

        PLATFORM.queueMicrotask(() => this.activatePaneIfApplicable());
    }

    private listenForOutputMessages() {
        this.disposables.add(
            this.eventBus.subscribeToServer(ScriptOutputEmittedEvent, msg => {
                if (!msg.output) {
                    return;
                }

                const model = this.outputModels.get(msg.scriptId);
                if (!model) {
                    this.logger.warn(`Got output for script ${msg.scriptId} but no model found for it. Message: `, msg);
                    return;
                }

                if ([nameof(HtmlResultsScriptOutput), nameof(HtmlErrorScriptOutput), nameof(HtmlRawScriptOutput)].indexOf(msg.outputType) >= 0) {
                    model.resultsDumpContainer.appendOutput(msg.output);
                } else if (msg.outputType === nameof(HtmlSqlScriptOutput)) {
                    model.sqlDumpContainer.appendOutput(msg.output);
                } else {
                    this.logger.warn(`Got output for script ${msg.scriptId} but message type ${msg.outputType} is unhandled. Message: `, msg);
                }
            })
        );

        this.disposables.add(
            this.eventBus.subscribeToServer(PromptUserForInputCommand, msg => {
                const model = this.outputModels.get(msg.scriptId);
                if (!model) {
                    this.logger.warn(`Got user input command for script ${msg.scriptId} but no model found for it. Message: `, msg);
                    return;
                }

                model.inputRequest = {
                    commandId: msg.id
                };

                setTimeout(() => {
                    (this.element.querySelector(".user-input-container input") as HTMLInputElement)?.focus();
                }, 50);
            })
        );
    }

    @watch<OutputPane>(vm => vm.session.active)
    private setCurrentOutputModel(active?: ScriptEnvironment | null) {
        let newCurrent: OutputModel | undefined = undefined;

        if (active) {
            let model = this.outputModels.get(active.script.id);

            if (!model) {
                model = new OutputModel(active, this.settings);
                this.outputModels.set(active.script.id, model);
            }

            newCurrent = model;
        }

        this.current = newCurrent;

        if (this.current) {
            this.findTextBox.registerSearchableElement(
                this.current.resultsDumpContainer.element,
                ".null, .property-value, .property-name, .text, .group > .title");

            this.findTextBox.registerSearchableElement(
                this.current.sqlDumpContainer.element,
                ".text, .sql-keyword, .query-time, .query-params, .logger-name, .not-special");

            this.setFindTextBoxSearchableElement();
        }
    }

    @watch<OutputPane>(vm => vm.session.environments.length)
    private destroyUnneededOutputModels() {
        const environments = this.session.environments;

        const removed = [...this.outputModels.keys()]
            .filter(id => !environments.some(e => e.script.id === id));

        for (const id of removed) {
            const model = this.outputModels.get(id);

            if (model) {
                this.findTextBox.unregisterSearchableElement(model.resultsDumpContainer.element);
                this.findTextBox.unregisterSearchableElement(model.sqlDumpContainer.element);

                model.destroy();
                this.outputModels.delete(id);
            }
        }
    }

    @watch<OutputPane>(vm => vm.activeTab)
    private setFindTextBoxSearchableElement() {
        PLATFORM.queueMicrotask(() => {
            if (!this.current) {
                return;
            }

            if (this.activeTab === 'Results') {
                this.findTextBox.setCurrent(this.current.resultsDumpContainer.element);
            } else {
                this.findTextBox.setCurrent(this.current.sqlDumpContainer.element);
            }
        });
    }

    @watch<OutputPane>(vm => vm.session.active?.status)
    private activatePaneIfApplicable() {
        if (this.appWindows.items.find(x => x.name === "output")) {
            return;
        }

        if (this.settings.results.openOnRun && this.session.active?.status === "Running") {
            this.activate();
        }
    }

    private async openExternalOutputWindow() {
        this.hide();
        await this.windowService.openOutputWindow();
    }

    @watch<OutputPane>(vm => vm.appWindows.items.map(x => x.name))
    private reactToExternalWindowState(currentWindowNames: string, previousWindowNames: string) {
        if (this.isWindow) {
            return;
        }

        const wasOpen = previousWindowNames.indexOf("output") >= 0;
        const currentlyOpen = currentWindowNames.indexOf("output") >= 0;
        const hostHasAnActivePaneOpen = this.host?.active;

        if (wasOpen && !currentlyOpen && !hostHasAnActivePaneOpen) {
            this.activate();
        } else if (currentlyOpen) {
            this.hide();
        }
    }

    private listenForExternalOutputWindowMessages() {
        // External window will request current outputs
        const bc = new BroadcastChannel("output-window");

        bc.onmessage = (ev) => {
            if (ev.data !== "send-outputs") {
                return;
            }

            bc.postMessage([...this.outputModels.values()].map(m => m.toDto()));
        };

        this.disposables.add(() => bc.close());
    }
}

import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    ContextMenuOptions,
    CreateScriptDto,
    DataConnectionResourcesUpdatedEvent,
    DataConnectionResourcesUpdateFailedEvent,
    DataConnectionResourcesUpdatingEvent,
    DataConnectionSchemaValidationCompletedEvent,
    DataConnectionSchemaValidationStartedEvent,
    DataConnectionStore,
    IDataConnectionService,
    IEventBus,
    IScriptService,
    ISession,
    ViewModelBase
} from "@application";
import {DataConnectionViewModel} from "./data-connection-view-model";
import {DataConnectionDnd} from "@application/dnd/data-connection-dnd";

export class DataConnectionsList extends ViewModelBase {
    public dataConnectionViewModels: DataConnectionViewModel[] = [];
    public dataConnectionContextOptions: ContextMenuOptions;
    public tableContextOptions: ContextMenuOptions;

    constructor(
        private readonly element: HTMLElement,
        @ISession private readonly session: ISession,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @IScriptService private readonly scriptService: IScriptService,
        private readonly dataConnectionStore: DataConnectionStore,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public binding() {
        this.dataConnectionContextOptions = new ContextMenuOptions("data-connection-name", [
            {
                icon: "use-data-connection-current-script-icon",
                text: "Use in Current Script",
                onSelected: async (target) => {
                    const active = this.session.active;
                    if (!active) return;

                    await this.scriptService.setDataConnection(active.script.id, this.getElementOrParentDataConnectionId(target));
                }
            },
            {
                icon: "use-data-connection-new-script-icon",
                text: "Use in New Script",
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        runImmediately: true,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            },
            {
                icon: "copy-icon",
                text: "Create Similar Connection",
                onSelected: async (target) => this.copyConnection(this.getElementOrParentDataConnectionId(target))
            },
            {
                isDivider: true
            },
            {
                icon: "refresh-icon",
                text: "Refresh",
                onSelected: async (target) => this.refresh(this.getElementOrParentDataConnectionId(target))
            },
            {
                icon: "delete-icon",
                text: "Delete",
                onSelected: async (target) => this.delete(this.getElementOrParentDataConnectionId(target))
            },
            {
                isDivider: true
            },
            {
                icon: "properties-icon",
                text: "Properties",
                onSelected: async (target) => this.editConnection(this.getElementOrParentDataConnectionId(target))
            }
        ]);

        this.tableContextOptions = new ContextMenuOptions(".list-group-item.db-table > .display-text", [
            {
                icon: "data-connection-query-action",
                text: (target) => this.buildActionItemText(target, (displayText) => `${displayText}.Take(100)`),
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        code: this.buildActionCode(target, displayText => `${displayText}.Take(100).Dump();`),
                        runImmediately: true,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            },
            {
                icon: "data-connection-query-action",
                text: (target) => this.buildActionItemText(target, (displayText) => `${displayText}.Take(...)`),
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        code: this.buildActionCode(target, displayText => `${displayText}.Take(...).Dump();`),
                        runImmediately: false,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            },
            {
                icon: "data-connection-query-action",
                text: (target) => this.buildActionItemText(target, (displayText) => `${displayText}.Count()`),
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        code: this.buildActionCode(target, displayText => `${displayText}.Count().Dump();`),
                        runImmediately: true,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            },
            {
                icon: "data-connection-query-action",
                text: (target) => this.buildActionItemText(target, (displayText, abbr) => `${displayText}.Where(${abbr} => ...)`),
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        code: this.buildActionCode(target, (displayText, abbr) => `${displayText}.Where(${abbr} => ${abbr}.).Dump();`),
                        runImmediately: false,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            },
            {
                icon: "data-connection-query-action",
                text: (target) => this.buildActionItemText(target, (displayText, abbr) => `${displayText}.OrderBy(${abbr} => ...).Take(100)`),
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        code: this.buildActionCode(target, (displayText, abbr) => `${displayText}.OrderBy(${abbr} => ${abbr}.).Dump();`),
                        runImmediately: false,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            },
            {
                icon: "data-connection-query-action",
                text: (target) => this.buildActionItemText(target, (displayText, abbr) => `${displayText}.OrderByDescending(${abbr} => ...).Take(100)`),
                onSelected: async (target) => {
                    await this.scriptService.create(new CreateScriptDto({
                        code: this.buildActionCode(target, (displayText, abbr) => `${displayText}.OrderByDescending(${abbr} => ${abbr}.).Dump();`),
                        runImmediately: false,
                        dataConnectionId: this.getElementOrParentDataConnectionId(target)
                    }));
                }
            }
        ]);

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdatingEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourceBeingLoaded(msg.updatingComponent);
            }
        });

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdatedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourceCompletedLoading(msg.updatedComponent);
            }
        });

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdateFailedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourceFailedLoading(msg.failedComponent, msg.error);
            }
        });

        this.eventBus.subscribeToServer(DataConnectionSchemaValidationStartedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnectionId);
            if (vm) {
                vm.schemaValidationStarted();
            }
        });

        this.eventBus.subscribeToServer(DataConnectionSchemaValidationCompletedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnectionId);
            if (vm) {
                vm.schemaValidationCompleted();
            }
        });
    }

    public async attached() {
        this.constructDataConnectionViewModels();
    }

    public async addConnection() {
        await this.dataConnectionService.openDataConnectionWindow(null, false);
    }

    public async editConnection(connectionId: string) {
        await this.dataConnectionService.openDataConnectionWindow(connectionId, false);
    }

    public async copyConnection(connectionId: string) {
        await this.dataConnectionService.openDataConnectionWindow(connectionId, true);
    }

    public async delete(connectionId: string) {
        const connection = this.dataConnectionViewModels.find(v => v.connection.id === connectionId);
        if (!connection) return;

        if (confirm(`Are you sure you want to delete "${connection.connection.name}"?`)) {
            await this.dataConnectionService.delete(connectionId);
        }
    }

    public async refresh(connectionId: string) {
        const dataConnection = this.dataConnectionViewModels.find(v => v.connection.id === connectionId);
        if (dataConnection) {
            await dataConnection.refresh();
        }
    }

    @watch<DataConnectionsList>(vm => vm.dataConnectionStore.connections.length)
    private constructDataConnectionViewModels() {
        this.dataConnectionViewModels = this.dataConnectionStore.connections
            .sort((a, b) => a.name.localeCompare(b.name))
            .map(c => new DataConnectionViewModel(c, this.dataConnectionService));
    }

    private async copyToClipboard(text: string) {
        await navigator.clipboard.writeText(text);
        alert("Copied to clipboard!");
    }

    private getElementOrParentDataConnectionId(element: Element) {
        let id: string | null;
        let el: Element | null = element;

        do {
            id = el.getAttribute("data-connection-id");

            if (id) {
                break;
            }

            // We want to stop searching up the DOM tree when we leave the scope of this element (component)
            el = el.parentElement == this.element.parentElement ? null : el.parentElement;
        } while (!id && !!el);

        if (!id) {
            this.logger.error("Could not get data connection ID from element", element);
            throw new Error("Could not get data connection ID from element");
        }

        return id;
    }

    private buildActionItemText(clickTarget: Element, buildText: (displayText: string | undefined, abbr: string | undefined) => string): string {
        const {displayText, abbr} = this.getDisplayTextAndAbbr(clickTarget);
        if (!displayText) return "";

        return `<pre class="m-0 text-truncate">${buildText(displayText, abbr)}</pre>`;
    }

    private buildActionCode(clickTarget: Element, buildCode: (displayText: string | undefined, abbr: string | undefined) => string): string {
        const {displayText, abbr} = this.getDisplayTextAndAbbr(clickTarget);
        if (!displayText) return "";

        return buildCode(displayText, abbr);
    }

    private getDisplayTextAndAbbr(clickTarget: Element): { displayText: string | undefined, abbr: string | undefined } {
        const displayText = clickTarget?.querySelector("b")?.innerText;
        let abbr: string | undefined = undefined;

        if (displayText) {
            const firstChar = displayText[0];

            // Extract only upper case chars
            const uppers = displayText.replace(/[^A-Z]/g, '');

            abbr = (!uppers ? firstChar : uppers).toLowerCase();

            // To account for cases where display text is like "someThing", we want abbr to be "st", not "t"
            if (uppers.length > 0 && firstChar === firstChar.toLowerCase()) {
                abbr = firstChar + abbr;
            }
        }

        return {
            displayText: displayText,
            abbr: abbr
        };
    }

    private connectionDragged(event: DragEvent) {
        const connectionId = (event.target as HTMLElement).getAttribute("data-connection-id");

        if (!connectionId) return false;

        new DataConnectionDnd(connectionId).transferDataToEvent(event);

        return true;
    }
}

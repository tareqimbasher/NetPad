import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    ContextMenuOptions,
    CreateScriptDto,
    DatabaseConnection,
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
import {DatabaseServerViewModel} from "./database-server-view-model";
import {DataConnectionDnd} from "@application/dnd/data-connection-dnd";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {ScaffoldToProjectDialog} from "./scaffold-to-project/scaffold-to-project-dialog";

export class DataConnectionsList extends ViewModelBase {
    public dataConnectionViewModels: DataConnectionViewModel[] = [];
    public serverViewModels: DatabaseServerViewModel[] = [];
    public standaloneConnectionViewModels: DataConnectionViewModel[] = [];
    public dataConnectionContextOptions: ContextMenuOptions;
    public serverContextOptions: ContextMenuOptions;
    public tableContextOptions: ContextMenuOptions;

    constructor(
        private readonly element: HTMLElement,
        @ISession private readonly session: ISession,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @IScriptService private readonly scriptService: IScriptService,
        private readonly dataConnectionStore: DataConnectionStore,
        private readonly dialogUtil: DialogUtil,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public binding() {
        this.dataConnectionContextOptions = new ContextMenuOptions("data-connection-name:not(.server-name)", [
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
                icon: "code-icon",
                text: "Scaffold to C# Project",
                onSelected: async (target) => this.showScaffoldToProjectModal(this.getElementOrParentDataConnectionId(target))
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

        this.serverContextOptions = new ContextMenuOptions("data-connection-name.server-name", [
            {
                icon: "refresh-icon",
                text: "Refresh All",
                onSelected: async (target) => this.refreshServerConnections(this.getElementOrParentServerConnectionId(target))
            },
            {
                isDivider: true
            },
            {
                icon: "delete-icon",
                text: "Delete Server",
                onSelected: async (target) => this.deleteServer(this.getElementOrParentServerConnectionId(target))
            },
            {
                isDivider: true
            },
            {
                icon: "properties-icon",
                text: "Properties",
                onSelected: async (target) => this.editServer(this.getElementOrParentServerConnectionId(target))
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
                vm.resourcesAreLoading();
            }
        });

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdatedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourcesCompletedLoading();
            }
        });

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdateFailedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourcesFailedLoading(msg.error);
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
        this.constructViewModels();
    }

    public async addConnection() {
        await this.dataConnectionService.openDataConnectionWindow(null, false, false);
    }

    public async addServer() {
        await this.dataConnectionService.openDataConnectionWindow(null, false, true);
    }

    public async editServer(serverId: string) {
        await this.dataConnectionService.openDataConnectionWindow(serverId, false, true);
    }

    public async deleteServer(serverId: string) {
        const serverVm = this.serverViewModels.find(v => v.server.id === serverId);
        if (!serverVm) return;

        const confirmation = await this.dialogUtil.ask({message: `Are you sure you want to delete server "${serverVm.server.name}"?`});

        if (confirmation.value === "OK") {
            await this.dataConnectionService.deleteServer(serverId);
        }
    }

    public async refreshServerConnections(serverId: string) {
        const serverVm = this.serverViewModels.find(v => v.server.id === serverId);
        if (serverVm) {
            await serverVm.refresh();
        }
    }

    public async editConnection(connectionId: string) {
        await this.dataConnectionService.openDataConnectionWindow(connectionId, false, false);
    }

    public async copyConnection(connectionId: string) {
        await this.dataConnectionService.openDataConnectionWindow(connectionId, true, false);
    }

    public async delete(connectionId: string) {
        const connection = this.dataConnectionViewModels.find(v => v.connection.id === connectionId);
        if (!connection) return;

        const confirmation = await this.dialogUtil.ask(
            {
                message: `Are you sure you want to delete "${connection.connection.name}"? This only deletes the connection.`
            });

        if (confirmation.value === "OK") {
            await this.dataConnectionService.delete(connectionId);
        }
    }

    public async refresh(connectionId: string) {
        const dataConnection = this.dataConnectionViewModels.find(v => v.connection.id === connectionId);
        if (dataConnection) {
            await dataConnection.refresh();
        }
    }

    public async showScaffoldToProjectModal(connectionId: string) {
        const result = await this.dialogUtil.open(ScaffoldToProjectDialog, {
            connectionId: connectionId
        });

        if (result?.status !== "ok") {
            return;
        }

        await this.dialogUtil.alert({
            title: "Scaffolding Complete",
            message: `A .NET project was created at: <code>${result.value}</code>`
        });
    }

    @watch<DataConnectionsList>(vm => vm.dataConnectionStore.connections.length)
    @watch<DataConnectionsList>(vm => vm.dataConnectionStore.servers.length)
    private constructViewModels() {
        // Build all connection VMs
        this.dataConnectionViewModels = this.dataConnectionStore.connections
            .sort((a, b) => a.name.localeCompare(b.name))
            .map(c => new DataConnectionViewModel(c, this.dataConnectionService));

        // Build server VMs
        const serverIds = new Set(this.dataConnectionStore.servers.map(s => s.id));
        this.serverViewModels = this.dataConnectionStore.servers
            .sort((a, b) => a.name.localeCompare(b.name))
            .map(s => new DatabaseServerViewModel(s));

        // Group connections by serverId
        const serverVmMap = new Map(this.serverViewModels.map(vm => [vm.server.id, vm]));
        const standalone: DataConnectionViewModel[] = [];

        for (const connVm of this.dataConnectionViewModels) {
            const dbConn = connVm.connection as DatabaseConnection;
            const serverId = dbConn.serverId;

            if (serverId && serverVmMap.has(serverId)) {
                serverVmMap.get(serverId)!.connections.push(connVm);
            } else {
                standalone.push(connVm);
            }
        }

        this.standaloneConnectionViewModels = standalone;
    }

    public async copyErrorToClipboard(vm: DataConnectionViewModel) {
        if (!vm.error) {
            return;
        }

        await navigator.clipboard.writeText(vm.error);
        vm.error = null;
    }

    private getElementOrParentServerConnectionId(element: Element) {
        return this.getElementOrParentId(element, "data-server-id");
    }

    private getElementOrParentDataConnectionId(element: Element) {
        return this.getElementOrParentId(element, "data-connection-id");
    }

    private getElementOrParentId(element: Element, attributeName: string): string {
        let id: string | null;
        let el: Element | null = element;

        do {
            id = el.getAttribute(attributeName);

            if (id) {
                break;
            }

            // Stop searching up the DOM tree when we leave the scope of this element (component)
            el = el.parentElement == this.element.parentElement ? null : el.parentElement;
        } while (!id && !!el);

        if (!id) {
            this.logger.error(`Could not get '${attributeName}' from element`, element);
            throw new Error(`Could not get '${attributeName}' from element`);
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

    public connectionDragged(event: DragEvent) {
        const connectionId = (event.target as HTMLElement).getAttribute("data-connection-id");

        if (!connectionId) return false;

        new DataConnectionDnd(connectionId).transferDataToEvent(event);

        return true;
    }
}

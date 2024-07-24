import {
    ApiException,
    DatabaseStructure,
    DataConnection,
    DataConnectionResourceComponent,
    IDataConnectionService
} from "@application";

export class DataConnectionViewModel {
    private resourcesBeingLoaded = new Set<DataConnectionResourceComponent>();

    public loadingStructure = false
    public expanded = false;
    public structure?: DatabaseStructure;
    public error: string | undefined | null;

    private schemaValidationRunning = false;

    constructor(public connection: DataConnection, private readonly dataConnectionService: IDataConnectionService) {
    }

    public get loadingMessage(): string | null {
        if (this.resourcesBeingLoaded.size > 0) {
            const onlyLoadingAssembly = this.resourcesBeingLoaded.size == 1 && Array.from(this.resourcesBeingLoaded)[0] === "Assembly";
            return onlyLoadingAssembly ? "Compiling" : "Scaffolding";
        }

        if (this.schemaValidationRunning) {
            return "Validating schema";
        }

        return this.loadingStructure ? "Loading" : null;
    }

    public toggleExpand() {
        this.expanded = !this.expanded;

        if (this.expanded && !this.structure) {
            this.getDatabaseStructure();
        }
    }

    public async refresh() {
        await this.dataConnectionService.refresh(this.connection.id);
    }

    public getDatabaseStructure() {
        if (this.loadingStructure) return;

        this.loadingStructure = true;
        this.error = null;

        this.dataConnectionService.getDatabaseStructure(this.connection.id)
            .then(structure => {
                this.structure = structure;
            })
            .catch((err) => {
                if (err instanceof ApiException) {
                    this.error = err.errorResponse?.message;
                }

                if (!this.error) {
                    this.error = err.message;
                }
            })
            .finally(() => this.loadingStructure = false);
    }

    public resourceBeingLoaded(component: DataConnectionResourceComponent) {
        this.resourcesBeingLoaded.add(component);
    }

    public resourceCompletedLoading(component: DataConnectionResourceComponent) {
        this.resourcesBeingLoaded.delete(component);
        this.error = null;

        // If structure was previously fetched and this is last resource being loaded
        if (this.structure && this.resourcesBeingLoaded.size === 0) {
            this.getDatabaseStructure();
        }
    }

    public resourceFailedLoading(component: DataConnectionResourceComponent, error: string | undefined) {
        this.resourcesBeingLoaded.delete(component);
        this.error = error || "Error";
    }

    public schemaValidationStarted() {
        this.schemaValidationRunning = true;
    }

    public schemaValidationCompleted() {
        this.schemaValidationRunning = false;
    }
}

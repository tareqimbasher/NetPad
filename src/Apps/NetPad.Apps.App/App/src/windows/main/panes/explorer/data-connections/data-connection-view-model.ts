import {
    ApiException,
    DatabaseStructure,
    DataConnection,
    IDataConnectionService
} from "@application";

export class DataConnectionViewModel {
    public loadingResources = false;
    public loadingStructure = false
    public expanded = false;
    public structure?: DatabaseStructure;
    public error: string | undefined | null;

    private schemaValidationRunning = false;

    constructor(public connection: DataConnection, private readonly dataConnectionService: IDataConnectionService) {
    }

    public get loadingMessage(): string | null {
        if (this.loadingResources) {
            return "Scaffolding";
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

    public resourcesAreLoading() {
        this.loadingResources = true;
    }

    public resourcesCompletedLoading() {
        this.loadingResources = false;
        this.error = null;

        // If structure was previously fetched and resources were renewed, fetch it again
        if (this.structure) {
            this.getDatabaseStructure();
        }
    }

    public resourcesFailedLoading(error: string | undefined) {
        this.loadingResources = false;
        this.error = error || "Error";
    }

    public schemaValidationStarted() {
        this.schemaValidationRunning = true;
    }

    public schemaValidationCompleted() {
        this.schemaValidationRunning = false;
    }
}

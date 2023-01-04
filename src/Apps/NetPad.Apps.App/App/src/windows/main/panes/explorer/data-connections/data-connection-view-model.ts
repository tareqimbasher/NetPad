import {
    ApiException,
    DatabaseStructure,
    DataConnection,
    DataConnectionResourceComponent,
    IDataConnectionService
} from "@domain";

export class DataConnectionViewModel {
    private resourceLoading = new Set<DataConnectionResourceComponent>();

    public expanded = false;
    public structure?: DatabaseStructure;
    public error: string | undefined | null;
    public loadingStructure = false

    constructor(public connection: DataConnection, private readonly dataConnectionService: IDataConnectionService) {
    }

    public get loadingMessage(): string | null {
        if (this.resourceLoading.size > 0) {
            const onlyLoadingAssembly = this.resourceLoading.size == 1 && Array.from(this.resourceLoading)[0] === "Assembly";
            return onlyLoadingAssembly ? "Compiling" : "Scaffolding";
        }

        return this.loadingStructure ? "Loading" : null
    }

    public toggleExpand() {
        this.expanded = !this.expanded;

        if (this.expanded && !this.structure) {
            this.getDatabaseStructure();
        }
    }

    public async refresh() {
        await this.dataConnectionService.refresh(this.connection.id);
        this.getDatabaseStructure();
    }

    public getDatabaseStructure() {
        if (this.loadingStructure) return;

        this.loadingStructure = true;
        this.error = null;

        this.dataConnectionService.getDatabaseStructure(this.connection.id)
            .then(structure => {
                this.structure = structure;
            })
            .catch((err: ApiException) => {
                if (err.response) {
                    const serverResponse = JSON.parse(err.response);
                    if (serverResponse?.message) {
                        this.error = serverResponse.message;
                    }
                }

                if (!this.error) {
                    this.error = err.message;
                }
            })
            .finally(() => this.loadingStructure = false);
    }

    public resourceBeingLoaded(component: DataConnectionResourceComponent) {
        this.resourceLoading.add(component);
    }

    public resourceCompletedLoading(component: DataConnectionResourceComponent) {
        this.resourceLoading.delete(component);
        this.error = null;
    }

    public resourceFailedLoading(component: DataConnectionResourceComponent, error: string | undefined) {
        this.resourceLoading.delete(component);
        this.error = error || "Error";
    }
}

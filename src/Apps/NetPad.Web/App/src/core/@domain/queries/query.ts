export class Query {
    public name: string;
    public filePath?: string;
    public code: string;

    public get isNew() {
        return !this.filePath;
    }

    public set isNew(value) {}
}

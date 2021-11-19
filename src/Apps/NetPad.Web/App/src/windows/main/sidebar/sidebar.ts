import path from "path";
import {IQueryManager} from "@domain";

export class Sidebar {
    private queries: { name: string, path: string }[] = [];

    constructor(@IQueryManager readonly queryManager: IQueryManager) {
    }

    public async attached() {
        const queries = await this.queryManager.getQueries();
        this.queries = queries.map(q => {
            return {
                name: path.parse(q).name,
                path: q
            }
        });
    }
}

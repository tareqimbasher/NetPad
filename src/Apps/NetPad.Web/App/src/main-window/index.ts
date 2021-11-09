import {IHttpClient} from "aurelia";
import {IQueryManager, ISession, Query, Settings} from "@domain";
import {Mapper} from "@common";
import * as path from "path";
import {QueriesClient} from "@domain/api";

export class Index {
    private queryList: string[] = [];

    constructor(
        @ISession readonly session: ISession,
        @IQueryManager readonly queryManager: IQueryManager,
        private queriesClient: QueriesClient,
        @IHttpClient readonly httpClient: IHttpClient,
        readonly settings: Settings) {
    }

    public async binding() {
        const response = await this.httpClient.get("settings");
        return Mapper.toInstance(this.settings, await response.json());
    }

    public async openQuery(queryName: string) {
        const query = await this.queryManager.openQuery(path.join(this.settings.queriesDirectoryPath, queryName));
        const query2 = await this.queriesClient.openQuery(path.join(this.settings.queriesDirectoryPath, queryName));
        console.log(query);
        console.log(query2);
    }

    public async attached() {
        const response = await this.httpClient.get("queries");
        this.queryList = (await response.json() as string[]);
    }
}

class Proxy {
    // Get open queries
    // Create new
    // Save
    // Open existing

    // Update code
    // Rename query
    // Run Query
    // Reference DLLs and Packages

    // Autocomplete

}

import {IQueryManager, ISession, ISessionManager, Query} from "@domain";
import {ipcRenderer} from "electron";
import {QueryBackgroundService} from "./background-services/query-background-service";

export class Index {
    public results: string = "";

    constructor(
        @ISession readonly session: ISession,
        @ISessionManager readonly sessionManager: ISessionManager,
        @IQueryManager readonly queryManager: IQueryManager,
        readonly queryBackgroundService: QueryBackgroundService) {
    }

    public async binding() {
        this.queryBackgroundService.start();

        ipcRenderer.on('session-query-added', (event, json) => {
            const queries = JSON.parse(json).map(q => Query.fromJS(q)) as Query[];
            this.session.add(...queries);
        });

        ipcRenderer.on('session-query-removed', (event, json) => {
            const queries = JSON.parse(json).map(q => Query.fromJS(q)) as Query[];
            this.session.remove(...queries);
        });
    }

    public async runQuery() {
        if (!this.session.activeQuery) return;
        this.results = (await this.queryManager.run(this.session.activeQuery.id)).replaceAll("\n", "<br/>");
    }

    public async attached() {
        const openQueries = await this.sessionManager.getOpenQueries();
        this.session.add(...openQueries);
    }
}

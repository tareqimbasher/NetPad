import {IQueryManager, ISession, ISessionManager, Query} from "@domain";

const {ipcRenderer} = require("electron");

export class Index {
    public results: string = "";

    constructor(
        @ISession readonly session: ISession,
        @ISessionManager readonly sessionManager: ISessionManager,
        @IQueryManager readonly queryManager: IQueryManager) {
    }

    public async binding() {
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
        this.results = await this.queryManager.run(this.session.activeQuery.id);
    }

    public async attached() {
        const openQueries = await this.sessionManager.getOpenQueries();
        this.session.add(...openQueries);
    }
}

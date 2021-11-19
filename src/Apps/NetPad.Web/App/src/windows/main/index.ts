import {IQueryManager, ISession, ISessionManager} from "@domain";
import {IBackgroundService} from "./background-services";
import {IContainer} from "aurelia";

export class Index {
    private readonly backgroundServices: IBackgroundService[] = [];

    constructor(
        @ISession readonly session: ISession,
        @ISessionManager readonly sessionManager: ISessionManager,
        @IQueryManager readonly queryManager: IQueryManager,
        @IContainer container: IContainer) {
        this.backgroundServices.push(...container.getAll(IBackgroundService));
    }

    public async binding() {
        for (const backgroundService of this.backgroundServices) {
            await backgroundService.start();
        }
    }

    public async attached() {
        const openQueries = await this.sessionManager.getOpenQueries();
        this.session.add(...openQueries);

        if (this.session.queries.length === 0) {
            this.queryManager.create();
        }
    }
}

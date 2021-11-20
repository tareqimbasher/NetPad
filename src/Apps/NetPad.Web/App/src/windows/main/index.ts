import {IScriptRepository, ISession, ISessionManager} from "@domain";
import {IBackgroundService} from "./background-services";
import {IContainer} from "aurelia";

export class Index {
    private readonly backgroundServices: IBackgroundService[] = [];

    constructor(
        @ISession readonly session: ISession,
        @ISessionManager readonly sessionManager: ISessionManager,
        @IScriptRepository readonly scriptRepository: IScriptRepository,
        @IContainer container: IContainer) {
        this.backgroundServices.push(...container.getAll(IBackgroundService));
    }

    public async binding() {
        for (const backgroundService of this.backgroundServices) {
            await backgroundService.start();
        }
    }

    public async attached() {
        const openScripts = await this.sessionManager.getOpenScripts();
        this.session.add(...openScripts);

        if (this.session.scripts.length === 0) {
            this.scriptRepository.create();
        }
    }
}

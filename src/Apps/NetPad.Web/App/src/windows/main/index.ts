import {IScriptManager, ISession} from "@domain";
import {IBackgroundService} from "./background-services";
import {IContainer} from "aurelia";

export class Index {
    private readonly backgroundServices: IBackgroundService[] = [];

    constructor(
        @ISession readonly session: ISession,
        @IScriptManager readonly scriptManager: IScriptManager,
        @IContainer container: IContainer) {
        this.backgroundServices.push(...container.getAll(IBackgroundService));
    }

    public async binding() {
        for (const backgroundService of this.backgroundServices) {
            await backgroundService.start();
        }

        await this.session.initialize();
        if (this.session.environments.length === 0) {
            await this.scriptManager.create();
        }
    }
}

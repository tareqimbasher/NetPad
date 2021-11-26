import {IScriptService, ISession} from "@domain";
import {IBackgroundService} from "./background-services";
import {IContainer} from "aurelia";

export class Index {
    private readonly backgroundServices: IBackgroundService[] = [];

    constructor(
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService,
        @IContainer container: IContainer) {
        this.backgroundServices.push(...container.getAll(IBackgroundService));
    }

    public async binding() {
        for (const backgroundService of this.backgroundServices) {
            await backgroundService.start();
        }

        await this.session.initialize();
        if (this.session.environments.length === 0) {
            await this.scriptService.create();
        }
    }
}

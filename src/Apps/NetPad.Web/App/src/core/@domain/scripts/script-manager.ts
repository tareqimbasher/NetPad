import {DI, IHttpClient} from "aurelia";
import {IScriptsService, ScriptsService, ISession} from "@domain";

export interface IScriptManager extends IScriptsService {}

export const IScriptManager = DI.createInterface<IScriptManager>();

export class ScriptManager extends ScriptsService implements IScriptManager {
    constructor(baseUrl: string, @IHttpClient http: IHttpClient, @ISession readonly session: ISession) {
        super(baseUrl, http);
    }

    public override async open(filePath: string | null | undefined): Promise<void> {
        const existing = this.session.scripts.find(q => q.filePath == filePath);
        if (existing)
            this.session.makeActive(existing);
         else
            await super.open(filePath);
    }
}

import {DI, IHttpClient} from "aurelia";
import {IScriptsService, ScriptsService, ISession} from "@domain";

export interface IScriptRepository extends IScriptsService {}

export const IScriptRepository = DI.createInterface<IScriptRepository>();

export class ScriptRepository extends ScriptsService implements IScriptRepository {
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

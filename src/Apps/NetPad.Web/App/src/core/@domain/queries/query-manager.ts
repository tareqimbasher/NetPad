import {DI, IHttpClient} from "aurelia";
import {IQueriesService, QueriesService, ISession} from "@domain";

export interface IQueryManager extends IQueriesService {}

export const IQueryManager = DI.createInterface<IQueryManager>("IQueryManager");

export class QueryManager extends QueriesService implements IQueryManager {
    constructor(baseUrl: string, @IHttpClient http: IHttpClient, @ISession readonly session: ISession) {
        super(baseUrl, http);
    }

    public override async open(filePath: string | null | undefined): Promise<void> {
        const existing = this.session.queries.find(q => q.filePath == filePath);
        if (existing)
            this.session.makeActive(existing);
         else
            await super.open(filePath);
    }
}

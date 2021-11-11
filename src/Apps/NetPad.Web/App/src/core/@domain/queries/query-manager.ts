import {DI, IHttpClient} from "aurelia";
import {Mapper} from "@common";
import {ISession, QueriesService, Query} from "@domain";

export interface IQueryManager {
    open(filePath: string): Promise<void>;
    close(id: string): Promise<void>;
}

export const IQueryManager = DI.createInterface<IQueryManager>(nameof("IQueryManager"));

export class QueryManager extends QueriesService {
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


    // public async openQuery(filePath: string): Promise<Query> {
    //     let query = this.session.queries.find(q => !q.isNew && q.filePath == filePath);
    //     try {
    //         if (query)
    //             return query;
    //
    //         const response = await this.httpClient.get("queries/open?filePath=" + filePath);
    //         query = Mapper.toInstance(new Query(), await response.json());
    //
    //         this.session.queries.push(query);
    //         return query;
    //     }
    //     finally {
    //         if (query)
    //             this.session.activeQuery = query;
    //     }
    // }
}

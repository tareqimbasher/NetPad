import {DI, IHttpClient} from "aurelia";
import {Mapper} from "@common";
import {ISession, Query} from "@domain";

export interface IQueryManager {
    openQuery(filePath: string): Promise<Query>;
}
export const IQueryManager = DI.createInterface<IQueryManager>(nameof("IQueryManager"));

export class QueryManager implements IQueryManager {
    constructor(@ISession readonly session: ISession, @IHttpClient readonly httpClient: IHttpClient) {
    }

    public async openQuery(filePath: string): Promise<Query> {
        let query = this.session.queries.find(q => !q.isNew && q.filePath == filePath);
        try {
            if (query)
                return query;

            const response = await this.httpClient.get("queries/open?filePath=" + filePath);
            query = Mapper.toInstance(new Query(), await response.json());

            this.session.queries.push(query);
            return query;
        }
        finally {
            if (query)
                this.session.activeQuery = query;
        }
    }
}

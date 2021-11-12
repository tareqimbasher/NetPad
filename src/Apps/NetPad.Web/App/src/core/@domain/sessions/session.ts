import {Query} from "@domain";
import {DI} from "aurelia";

export interface ISession {
    queries: Query[];
    activeQuery: Query | undefined;
    add(...queries: Query[]): void;
    remove(...queries: Query[]): void;
    makeActive(query: Query): void;
}

export const ISession = DI.createInterface<ISession>(nameof("ISession"));

export class Session implements ISession {
    public activeQuery: Query | null | undefined;

    public queries: Query[] = [];

    public add(...queries: Query[]) {
        this.queries.push(...queries);
        this.makeActive(queries[queries.length - 1]);
    }

    public remove(...queries: Query[]) {
        for (let query of queries) {
            const ix = this.queries.findIndex(q => q.id == query.id);
            if (ix >= 0)
                this.queries.splice(ix, 1);
        }

        this.makeActive(this.queries.length > 0 ? this.queries[0] : null);
    }

    public makeActive(query: Query | null | undefined) {
        this.activeQuery = query;
    }
}

import {Query} from "@domain";
import {DI} from "aurelia";

export interface ISession {
    queries: Query[];
    activeQuery?: Query;
}
export const ISession = DI.createInterface<ISession>(nameof("ISession"));

export class Session implements ISession{
    public queries: Query[] = [];
    public activeQuery?: Query;
}

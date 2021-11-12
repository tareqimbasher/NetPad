import {DI} from "aurelia";
import {ISessionService, SessionService} from "@domain/api";

export interface ISessionManager extends ISessionService {}

export const ISessionManager = DI.createInterface<ISessionManager>("ISessionManager");

export class SessionManager extends SessionService implements ISessionManager {
}

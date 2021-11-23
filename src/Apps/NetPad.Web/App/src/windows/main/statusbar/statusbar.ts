import {ISession} from "@domain";

export class Statusbar {
    constructor(@ISession readonly session: ISession) {
    }
}

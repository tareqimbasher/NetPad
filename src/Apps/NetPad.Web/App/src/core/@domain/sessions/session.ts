import {Script} from "@domain";
import {DI} from "aurelia";

export interface ISession {
    scripts: Script[];
    activeScript: Script | undefined;
    add(...scripts: Script[]): void;
    remove(...scripts: Script[]): void;
    makeActive(script: Script): void;
}

export const ISession = DI.createInterface<ISession>();

export class Session implements ISession {
    public activeScript: Script | null | undefined;

    public scripts: Script[] = [];

    public add(...scripts: Script[]) {
        this.scripts.push(...scripts);
        this.makeActive(scripts[scripts.length - 1]);
    }

    public remove(...scripts: Script[]) {
        for (let script of scripts) {
            const ix = this.scripts.findIndex(q => q.id == script.id);
            if (ix >= 0)
                this.scripts.splice(ix, 1);
        }

        this.makeActive(this.scripts.length > 0 ? this.scripts[0] : null);
    }

    public makeActive(script: Script | null | undefined) {
        this.activeScript = script;
    }
}

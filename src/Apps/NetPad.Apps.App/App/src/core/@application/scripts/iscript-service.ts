import {DI} from "aurelia";
import {IScriptsApiClient} from "@application";

export interface IScriptService extends IScriptsApiClient {
    openRenamePrompt(script: { id: string; name: string }): Promise<void>;
}

export const IScriptService = DI.createInterface<IScriptService>();

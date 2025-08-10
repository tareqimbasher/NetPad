import {DI} from "aurelia";
import {IScriptsApiClient, Script} from "@application";

export interface IScriptService extends IScriptsApiClient {
    openRenamePrompt(script: Script): Promise<void>;
}

export const IScriptService = DI.createInterface<IScriptService>();

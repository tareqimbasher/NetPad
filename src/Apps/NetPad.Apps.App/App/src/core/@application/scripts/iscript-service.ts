import {DI} from "aurelia";
import {IScriptsApiClient} from "@application";

export interface IScriptService extends IScriptsApiClient {}

export const IScriptService = DI.createInterface<IScriptService>();

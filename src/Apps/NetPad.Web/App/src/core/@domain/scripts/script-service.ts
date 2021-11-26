import {DI} from "aurelia";
import {IScriptsApiClient, ScriptsApiClient} from "@domain";

export interface IScriptService extends IScriptsApiClient {}

export const IScriptService = DI.createInterface<IScriptService>();

export class ScriptService extends ScriptsApiClient implements IScriptService {}

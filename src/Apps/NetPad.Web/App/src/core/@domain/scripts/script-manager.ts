import {DI} from "aurelia";
import {IScriptsService, ScriptsService, ISession} from "@domain";

export interface IScriptManager extends IScriptsService {}

export const IScriptManager = DI.createInterface<IScriptManager>();

export class ScriptManager extends ScriptsService implements IScriptManager {}

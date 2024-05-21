import {DI} from "aurelia";
import {ICodeApiClient, CodeApiClient} from "@domain";

export interface ICodeService extends ICodeApiClient {
}

export const ICodeService = DI.createInterface<ICodeService>();

export class CodeService extends CodeApiClient implements ICodeService {
}

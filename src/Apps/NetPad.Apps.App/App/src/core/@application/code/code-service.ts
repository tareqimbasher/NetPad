import {DI} from "aurelia";
import {CodeApiClient, ICodeApiClient} from "@application";

export interface ICodeService extends ICodeApiClient {
}

export const ICodeService = DI.createInterface<ICodeService>();

export class CodeService extends CodeApiClient implements ICodeService {
}

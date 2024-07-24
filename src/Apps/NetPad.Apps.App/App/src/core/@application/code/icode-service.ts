import {DI} from "aurelia";
import {ICodeApiClient} from "@application";

export interface ICodeService extends ICodeApiClient {
}

export const ICodeService = DI.createInterface<ICodeService>();

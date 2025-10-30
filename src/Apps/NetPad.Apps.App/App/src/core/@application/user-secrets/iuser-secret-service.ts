import {IUserSecretsApiClient} from "@application";
import {DI} from "aurelia";

export const IUserSecretService = DI.createInterface<IUserSecretService>();

export interface IUserSecretService extends IUserSecretsApiClient {}

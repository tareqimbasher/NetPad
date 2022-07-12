import {IOmniSharpApiClient, OmniSharpApiClient} from "./api";
import {DI} from "aurelia";

export interface IOmniSharpService extends IOmniSharpApiClient {
}

export const IOmniSharpService = DI.createInterface<IOmniSharpService>();

export class OmniSharpService extends OmniSharpApiClient implements IOmniSharpService {
}

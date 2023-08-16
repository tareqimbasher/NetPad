import {resolve} from "aurelia";
import {CancellationToken} from "monaco-editor";
import {IOmniSharpService} from "../omnisharp-service";

export abstract class FeatureProvider {
    protected readonly omnisharpService: IOmniSharpService = resolve(IOmniSharpService);

    protected getAbortSignal(cancellationToken: CancellationToken): AbortSignal {
        return new AbortController().signalFrom(cancellationToken);
    }
}

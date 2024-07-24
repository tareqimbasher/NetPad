import {resolve} from "aurelia";
import {CancellationToken} from "monaco-editor";
import {IOmniSharpService} from "../omnisharp-service";
import {MonacoEditorUtil} from "@application";

export abstract class FeatureProvider {
    protected readonly omnisharpService: IOmniSharpService = resolve(IOmniSharpService);

    protected getAbortSignal(cancellationToken: CancellationToken): AbortSignal {
        return MonacoEditorUtil.abortSignalFrom(10000, cancellationToken);
    }
}

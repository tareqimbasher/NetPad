import {ViewModelBase} from "@application";
import {bindable, ILogger, PLATFORM, resolve} from "aurelia";
import {OutputModel} from "../output-model";
import {DumpContainer} from "./dump-container";

export abstract class OutputViewBase extends ViewModelBase {
    @bindable public model: OutputModel;
    public dumpContainer: DumpContainer;
    protected dumpContainerWrapper: Element;

    public constructor() {
        super(resolve(ILogger));
    }

    public attached() {
        this.modelChanged(this.model);
    }

    protected async modelChanged(model: OutputModel, previous?: OutputModel) {
        const dumpContainer = await this.getDumpContainer(model);

        PLATFORM.queueMicrotask(async () => {
            if (previous) {
                (await this.getDumpContainer(previous)).detachingFromDom();
            }

            this.dumpContainer = dumpContainer;
            this.dumpContainerWrapper.replaceChildren(dumpContainer.element);

            dumpContainer.attachedToDom();
        });
    }

    private async getDumpContainer(model: OutputModel) {
        if (this instanceof (await import("./results-view/results-view")).ResultsView) {
            return model.resultsDumpContainer;
        } else {
            return model.sqlDumpContainer;
        }
    }
}

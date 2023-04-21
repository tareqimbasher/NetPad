import {DI} from "aurelia";
import {WithDisposables} from "@common";
import {ViewerHostCollection} from "./viewers/viewer-host-collection";
import {IWorkAreaAppearance} from "./work-area-appearance";

export const IWorkAreaService = DI.createInterface<IWorkAreaService>();

export interface IWorkAreaService {
    readonly viewerHosts: ViewerHostCollection;
    readonly appearance: IWorkAreaAppearance;
}

export class WorkAreaService extends WithDisposables implements IWorkAreaService {
    public readonly viewerHosts = new ViewerHostCollection();

    constructor(@IWorkAreaAppearance public readonly appearance: IWorkAreaAppearance) {
        super();
        this.appearance.load();
        this.addDisposable(this.appearance);
    }
}

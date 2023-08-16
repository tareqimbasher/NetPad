import {ViewableObject} from "./viewable-object";
import {ViewModelBase} from "@application";
import {observable} from "@aurelia/runtime";
import {ViewerHost} from "./viewer-host";
import {ILogger} from "aurelia";

export abstract class Viewer extends ViewModelBase {
    @observable public viewable: ViewableObject;

    protected constructor(public readonly host: ViewerHost, logger: ILogger) {
        super(logger);
    }

    public abstract canOpen(viewable: ViewableObject): boolean;
    public abstract open(viewable: ViewableObject): void;
    public abstract close(viewable: ViewableObject): void;
}

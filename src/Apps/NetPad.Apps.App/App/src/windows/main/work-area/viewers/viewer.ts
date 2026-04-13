import {ViewableObject} from "./viewable-object";
import {ViewModelBase} from "@application";
import {observable} from "@aurelia/runtime";
import {ViewerHost} from "./viewer-host";
import {ILogger} from "aurelia";

export abstract class Viewer extends ViewModelBase {
    @observable public viewable: ViewableObject;
    // Set immediately after construction by ViewerHost. Not assigned in the constructor so
    // that concrete viewers can be instantiated via DI (container.getFactory).
    public host!: ViewerHost;

    protected constructor(logger: ILogger) {
        super(logger);
    }

    public setHost(host: ViewerHost): void {
        this.host = host;
    }

    // Viewer lifetime is owned by ViewerHost, not by Aurelia attach/detach. ViewerHost will
    // call dispose() during eviction (see ViewerHost.removeUnneededViewers). Overriding this
    // prevents ViewModelBase.detaching() from auto-disposing the viewer while it's still
    // cached in the host, which would leave dangling references.
    protected override detaching() {
        this.componentLifecycleLogger.debug("detaching...");
    }

    public abstract canOpen(viewable: ViewableObject): boolean;
    public abstract open(viewable: ViewableObject): void;
    public abstract close(viewable: ViewableObject): void;
}

import {ILogger} from "aurelia";
import {WithDisposables} from "@common";

/**
 * Can be used as a base class for a UI component's view model, offers common functionality.
 */
export class ViewModelBase extends WithDisposables {
    protected logger: ILogger;

    constructor(@ILogger logger: ILogger) {
        super();
        this.logger = logger.scopeTo((this as Record<string, unknown>).constructor.name)
    }

    public attaching() {
        this.logComponentLifecycle("attaching...");
    }

    public detaching() {
        this.logComponentLifecycle("detaching...");
        this.dispose();
    }

    private logComponentLifecycle(action: string) {
        this.logger.scopeTo("ComponentLifecycle").debug(action);
    }
}

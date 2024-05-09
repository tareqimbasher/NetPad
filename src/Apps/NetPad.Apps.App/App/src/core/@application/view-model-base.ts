import {ILogger} from "aurelia";
import {WithDisposables} from "@common";

/**
 * Can be used as a base class for a UI component's view model, offers common functionality.
 */
export class ViewModelBase extends WithDisposables {
    protected logger: ILogger;
    protected componentLifecycleLogger: ILogger;

    constructor(@ILogger logger: ILogger) {
        super();

        if (logger === undefined || logger === null) {
            throw new Error("logger is null or undefined");
        }

        this.logger = logger.scopeTo((this as Record<string, unknown>).constructor.name)
        this.componentLifecycleLogger = this.logger.scopeTo("ComponentLifecycle");
    }

    protected attaching() {
        this.componentLifecycleLogger.debug("attaching...");
    }

    protected detaching() {
        this.componentLifecycleLogger.debug("detaching...");
        this.dispose();
    }
}

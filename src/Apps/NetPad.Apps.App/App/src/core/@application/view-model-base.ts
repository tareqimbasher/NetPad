import {ILogger} from "aurelia";

export class ViewModelBase {
    protected disposables: (() => void)[] = [];
    protected logger: ILogger;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo((this as Record<string, unknown>).constructor.name)
    }

    public detaching() {
        this.disposables.forEach(disposable => {
            try {
                disposable();
            }
            catch (ex) {
                this.logger.error("Error while disposing", ex, disposable);
            }
        });
    }
}

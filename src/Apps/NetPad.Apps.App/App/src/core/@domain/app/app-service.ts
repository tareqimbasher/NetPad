import {DI} from "aurelia";
import {AppApiClient, AppDependencyCheckResult, IAppApiClient} from "@domain";
import {Util} from "@common/utils/util";

export interface IAppService extends IAppApiClient {}

export const IAppService = DI.createInterface<IAppService>();

export class AppService extends AppApiClient implements IAppService {
    private lastDependencyCheckResult: Promise<AppDependencyCheckResult> | undefined;
    private debouncedNullifyLastDepCheckResult = Util.debounce(this, () => {
        this.lastDependencyCheckResult = undefined;
    }, 3000);

    public override async checkDependencies(signal?: AbortSignal | undefined): Promise<AppDependencyCheckResult> {
        if (!this.lastDependencyCheckResult) {
            this.lastDependencyCheckResult = super.checkDependencies(signal);
            this.debouncedNullifyLastDepCheckResult();
        }

        // Even though we're caching, we want to return a new instance everytime
        // Reason is consumers of service methods assume they are getting data they can
        // take ownership of. They should be able to mutate the result without having
        // to worry about, or take into account, other consumers also reading from the same
        // cached instance.
        return this.lastDependencyCheckResult.then(r => AppDependencyCheckResult.fromJS(r));
    }
}

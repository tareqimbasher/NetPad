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
        if (this.lastDependencyCheckResult) return this.lastDependencyCheckResult;

        this.lastDependencyCheckResult = super.checkDependencies(signal);
        this.debouncedNullifyLastDepCheckResult();
        return this.lastDependencyCheckResult;
    }
}

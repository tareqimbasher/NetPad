import {DI} from "aurelia";
import {AppApiClient, AppDependencyCheckResult, IAppApiClient, SemanticVersion} from "@domain";
import {Util} from "@common/utils/util";

export interface IAppService extends IAppApiClient {
    /**
     * Returns true if there is a newer version than the version
     * of the currently running app; otherwise, false.
     */
    get appHasUpdate(): boolean;


    /**
     * Checks if there is an update for the app.
     */
    checkForUpdates(): Promise<void>;

    /**
     * Gets the current version and latest available version of the app.
     */
    getCurrentAndLatestVersions(): Promise<{ current: SemanticVersion, latest: SemanticVersion } | null>;
}

export const IAppService = DI.createInterface<IAppService>();

export class AppService extends AppApiClient implements IAppService {
    public get appHasUpdate() {
        return this._appHasUpdate;
    }

    private _appHasUpdate = false;
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

    public async checkForUpdates(): Promise<void> {
        const versions = await this.getCurrentAndLatestVersions();

        if (versions == null) {
            return;
        }

        const current = versions.current;
        const latest = versions.latest;

        this._appHasUpdate = latest.major > current.major
            || latest.minor > current.minor
            || latest.patch > current.patch;
    }

    public async getCurrentAndLatestVersions(): Promise<{ current: SemanticVersion, latest: SemanticVersion } | null> {
        const appId = await this.getIdentifier();
        const current = appId.version;

        const latest = await this.getLatestVersion();
        if (!latest) {
            return null;
        }

        return {
            current: current,
            latest: latest
        };
    }
}

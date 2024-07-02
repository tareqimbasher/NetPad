import {DI} from "aurelia";
import {AppApiClient, AppDependencyCheckResult, IAppApiClient} from "@application";
import {Util} from "@common/utils/util";
import {Version} from "@common/data/version";

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
    getCurrentAndLatestVersions(): Promise<{current: Version, latest: Version} | null>;
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

        this._appHasUpdate = versions.latest.greaterThan(versions.current);
    }

    public async getCurrentAndLatestVersions(): Promise<{current: Version, latest: Version} | null> {
        const appId = await this.getIdentifier();
        const current = new Version(appId.version);

        if (current.isEmpty) {
            return null;
        }

        const latest = new Version(await this.getLatestVersion());
        if (latest.isEmpty) {
            return null;
        }

        return {
            current: current,
            latest: latest
        };
    }
}

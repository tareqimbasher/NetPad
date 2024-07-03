import {IAppApiClient} from "@application";
import {Version} from "@common/data/version";
import {DI} from "aurelia";

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

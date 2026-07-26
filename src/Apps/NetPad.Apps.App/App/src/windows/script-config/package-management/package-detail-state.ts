/** The number of recent versions the panel shows before "All versions" is expanded. */
export const COLLAPSED_VERSION_COUNT = 5;

export interface DisplayVersion {
    version: string;
    latest: boolean;
    referenced: boolean;
    cached: boolean;
}

export interface DetailState {
    contextLine: string;
    displayedVersions: DisplayVersion[];
    hasMoreVersions: boolean;
    dependenciesLabel: string;
    primaryAction: { label: string; version: string } | null;
    canRemove: boolean;
    canDeleteFromCache: boolean;
    canDownloadOnly: boolean;
}

/** The subset of package fields the derivation needs. */
export interface DetailStatePackage {
    packageId: string;
    title: string;
    version?: string;
    latestAvailableVersion?: string;
    dependencies?: readonly unknown[];
}

/** Inputs needed to compute {@link DetailState} */
export interface DetailStateInput {
    package: DetailStatePackage;
    /** The version the user picked in the panel, if any (a cache row selects its own version). */
    pickedVersion?: string;
    /** Versions fetched for the panel, undefined while still loading. */
    fetchedVersions?: string[];
    expanded: boolean;
    filter: string;
    referencedVersion?: string;
    cachedVersions: string[];
}

export function emptyDetailState(): DetailState {
    return {
        contextLine: "",
        displayedVersions: [],
        hasMoreVersions: false,
        dependenciesLabel: "Dependencies",
        primaryAction: null,
        canRemove: false,
        canDeleteFromCache: false,
        canDownloadOnly: false,
    };
}

export function computeDetailState(input: DetailStateInput): DetailState {
    const {package: pkg, pickedVersion, fetchedVersions, expanded, filter, referencedVersion, cachedVersions} = input;

    const isReferenced = !!referencedVersion;
    const latest = pkg.latestAvailableVersion || pkg.version || fetchedVersions?.[0];
    const target = pickedVersion || latest;
    const targetCached = !!target && cachedVersions.includes(target);

    // The identity header already shows the title, only prefix the id when it differs, since many
    // packages use their id as their title.
    const idPrefix = pkg.packageId === pkg.title ? "" : `${pkg.packageId} · `;
    let contextLine: string;
    if (isReferenced) contextLine = `${idPrefix}referenced ${referencedVersion}`;
    else if (targetCached) contextLine = `${idPrefix}cached ${target}`;
    else contextLine = `${idPrefix}not cached`;

    let primaryAction: { label: string; version: string } | null = null;
    if (target) {
        if (!isReferenced) {
            primaryAction = {label: `Reference ${target}`, version: target};
        } else if (pickedVersion && pickedVersion !== referencedVersion) {
            primaryAction = {label: `Reference ${pickedVersion}`, version: pickedVersion};
        } else if (latest && latest !== referencedVersion) {
            primaryAction = {label: `Update to ${latest}`, version: latest};
        }
    }

    const canRemove = isReferenced;
    const canDeleteFromCache = !isReferenced && targetCached;
    const canDownloadOnly = !isReferenced && !targetCached;

    const depGroups = pkg.dependencies?.length ?? 0;
    const dependenciesLabel = depGroups
        ? `Dependencies · ${depGroups} ${depGroups === 1 ? "TFM" : "TFMs"}`
        : "Dependencies";

    let displayedVersions: DisplayVersion[];
    let hasMoreVersions: boolean;
    if (!fetchedVersions) {
        displayedVersions = [];
        hasMoreVersions = false;
    } else {
        let shown: string[];
        if (!expanded) {
            shown = collapsedVersions(fetchedVersions, referencedVersion, cachedVersions, pickedVersion);
            hasMoreVersions = shown.filter(v => fetchedVersions.includes(v)).length < fetchedVersions.length;
        } else {
            const f = filter.trim().toLowerCase();
            shown = f ? fetchedVersions.filter(v => v.toLowerCase().includes(f)) : fetchedVersions;
            hasMoreVersions = false;
        }
        displayedVersions = shown.map(v => ({
            version: v,
            latest: v === latest,
            referenced: v === referencedVersion,
            cached: cachedVersions.includes(v),
        }));
    }

    return {
        contextLine,
        displayedVersions,
        hasMoreVersions,
        dependenciesLabel,
        primaryAction,
        canRemove,
        canDeleteFromCache,
        canDownloadOnly,
    };
}

/**
 * The collapsed version list: the most recent versions, plus any referenced, cached, or explicitly-picked version.
 */
function collapsedVersions(versions: string[], referenced?: string, cached: string[] = [], picked?: string): string[] {
    const head = versions.slice(0, COLLAPSED_VERSION_COUNT);
    const pinned = new Set<string>(head);

    if (referenced) {
        pinned.add(referenced);
    }

    for (const c of cached) {
        pinned.add(c);
    }

    if (picked) {
        pinned.add(picked);
    }

    const inList = versions.filter(v => pinned.has(v));
    const notInList = [...pinned].filter(v => !versions.includes(v));
    return [...inList, ...notInList];
}

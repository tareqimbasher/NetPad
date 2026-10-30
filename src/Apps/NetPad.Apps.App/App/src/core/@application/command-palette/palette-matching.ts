import {fuzzyMatch, MatchSegment, toMatchSegments} from "@common";

export interface PaletteMatch {
    /** Ranks rows within a group. Only comparable between matches against the same query. */
    score: number;
    /** Whether the query reached the title, as opposed to only the detail. */
    onTitle: boolean;
    titleSegments: MatchSegment[];
    /** Undefined when the row carries no detail. */
    detailSegments?: MatchSegment[];
}

/**
 * Matches a query against a row: its title first, then, only if the title misses, the detail.
 */
export function matchPaletteItem(
    item: { title: string; detail?: string },
    query: string
): PaletteMatch | undefined {
    const onTitle = fuzzyMatch(item.title, query);

    if (onTitle) {
        return {
            score: onTitle.score,
            onTitle: true,
            titleSegments: toMatchSegments(item.title, onTitle.positions),
            detailSegments: item.detail ? toMatchSegments(item.detail, []) : undefined,
        };
    }

    if (!item.detail) return undefined;

    const onDetail = fuzzyMatch(item.detail, query);
    if (!onDetail) return undefined;

    return {
        score: onDetail.score,
        onTitle: false,
        titleSegments: toMatchSegments(item.title, []),
        detailSegments: toMatchSegments(item.detail, onDetail.positions),
    };
}

/**
 * Sorts title matches before detail-only matches, then by score.
 */
export function comparePaletteMatches(a: PaletteMatch, b: PaletteMatch): number {
    return Number(b.onTitle) - Number(a.onTitle) || b.score - a.score;
}

/**
 * Orders groups by their best row, so the group holding the best match comes first and gets the
 * selection. The sort is stable: groups whose best rows tie keep their order.
 */
export function orderGroupsByBestMatch<T>(groups: T[], bestOf: (group: T) => PaletteMatch): T[] {
    return [...groups].sort((a, b) => comparePaletteMatches(bestOf(a), bestOf(b)));
}

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
 * Orders matched rows: a row the query found by title comes before one it only found by matching detail,
 * and within each of those, the better match comes first.
 */
export function comparePaletteMatches(a: PaletteMatch, b: PaletteMatch): number {
    return Number(b.onTitle) - Number(a.onTitle) || b.score - a.score;
}

/**
 * Orders groups by their strongest row, so the section holding the best answer leads and the
 * selection lands on it. Rows never move between sections. The sort is stable, so sections whose
 * best rows tie keep the order their sources built them in.
 */
export function orderGroupsByBestMatch<T>(groups: T[], bestOf: (group: T) => PaletteMatch): T[] {
    return [...groups].sort((a, b) => comparePaletteMatches(bestOf(a), bestOf(b)));
}

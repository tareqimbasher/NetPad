import {fuzzyMatch, MatchSegment, toMatchSegments} from "@common";

export interface PaletteMatch {
    /** Ranks rows within a group. Only comparable between matches against the same query. */
    score: number;
    /** Whether the query reached the title, as opposed to only the detail or a search term. */
    onTitle: boolean;
    /** Whether only a search term matched. The term is then the detail. */
    onTerm: boolean;
    titleSegments: MatchSegment[];
    /** Undefined when the row carries no detail. */
    detailSegments?: MatchSegment[];
}

/**
 * Matches a query against a row: its title first, then the detail, then its search terms. Title and
 * detail match fuzzily; a term matches only where one of its words starts with the query.
 */
export function matchPaletteItem(
    item: { title: string; detail?: string; searchTerms?: string[] },
    query: string
): PaletteMatch | undefined {
    const onTitle = fuzzyMatch(item.title, query);

    if (onTitle) {
        return {
            score: onTitle.score,
            onTitle: true,
            onTerm: false,
            titleSegments: toMatchSegments(item.title, onTitle.positions),
            detailSegments: item.detail ? toMatchSegments(item.detail, []) : undefined,
        };
    }

    const onDetail = item.detail ? fuzzyMatch(item.detail, query) : undefined;

    if (item.detail && onDetail) {
        return {
            score: onDetail.score,
            onTitle: false,
            onTerm: false,
            titleSegments: toMatchSegments(item.title, []),
            detailSegments: toMatchSegments(item.detail, onDetail.positions),
        };
    }

    for (const term of item.searchTerms ?? []) {
        const positions = matchTermWord(term, query);
        if (!positions) continue;

        return {
            score: fuzzyMatch(term, query)?.score ?? 0,
            onTitle: false,
            onTerm: true,
            titleSegments: toMatchSegments(item.title, []),
            detailSegments: toMatchSegments(term, positions),
        };
    }

    return undefined;
}

/** The positions the query covers where a word of the term starts with it. */
function matchTermWord(term: string, query: string): number[] | undefined {
    const lowerTerm = term.toLowerCase();
    const lowerQuery = query.trim().toLowerCase();
    if (!lowerQuery) return undefined;

    for (let i = lowerTerm.indexOf(lowerQuery); i >= 0; i = lowerTerm.indexOf(lowerQuery, i + 1)) {
        if (i === 0 || lowerTerm[i - 1] === " ") {
            return Array.from({length: lowerQuery.length}, (_, offset) => i + offset);
        }
    }

    return undefined;
}

/**
 * Sorts title and search-term matches together by score, a title match first on a tie, then detail
 * matches by score.
 */
export function comparePaletteMatches(a: PaletteMatch, b: PaletteMatch): number {
    const ranksWithTitles = (match: PaletteMatch) => match.onTitle || match.onTerm ? 1 : 0;

    return ranksWithTitles(b) - ranksWithTitles(a)
        || b.score - a.score
        || Number(b.onTitle) - Number(a.onTitle);
}

/**
 * Orders groups by their best row, so the group holding the best match comes first and gets the
 * selection. The sort is stable: groups whose best rows tie keep their order.
 */
export function orderGroupsByBestMatch<T>(groups: T[], bestOf: (group: T) => PaletteMatch): T[] {
    return [...groups].sort((a, b) => comparePaletteMatches(bestOf(a), bestOf(b)));
}

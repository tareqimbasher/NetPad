/**
 * A run (section) of a matched string, flagged with whether the query reached it. Rendering the runs in
 * order reproduces the string with the matched characters marked.
 */
export interface MatchSegment {
    text: string;
    matched: boolean;
}

export interface FuzzyMatch {
    /** Higher is a better match. Only comparable between matches against the same query. */
    score: number;
    /** Indexes in the searched string that matched, in ascending order. */
    positions: number[];
}

const scoreCharacter = 1;
const bonusConsecutive = 8;
const bonusWordStart = 10;
const bonusFirstCharacter = 12;
const penaltyPerSkip = 1;
const penaltyMaxSkip = 12;

/** Determines if a new word starts at the specified {@link index} of the provided {@link text}. */
function isWordStart(text: string, index: number): boolean {
    if (index === 0) return true;

    const previous = text[index - 1];
    const current = text[index];

    if (!/[a-zA-Z0-9]/.test(previous)) return true;

    // A capital following a lower-case letter starts a word in camel/Pascal case.
    return current === current.toUpperCase()
        && current !== current.toLowerCase()
        && previous === previous.toLowerCase();
}

/**
 * Matches a query against a string as a subsequence: every query character must appear in the
 * string, in order, but not necessarily adjacently. Ranks matches by how tightly and how close to
 * word boundaries the characters landed, so "gts" matches on "Go to Script" better than it does
 * on "Toggle Developer Tools".
 *
 * Matching is case-insensitive and greedy from the left, with one refinement: when the greedy
 * position neither continues the previous match nor starts a word, but a later occurrence of the
 * same character does start one, the later occurrence is taken.
 *
 * An empty query matches everything with a score of 0. Returns undefined when the query is not a
 * subsequence of the string.
 */
export function fuzzyMatch(text: string, query: string): FuzzyMatch | undefined {
    if (!query) return {score: 0, positions: []};
    if (!text) return undefined;

    const lowerText = text.toLowerCase();
    const lowerQuery = query.toLowerCase();

    const positions: number[] = [];
    let score = 0;
    let searchFrom = 0;

    for (let q = 0; q < lowerQuery.length; q++) {
        const queryChar = lowerQuery[q];
        if (queryChar === " ") continue;

        let index = lowerText.indexOf(queryChar, searchFrom);
        if (index < 0) return undefined;

        const previous = positions.length ? positions[positions.length - 1] : -1;

        if (index !== previous + 1 && !isWordStart(text, index)) {
            const atWordStart = findWordStartOccurrence(text, lowerText, queryChar, index + 1);
            if (atWordStart >= 0) index = atWordStart;
        }

        const skipped = index - previous - 1;

        score += scoreCharacter;
        if (previous >= 0 && skipped === 0) score += bonusConsecutive;
        if (isWordStart(text, index)) score += bonusWordStart;
        if (index === 0) score += bonusFirstCharacter;
        score -= Math.min(skipped * penaltyPerSkip, penaltyMaxSkip);

        positions.push(index);
        searchFrom = index + 1;
    }

    // Shorter strings carrying the same match are the better hit ("Save" over "Save All" for "sa").
    score -= text.length * 0.05;

    return {score, positions};
}

function findWordStartOccurrence(text: string, lowerText: string, queryChar: string, from: number): number {
    for (let i = lowerText.indexOf(queryChar, from); i >= 0; i = lowerText.indexOf(queryChar, i + 1)) {
        if (isWordStart(text, i)) return i;
    }

    return -1;
}

/**
 * Splits a string into matched and unmatched runs, for rendering a match with its hit characters
 * marked. Positions must be ascending and within the string.
 */
export function toMatchSegments(text: string, positions: readonly number[]): MatchSegment[] {
    if (!positions.length) return text ? [{text, matched: false}] : [];

    const segments: MatchSegment[] = [];
    let cursor = 0;

    for (let i = 0; i < positions.length;) {
        const start = positions[i];

        let end = start + 1;
        while (i + 1 < positions.length && positions[i + 1] === end) {
            end++;
            i++;
        }
        i++;

        if (start > cursor) segments.push({text: text.substring(cursor, start), matched: false});
        segments.push({text: text.substring(start, end), matched: true});
        cursor = end;
    }

    if (cursor < text.length) segments.push({text: text.substring(cursor), matched: false});

    return segments;
}

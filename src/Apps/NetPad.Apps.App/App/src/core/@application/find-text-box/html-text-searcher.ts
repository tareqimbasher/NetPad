/**
 * Finds occurrences of text within elements and wraps each match in a
 * `span.text-search-result`.
 *
 * Matching runs against each element's `innerHTML`, not its text content, so the search text is
 * HTML-encoded before comparison and matches that fall inside an HTML entity are skipped.
 * Elements that contain child elements (other than `<br>`) are left untouched.
 */
export class HtmlTextSearcher {
    /**
     * Searches for text and highlights all matches.
     * @param searchElement The root element to search within.
     * @param searchText The text to search for.
     * @param searchableChildElementsQuerySelector Selects the descendants of the root element whose
     * contents are searched.
     * @returns The elements wrapping each match, in document order.
     */
    public search(searchElement: Element, searchText: string, searchableChildElementsQuerySelector: string): HTMLElement[] {
        this.removeAllSearchResults(searchElement);
        if (!searchText) {
            return [];
        }

        searchText = HtmlTextSearcher.normalizeSearchText(searchText);

        const searchable = Array.from(searchElement.querySelectorAll(searchableChildElementsQuerySelector)) as HTMLElement[];
        const sets: [HTMLElement, string][] = [];

        for (let iSearchableElement = 0; iSearchableElement < searchable.length; iSearchableElement++) {
            const element = searchable[iSearchableElement];

            const containsAnyElementsBesidesBreaks = element.childElementCount > 0 &&
                Array.from(element.children).some(x => x.tagName !== "BR");

            if (containsAnyElementsBesidesBreaks) {
                continue;
            }

            const matchingIndexes = HtmlTextSearcher.findMatchingIndexes(element.innerHTML, searchText);

            if (matchingIndexes.length === 0) {
                continue;
            }

            const originalHtml = element.innerHTML;
            let ixOriginalHtml = 0;
            let newHtml = "";

            for (let i = 0; i < matchingIndexes.length; i++) {
                const ixMatch = matchingIndexes[i];

                if (ixOriginalHtml < ixMatch) {
                    newHtml += originalHtml.substring(ixOriginalHtml, ixMatch);
                    ixOriginalHtml = ixMatch;
                }

                newHtml +=
                    '<span class="text-search-result">'
                    + originalHtml.substring(ixMatch, ixMatch + searchText.length)
                    + '</span>';

                ixOriginalHtml += searchText.length;

                // On last iteration, add the rest of the original html
                if (i == (matchingIndexes.length - 1)) {
                    newHtml += originalHtml.substring(ixMatch + searchText.length);
                }
            }

            sets.push([element, newHtml]);
        }

        for (const set of sets) {
            set[0].innerHTML = set[1];
        }

        return Array.from(searchElement.querySelectorAll(".text-search-result")) as HTMLElement[];
    }

    private removeAllSearchResults(searchElement: Element) {
        searchElement.querySelectorAll(".text-search-result")
            .forEach(el => {
                el.replaceWith(el.textContent || "");
            });
    }

    private static normalizeSearchText(searchText: string) {
        return searchText.toLowerCase()
            .replaceAll("&", "&amp;")
            .replaceAll(" ", "&nbsp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            ;
    }

    /**
     * Finds all indexes where the text contains the search text.
     * @param htmlSource The html to search within
     * @param searchHtml The html to search for
     */
    private static findMatchingIndexes(htmlSource: string, searchHtml: string): number[] {
        htmlSource = htmlSource.toLowerCase();
        const searchTextFoundAtIndexes: number[] = [];

        // We need to check if search text is text that can be found in a special symbol.
        // Example: searching for 'b' might be found in special symbol '&nbsp;'
        const searchHtmlMightBeFoundInSpecialSymbols =
            searchHtml.length < 4
            || !(searchHtml.startsWith("&") && searchHtml.endsWith(";"))

        let index = -1;
        do {
            index = htmlSource.indexOf(searchHtml, index + 1);
            if (index < 0) break;

            if (searchHtmlMightBeFoundInSpecialSymbols
                && HtmlTextSearcher.hasSpecialAmpBefore(htmlSource, index)
                && HtmlTextSearcher.hasSemiColonAfter(htmlSource, index)) continue;

            searchTextFoundAtIndexes.push(index);
        } while (index >= 0)

        return searchTextFoundAtIndexes;
    }

    private static hasSpecialAmpBefore(searchString: string, startIndex: number) {
        for (let i = 0; i < 5; i++) {
            const iCharToCheck = startIndex - i;
            if (iCharToCheck < 0) return false;

            const char = searchString[iCharToCheck];
            if (char === ";") return false;
            if (char === "&") return true;
        }

        return false;
    }

    private static hasSemiColonAfter(searchString: string, startIndex: number) {
        const maxIndex = searchString.length;
        for (let i = 0; i < 4; i++) {
            const iCharToCheck = startIndex + i;
            if (iCharToCheck > maxIndex) return false;

            const char = searchString[startIndex + i];
            if (char === "&") return false;
            if (char === ";") return true;
        }

        return false;
    }
}

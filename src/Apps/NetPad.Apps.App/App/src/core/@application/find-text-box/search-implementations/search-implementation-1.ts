import {SearchImplementation} from "@application/find-text-box/search-implementations/search-implementation";

export class SearchImplementation1 extends SearchImplementation {
    private removeAllSearchResults(searchElement: Element) {
        searchElement.querySelectorAll(".text-search-result")
            .forEach(el => {
                el.replaceWith(el.textContent || "");
            });
    }

    public search(searchElement: Element, searchText: string, searchableChildElementsQuerySelector: string): HTMLElement[] {
        this.removeAllSearchResults(searchElement);
        if (!searchText) {
            return [];
        }

        searchText = this.normalizeSearchText(searchText);

        const searchable = Array.from(searchElement.querySelectorAll(searchableChildElementsQuerySelector)) as HTMLElement[];
        const sets: [HTMLElement, string][] = [];

        for (let iSearchableElement = 0; iSearchableElement < searchable.length; iSearchableElement++) {
            const element = searchable[iSearchableElement];

            const containsAnyElementsBesidesBreaks = element.childElementCount > 0 &&
                Array.from(element.children).some(x => x.tagName !== "BR");

            if (containsAnyElementsBesidesBreaks) {
                continue;
            }

            const matchingIndexes = this.findMatchingIndexes(element.innerHTML, searchText);

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
}

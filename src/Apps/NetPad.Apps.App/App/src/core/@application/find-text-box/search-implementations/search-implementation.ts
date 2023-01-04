export abstract class SearchImplementation {
    protected normalizeSearchText(searchText: string) {
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
    protected findMatchingIndexes(htmlSource: string, searchHtml: string): number[] {
        const searchTextFoundAtIndexes: number[] = [];

        // We need to check if search text is text that can be found in a special symbol.
        // Example: searching for 'b' might be found in special symbol '&nbsp;'
        const searchHtmlMightBeFoundInSpecialSymbols = searchHtml.length < 4 || !(searchHtml.startsWith("&") && searchHtml.endsWith(";"))

        let index = -1;
        do {
            index = htmlSource.indexOf(searchHtml, index + 1);
            if (index < 0) break;

            if (searchHtmlMightBeFoundInSpecialSymbols
                && SearchImplementation.hasSpecialAmpBefore(htmlSource, index)
                && SearchImplementation.hasSemiColonAfter(htmlSource, index)) continue;

            searchTextFoundAtIndexes.push(index);
        } while (index >= 0)

        return searchTextFoundAtIndexes;
    }

    private static hasSpecialAmpBefore(searchString: string, startIndex: number) {
        for (let i = 0; i < 4; i++) {
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

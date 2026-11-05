/**
 * Finds text in the shown parts of an element and highlights each match. A match may cross elements
 * within a search unit, so it is highlighted by one `span.text-search-result` per text node it
 * touches.
 */
export class HtmlTextSearcher {
    /**
     * Highlights every match of the text, replacing the highlights of the previous search.
     * @param root The element to search within.
     * @param searchText The text to find. Case is ignored, and a no-break space matches a space.
     * @param unitSelector Selects the elements searched as separate units. A match never crosses
     * from one unit into another, or past a line break. A unit inside another unit is searched on
     * its own, and a unit that is not shown is not searched.
     * @returns The matches in document order, each as the spans that highlight its parts.
     */
    public search(root: ParentNode, searchText: string, unitSelector: string): HTMLElement[][] {
        this.clear(root);

        const query = HtmlTextSearcher.fold(searchText);
        if (!query) {
            return [];
        }

        const units = Array.from(root.querySelectorAll(unitSelector));
        const unitSet = new Set<Element>(units);
        const parts: IMatchPart[] = [];
        let matchCount = 0;

        for (const unit of units) {
            const partsBefore = parts.length;
            const matchesBefore = matchCount;

            for (const line of HtmlTextSearcher.lines(unit, unitSet)) {
                matchCount = HtmlTextSearcher.findInLine(line, query, matchCount, parts);
            }

            // Only units that matched are checked, which on long output is a small share of them.
            if (matchCount > matchesBefore && !HtmlTextSearcher.isShown(unit)) {
                parts.length = partsBefore;
                matchCount = matchesBefore;
            }
        }

        const matches: HTMLElement[][] = Array.from({length: matchCount}, () => []);

        // Wrapping a node's parts from its last to its first keeps the earlier offsets valid.
        for (let i = parts.length - 1; i >= 0; i--) {
            const part = parts[i];
            matches[part.match].unshift(HtmlTextSearcher.highlight(part));
        }

        return matches;
    }

    private clear(root: ParentNode) {
        const parents = new Set<Node>();

        for (const highlight of Array.from(root.querySelectorAll(".text-search-result"))) {
            if (highlight.parentNode) {
                parents.add(highlight.parentNode);
            }

            highlight.replaceWith(...Array.from(highlight.childNodes));
        }

        for (const parent of parents) {
            parent.normalize();
        }
    }

    private static isShown(unit: Element) {
        // Without checkVisibility (WebKit before Safari 17.4), every unit counts as shown.
        return typeof unit.checkVisibility !== "function" || unit.checkVisibility();
    }

    /**
     * The unit's text nodes in document order, split into lines at each `<br>` and at each unit
     * nested inside it.
     */
    private static lines(unit: Element, units: Set<Element>): Text[][] {
        const lines: Text[][] = [[]];

        const visit = (parent: Node) => {
            for (let child = parent.firstChild; child; child = child.nextSibling) {
                if (child.nodeType === Node.TEXT_NODE) {
                    lines[lines.length - 1].push(child as Text);
                } else if (child.nodeType === Node.ELEMENT_NODE) {
                    if (child.nodeName === "BR" || units.has(child as Element)) {
                        lines.push([]);
                    } else {
                        visit(child);
                    }
                }
            }
        };

        visit(unit);

        return lines.filter(line => line.length > 0);
    }

    private static findInLine(line: Text[], query: string, matchCount: number, parts: IMatchPart[]): number {
        const text = HtmlTextSearcher.fold(line.map(node => node.data).join(""));

        let index = text.indexOf(query);
        if (index < 0) {
            return matchCount;
        }

        const starts: number[] = [];
        let start = 0;
        for (const node of line) {
            starts.push(start);
            start += node.data.length;
        }

        let iNode = 0;

        while (index >= 0) {
            const end = index + query.length;

            while (starts[iNode] + line[iNode].data.length <= index) {
                iNode++;
            }

            for (let i = iNode; i < line.length && starts[i] < end; i++) {
                const from = Math.max(index, starts[i]) - starts[i];
                const to = Math.min(end, starts[i] + line[i].data.length) - starts[i];

                if (to > from) {
                    parts.push({node: line[i], from, to, match: matchCount});
                }
            }

            matchCount++;
            index = text.indexOf(query, end);
        }

        return matchCount;
    }

    private static highlight(part: IMatchPart): HTMLElement {
        let text = part.node;

        if (part.to < text.length) {
            text.splitText(part.to);
        }

        if (part.from > 0) {
            text = text.splitText(part.from);
        }

        const span = document.createElement("span");
        span.className = "text-search-result";
        text.replaceWith(span);
        span.appendChild(text);

        return span;
    }

    /**
     * Folds text for comparison without changing its length, so an index into the folded text is an
     * index into the original.
     */
    private static fold(text: string): string {
        const lower = text.toLowerCase();

        const folded = lower.length === text.length
            ? lower
            : Array.from(text, char => {
                const lowerChar = char.toLowerCase();
                return lowerChar.length === char.length ? lowerChar : char;
            }).join("");

        return folded.replaceAll("\u00A0", " ");
    }
}

interface IMatchPart {
    node: Text;
    from: number;
    to: number;
    match: number;
}

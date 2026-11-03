import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {KeyCombo, ViewModelBase} from "@application";

import {HtmlTextSearcher} from "@application/find-text-box/html-text-searcher";
import {shownContentChangedEvent} from "@application/find-text-box/shown-content-changed-event";

interface ITextSearchResult {
    searchText: string;
    matches: HTMLElement[][];
    currentResultNumber: number;
}

interface ITextSearchableArea {
    element: HTMLElement;
    searchableChildrenQuerySelector: string;
    searchResults?: ITextSearchResult | null;
}

/**
 * A component to find text in an element. Add elements that can be searched to make them available.
 * Then set the "current" searchable element.
 */
export class FindTextBox extends ViewModelBase {
    private readonly keyBinding = new KeyCombo({primary: true, key: "F"});
    private searchText = "";
    private show: boolean;
    private txtSearch: HTMLInputElement;
    private searcher = new HtmlTextSearcher();

    private searchableElements = new Map<HTMLElement, ITextSearchableArea>();
    private current?: ITextSearchableArea;

    private ctrlFHandler = (ev: KeyboardEvent) => {
        if (this.keyBinding.matches(ev)) {
            const selectedText = window.getSelection()?.toString();
            if (selectedText) {
                this.searchText = selectedText;
            }

            this.show = true;
            setTimeout(() => this.txtSearch.focus(), 100);

            ev.preventDefault();
            ev.stopPropagation();
        }
    };

    private shownContentChangedHandler = (ev: Event) => {
        if (this.show && this.current?.element === ev.currentTarget) {
            this.search(this.searchText);
        }
    };

    constructor(private readonly element: HTMLElement, @ILogger logger: ILogger) {
        super(logger);
    }

    private attached() {
        if (!this.element.parentElement) {
            throw new Error("Expected the FindTextBox to be nested inside an element");
        }

        // Needed because this element is absolutely positioned
        this.element.parentElement.style.position = "relative";

        const searchTextBoxKeyHandler = (ev: KeyboardEvent) => {
            if (ev.key === "Enter") {
                ev.shiftKey ? this.goToPreviousResult() : this.goToNextResult();
            }
            if (ev.key === "Escape") {
                this.close();
            }
        };

        this.txtSearch.addEventListener("keydown", searchTextBoxKeyHandler);
        this.addDisposable(() => this.txtSearch.removeEventListener("keydown", searchTextBoxKeyHandler));
    }

    public registerSearchableElement(element: HTMLElement, searchableChildrenQuerySelector: string) {
        if (this.searchableElements.has(element)) {
            return;
        }

        if (!element.hasAttribute("tabIndex")) {
            // Needed so keydown event is tracked on element
            element.tabIndex = 0;
        }

        element.addEventListener("keydown", this.ctrlFHandler);
        element.addEventListener(shownContentChangedEvent, this.shownContentChangedHandler);
        this.searchableElements.set(element, {element, searchableChildrenQuerySelector});
        this.addDisposable(() => this.unregisterSearchableElement(element));
    }

    public unregisterSearchableElement(element: HTMLElement) {
        this.searchableElements.delete(element);
        element.removeEventListener("keydown", this.ctrlFHandler);
        element.removeEventListener(shownContentChangedEvent, this.shownContentChangedHandler);
        element.classList.add("hide-text-search-results");
    }

    public setCurrent(element: HTMLElement) {
        if (!this.searchableElements.has(element)) {
            throw new Error("Could not find searchable element");
        }

        this.current = this.searchableElements.get(element);

        if (this.show) {
            element.classList.remove("hide-text-search-results");

            const cachedSearchResults = this.current?.searchResults;

            if (cachedSearchResults?.matches.length && cachedSearchResults?.searchText === this.searchText) {
                this.goToResult(cachedSearchResults.currentResultNumber);
            } else {
                this.search(this.searchText);
            }
        }
    }

    @watch<FindTextBox>(vm => vm.searchText)
    public search(searchText: string) {
        if (!this.current) {
            return;
        }

        const area = this.current;
        const element = area.element;

        if ((element.textContent?.length || 0) > 1000000 && searchText.length > 0 && searchText.length < 2) {
            this.logger.debug("search text is too short (less than 2) and searchable text is too long");
            return;
        }

        if ((element.textContent?.length || 0) > 1500000 && searchText.length > 0 && searchText.length < 3) {
            this.logger.debug("search text is too short (less than 3) and searchable text is too long");
            return;
        }

        try {
            const results = this.searcher.search(element, searchText, area.searchableChildrenQuerySelector);

            area.searchResults = {
                matches: results,
                searchText: searchText,
                currentResultNumber: 0
            };
        } catch (ex) {
            this.logger.error("Error while searching", ex);
        } finally {
            if (area.searchResults?.matches.length) {
                this.goToNextResult();
            }
        }
    }

    public goToNextResult() {
        const results = this.current?.searchResults;
        if (!results) {
            return;
        }

        const resultNumber =  results.matches.length < (results.currentResultNumber + 1)
            ? 1
            : results.currentResultNumber + 1;

        this.goToResult(resultNumber);
    }

    public goToPreviousResult() {
        const results = this.current?.searchResults;
        if (!results) {
            return;
        }

        const resultNumber =  results.currentResultNumber === 1
            ? results.matches.length
            : results.currentResultNumber - 1;

        this.goToResult(resultNumber);
    }

    public goToResult(resultNumber: number) {
        const results = this.current?.searchResults;
        if (!results) {
            return;
        }

        if (results.matches.length === 0) {
            results.currentResultNumber = 0;
            return;
        }

        const resultNumberBeforeChange = results.currentResultNumber;

        results.currentResultNumber = resultNumber;

        if (resultNumberBeforeChange > 0) {
            for (const part of results.matches[resultNumberBeforeChange - 1]) {
                part.classList.remove("text-search-active");
            }
        }

        const active = results.matches[results.currentResultNumber - 1];
        for (const part of active) {
            part.classList.add("text-search-active");
        }
        active[0].scrollIntoView({block: "center", inline: "nearest"});
    }

    public close() {
        this.show = false;

        for (const element of this.searchableElements.keys()) {
            element.classList.add("hide-text-search-results");
            element.focus();
        }
    }
}

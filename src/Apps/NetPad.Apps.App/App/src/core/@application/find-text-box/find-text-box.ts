import {bindable, ILogger} from "aurelia";
import {ViewModelBase} from "@application";
import {KeyCode} from "@common";
import {observable} from "@aurelia/runtime";
import {SearchImplementation1} from "@application/find-text-box/search-implementations/search-implementation-1";
import {FindTextBoxOptions} from "@application/find-text-box/find-text-box-options";


export class FindTextBox extends ViewModelBase {
    @bindable options: FindTextBoxOptions;
    @observable private searchText: string | undefined;
    private show: boolean;
    private txtSearch: HTMLInputElement;
    private results: HTMLElement[] = [];
    private currentResultNumber = 0;

    constructor(private readonly element: HTMLElement, @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        if (!this.element.parentElement) {
            throw new Error("Expected the FindTextBox to be nested inside an element");
        }

        // Needed because this element is absolutely positioned
        this.element.parentElement.style.position = "relative";

        // Needed so keydown event is tracked on root element
        this.options.rootElement.tabIndex = 1;

        const ctrlFHandler = (ev: KeyboardEvent) => {
            if (ev.ctrlKey && ev.code == KeyCode.KeyF) {
                this.show = true;
                const selectedText = window.getSelection()?.toString();
                if (selectedText) {
                    this.searchText = selectedText;
                }

                this.options.rootElement.classList.remove("hide-text-search-results");
                setTimeout(() => this.txtSearch.focus(), 100);
                ev.preventDefault();
                ev.stopPropagation();
            }
        };

        const searchTextBoxKeyHandler = (ev: KeyboardEvent) => {
            if (ev.code === KeyCode.Enter) {
                ev.shiftKey ? this.goToPreviousResult() : this.goToNextResult();
            }
            if (ev.code === KeyCode.Escape) {
                this.close();
            }
        };

        this.options.rootElement.addEventListener("keydown", ctrlFHandler);
        this.addDisposable(() => this.options.rootElement.removeEventListener("keydown", ctrlFHandler));

        this.txtSearch.addEventListener("keydown", searchTextBoxKeyHandler);
        this.addDisposable(() => this.txtSearch.removeEventListener("keydown", searchTextBoxKeyHandler));
    }


    private searchTextChanged(searchText: string) {
        if ((this.options.rootElement.textContent?.length || 0) > 1000000 && searchText.length > 0 && searchText.length < 2) {
            this.logger.debug("search text is too short (less than 2) and searchable text is too long");
            return;
        }

        if ((this.options.rootElement.textContent?.length || 0) > 1500000 && searchText.length > 0 && searchText.length < 3) {
            this.logger.debug("search text is too short (less than 3) and searchable text is too long");
            return;
        }

        try {
            this.currentResultNumber = 0;
            this.results = new SearchImplementation1(this.options).search(searchText);
        } catch (ex) {
            this.logger.error("Error while searching", ex);
        } finally {
            if (this.results.length) this.goToNextResult();
        }
    }

    private goToNextResult() {
        this.goToResult(() =>
            this.results.length < (this.currentResultNumber + 1)
                ? 1
                : this.currentResultNumber + 1);
    }

    private goToPreviousResult() {
        this.goToResult(() =>
            this.currentResultNumber === 1
                ? this.results.length
                : this.currentResultNumber - 1);
    }

    private goToResult(nextResultNumber: () => number) {
        if (this.results.length === 0) {
            this.currentResultNumber = 0;
            return;
        }

        const resultNumberBeforeChange = this.currentResultNumber;

        this.currentResultNumber = nextResultNumber();

        if (resultNumberBeforeChange > 0)
            this.results[resultNumberBeforeChange - 1].classList.remove("text-search-active");

        const active = this.results[this.currentResultNumber - 1];
        active.classList.add("text-search-active");
        active.scrollIntoView({block: "nearest", inline: "nearest"});
    }

    private close() {
        this.show = false;
        this.options.rootElement.classList.add("hide-text-search-results");
        this.options.rootElement.focus();
    }
}

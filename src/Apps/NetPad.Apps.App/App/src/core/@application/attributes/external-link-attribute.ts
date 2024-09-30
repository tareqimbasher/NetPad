import {System} from "@common";
import {ViewModelBase} from "@application";
import {ILogger} from "aurelia";

/**
 * Used to mark an anchor tag to be opened in a new browser/window.
 * Usage: <a href="https://google.com" external-link></a>
 */
export class ExternalLinkCustomAttribute extends ViewModelBase {

    constructor(private readonly element: Element, @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        const handler = (event: Event) => {
            if (this.element.tagName !== "A" || !this.element.getAttribute("href"))
                return true;

            this.openLinkExternally(event);
            return false;
        };

        this.element.addEventListener("click", handler);
        this.addDisposable(() => this.element.removeEventListener("click", handler));
    }

    private openLinkExternally(event: Event) {
        event.preventDefault();

        const href = this.element.getAttribute("href");
        if (!href) {
            return;
        }

        System.openUrlInBrowser(href);
    }
}

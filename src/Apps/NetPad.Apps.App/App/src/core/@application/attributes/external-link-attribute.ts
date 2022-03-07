import {System} from "@common";
import {ViewModelBase} from "@application/view-model-base";
import {ILogger} from "aurelia";

export class ExternalLinkCustomAttribute extends ViewModelBase {

    constructor(private readonly element: Element, @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        const handler = async (event: Event) => {
            if (this.element.tagName !== "A" || !this.element.getAttribute("href"))
                return;

            await this.openLinkExternally(event);
            return false;
        };

        this.element.addEventListener("click", handler);
        this.disposables.push(() => this.element.removeEventListener("click", handler));
    }

    private async openLinkExternally(event: Event): Promise<void> {
        event.preventDefault();

        const href = this.element.getAttribute("href");
        if (!href) return;

        await System.openUrlInBrowser(href);
    }
}

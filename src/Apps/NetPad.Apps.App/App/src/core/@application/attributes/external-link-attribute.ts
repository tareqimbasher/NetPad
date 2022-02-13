import {System} from "@common";

export class ExternalLinkCustomAttribute {
    private disposables: (() => void)[] = [];

    constructor(private readonly element: Element) {
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

    public detaching() {
        this.disposables.forEach(d => d());
    }

    private async openLinkExternally(event: Event): Promise<void> {
        event.preventDefault();

        const href = this.element.getAttribute("href");
        if (!href) return;

        await System.openUrlInBrowser(href);
    }
}

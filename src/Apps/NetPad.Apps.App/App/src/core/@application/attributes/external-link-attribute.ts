import {System} from "@common";

export class ExternalLinkCustomAttribute {
    private disposables: (() => void)[] = [];

    constructor(private readonly element: Element) {
    }

    public attached() {
        if (this.element.tagName !== "A" || !this.element.getAttribute("href"))
            return;

        const handler = (event: Event) => this.openLinkExternally(event);
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

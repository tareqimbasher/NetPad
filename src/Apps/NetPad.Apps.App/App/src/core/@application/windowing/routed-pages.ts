import {IWindowDestinations} from "./iwindow-destinations";
import {WindowParams} from "./window-params";

/**
 * The pages of a window that shows one page at a time, and which one is showing. The window opens on the page
 * its `tab` param names, or the first page.
 */
export abstract class RoutedPages<TPage extends { route: string }> implements IWindowDestinations {
    public abstract readonly identityParams: string[];

    public selectedPage: TPage;

    protected constructor(public readonly pages: readonly TPage[]) {
        this.selectedPage = this.pageAt(WindowParams.get("tab")) ?? pages[0];
    }

    public goTo(route: string) {
        const page = this.pageAt(route);
        if (page) {
            this.selectedPage = page;
        }
    }

    private pageAt(route: string | null): TPage | undefined {
        return this.pages.find(p => p.route === route);
    }
}

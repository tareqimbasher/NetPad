import {bindable} from "aurelia";
import {ViewModelBase} from "@application";
import {Util} from "@common";

export class NavigationControls extends ViewModelBase {
    @bindable outputElement: HTMLElement
    public showNavigationControls: boolean;
    public showTop = true;
    public showBottom = true;

    public attached() {
        const resizeObserver = new ResizeObserver(() => this.showOrHideNavigationControls());
        resizeObserver.observe(this.outputElement);
        this.addDisposable(() => resizeObserver.disconnect());

        const scrollHandler = Util.debounce(this, (event: Event) => this.showOrHideNavigationControls(), 100, false);
        this.outputElement.addEventListener("scroll", scrollHandler);
        this.addDisposable(() => this.outputElement.removeEventListener("scroll", scrollHandler));
    }

    private showOrHideNavigationControls = Util.debounce(this, () => {
        const groups = Array.from(this.outputElement.querySelectorAll(".group"));
        if (!groups.length) {
            this.showNavigationControls = false;
        }
        else {
            const output = this.outputElement.getBoundingClientRect();
            const first = groups[0].getBoundingClientRect();
            const last = groups[groups.length - 1].getBoundingClientRect();

            const firstTopInView = first.top >= output.top;
            const lastBottomInView = last.bottom <= output.bottom;
            const allOutputInView = firstTopInView && lastBottomInView;

            this.showTop = !firstTopInView;
            this.showBottom = !lastBottomInView;
            this.showNavigationControls = !allOutputInView;
        }
    }, 100);

    private navigateTop() {
        this.outputElement.scrollTop = 0;
    }

    private navigateUp() {
        const viewport = this.outputElement;
        const groups = Array.from(viewport.querySelectorAll(".group"));
        const groupsInView = groups.filter(x => this.elementIsVisibleInViewport(viewport, x, true));

        if (!groupsInView.length) {
            return;
        }

        const firstInViewGroup = groupsInView[0];
        const isTopInView =
            firstInViewGroup.getBoundingClientRect().top >= viewport.getBoundingClientRect().top - 5;

        if (isTopInView) {
            const ix = groups.indexOf(firstInViewGroup);
            if (ix > 0) {
                groups[ix - 1].scrollIntoView({behavior: "smooth"});
            }
        } else {
            // Go to the top of the view
            firstInViewGroup.scrollIntoView({behavior: "smooth"});
        }
    }

    private navigateDown() {
        const viewport = this.outputElement;
        const groups = Array.from(viewport.querySelectorAll(".group"));
        const groupsInView = groups.filter(x => this.elementIsVisibleInViewport(viewport, x, true));

        if (!groupsInView.length) {
            return;
        }

        const viewportTop = viewport.getBoundingClientRect().top;
        const firstInViewGroup = groupsInView.find(x => x.getBoundingClientRect().top >= (viewportTop - 3));
        if (!firstInViewGroup) return;

        const ix = groups.indexOf(firstInViewGroup);
        if ((ix + 1) < groups.length) {
            groups[ix + 1].scrollIntoView({behavior: "smooth"});
        }
    }

    private navigateBottom() {
        this.outputElement.scrollTop = this.outputElement.scrollHeight;
    }

    private elementIsVisibleInViewport(viewportElement: Element, el: Element, partiallyVisible = false) {
        let {top, left, bottom, right} = el.getBoundingClientRect();
        const viewport = viewportElement.getBoundingClientRect();

        const elStyle = window.getComputedStyle(el);
        top -= Number(elStyle.marginTop.replace("px", ""));
        bottom += Number(elStyle.marginBottom.replace("px", ""));

        const isTopInView = top >= viewport.top && top <= viewport.bottom;
        const isBottomInView = bottom <= viewport.bottom && bottom >= viewport.top
        const isMiddleInView = !isTopInView && !isBottomInView && top < viewport.top && bottom > viewport.bottom;

        return partiallyVisible
            ? (isTopInView) || (isBottomInView) || isMiddleInView
            : top >= viewport.top && left >= viewport.left && bottom <= viewport.bottom && right <= viewport.right;
    };
}

export class NavigationControls {
    public showNavigationControls = true;
    public showTop = true;
    public showBottom = true;

    constructor(private readonly dumpContainer: Element) {
        // TODO: disabled because debouncing showOrHideNavigationControls causes issue where nav controls
        // won't show at all till output has entirely finished. Until fixed properly, always show
        // this.watchNavigationControls();
    }

    private get viewport(): Element {
        return this.dumpContainer.parentElement!;
    }

    public navigateTop() {
        this.viewport.scrollTop = 0;
    }

    public navigateUp() {
        const viewport = this.viewport;
        const viewportRect = viewport.getBoundingClientRect();

        const groups = Array.from(viewport.querySelectorAll(".group"));
        if (!groups.length) {
            return;
        }

        const groupsInView = groups.filter(x => this.elementIsVisibleInViewport(viewport, viewportRect, x, true));
        if (!groupsInView.length) {
            return;
        }

        const firstInViewGroup = groupsInView[0];
        const isTopInView =
            firstInViewGroup.getBoundingClientRect().top >= viewportRect.top - 5;

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

    public navigateDown() {
        const viewport = this.viewport;
        const viewportRect = viewport.getBoundingClientRect();

        const groups = Array.from(viewport.querySelectorAll(".group"));
        if (!groups.length) {
            return;
        }

        const groupsInView = groups.filter(x => this.elementIsVisibleInViewport(viewport, viewportRect, x, true));
        if (!groupsInView.length) {
            return;
        }

        const viewportTop = viewportRect.top;
        const firstInViewGroup = groupsInView.find(x => x.getBoundingClientRect().top >= (viewportTop - 3));
        if (!firstInViewGroup) return;

        const ix = groups.indexOf(firstInViewGroup);
        if ((ix + 1) < groups.length) {
            groups[ix + 1].scrollIntoView({behavior: "smooth"});
        }
    }

    public navigateBottom() {
        this.viewport.scrollTop = this.viewport.scrollHeight;
    }

    private elementIsVisibleInViewport(viewportElement: Element, viewportRect: DOMRect, el: Element, partiallyVisible = false) {
        const elRect = el.getBoundingClientRect();
        let top = elRect.top;
        let bottom = elRect.bottom;
        const right = elRect.right;
        const left = elRect.left;

        const elStyle = window.getComputedStyle(el);
        top -= Number(elStyle.marginTop.replace("px", ""));
        bottom += Number(elStyle.marginBottom.replace("px", ""));

        const isTopInView = top >= viewportRect.top && top <= viewportRect.bottom;
        const isBottomInView = bottom <= viewportRect.bottom && bottom >= viewportRect.top
        const isMiddleInView = !isTopInView && !isBottomInView && top < viewportRect.top && bottom > viewportRect.bottom;

        return partiallyVisible
            ? (isTopInView) || (isBottomInView) || isMiddleInView
            : top >= viewportRect.top && left >= viewportRect.left && bottom <= viewportRect.bottom && right <= viewportRect.right;
    }

    // private watchNavigationControls() {
    //     const resizeObserver = new ResizeObserver(() => this.showOrHideNavigationControls());
    //     resizeObserver.observe(this.outputElement);
    //     this.addDisposable(() => resizeObserver.disconnect());
    //
    //     const scrollHandler = () => this.showOrHideNavigationControls();
    //     this.outputElement.addEventListener("scroll", scrollHandler);
    //     this.addDisposable(() => this.outputElement.removeEventListener("scroll", scrollHandler));
    // }

    // private showOrHideNavigationControls = Util.debounce(this, () => {
    //     const groups = Array.from(this.outputElement.querySelectorAll(".group"));
    //     if (!groups.length) {
    //         this.showNavigationControls = false;
    //     }
    //     else {
    //         const output = this.outputElement.getBoundingClientRect();
    //         const first = groups[0].getBoundingClientRect();
    //         const last = groups[groups.length - 1].getBoundingClientRect();
    //
    //         const firstTopInView = first.top >= output.top;
    //         const lastBottomInView = last.bottom <= output.bottom;
    //         const allOutputInView = firstTopInView && lastBottomInView;
    //
    //         this.showTop = !firstTopInView;
    //         this.showBottom = !lastBottomInView;
    //         this.showNavigationControls = !allOutputInView;
    //     }
    // }, 100);
}

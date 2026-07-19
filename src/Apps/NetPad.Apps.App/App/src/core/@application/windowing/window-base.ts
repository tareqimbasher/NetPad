import {ILogger, IObserverLocator, resolve} from "aurelia";
import {Settings, ViewModelBase} from "@application";
import {ThemeBootCache} from "@application/themes/theme-boot-cache";

export abstract class WindowBase extends ViewModelBase {
    protected readonly settings: Readonly<Settings> = resolve(Settings);
    private observerLocator: IObserverLocator;

    protected constructor() {
        super(resolve(ILogger));
        this.observerLocator = resolve(IObserverLocator);
        this.logger = resolve(ILogger).scopeTo(this.constructor.name);
    }

    protected get classes() {
        return `theme-netpad-${this.settings.appearance.theme.toLowerCase()} icon-theme-${this.settings.appearance.iconTheme.toLowerCase()}`;
    }

    public override attaching() {
        super.attaching();

        this.observe([
            x => x.settings.styles.enabled,
            x => x.settings.styles.customCss,
        ], () => this.applyCustomCss());

        this.observe([
            x => x.settings.appearance.theme,
            x => x.settings.appearance.iconTheme,
        ], () => this.applyTheme());

        this.applyCustomCss();
        this.applyTheme();
    }

    private observe(properties: ((self: this) => unknown)[], onChange: () => void) {
        const handler = {handleChange: onChange};

        for (const property of properties) {
            const observer = this.observerLocator.getObserver(this, property);
            observer.subscribe(handler);
            this.addDisposable(() => observer.unsubscribe(handler));
        }
    }

    /**
     * Puts the theme classes on the document element, not just on the window element. Surfaces
     * that render outside the window (the document background, dialog overlays) need the theme's
     * CSS variables too, and `color-scheme` only styles native UI when it is set on the root.
     */
    private applyTheme() {
        const root = document.documentElement;
        const classes = this.classes.split(" ").filter(c => c.length > 0);

        root.classList.remove(...[...root.classList].filter(c => c.startsWith("theme-netpad-") || c.startsWith("icon-theme-")));
        root.classList.add(...classes);

        // The pre-boot paint from index.html has served its purpose. Hand the background back to
        // the stylesheet so it keeps up with theme changes.
        root.style.removeProperty("background-color");

        ThemeBootCache.write(this.classes, getComputedStyle(root).getPropertyValue("--bg0").trim());
    }

    private applyCustomCss() {
        const styleElementId = "user-custom-styles";
        const css = this.settings.styles.enabled ? (this.settings.styles.customCss ?? null) : null;

        let styleElement = document.getElementById(styleElementId);

        if (css) {
            const cssTextNode = document.createTextNode(css);

            if (!styleElement) {
                styleElement = document.createElement("style");
                styleElement.id = styleElementId;
                styleElement.setAttribute("type", "text/css");
                styleElement.appendChild(cssTextNode);

                // Add to body instead of header to ensure it has the highest precedence
                document.body.prepend(styleElement);
            } else {
                styleElement.replaceChildren(cssTextNode);
            }

        } else if (styleElement) {
            styleElement.remove();
        }
    }
}

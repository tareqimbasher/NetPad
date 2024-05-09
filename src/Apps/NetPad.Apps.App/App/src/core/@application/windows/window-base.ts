import {ILogger, resolve, IObserverLocator} from "aurelia";
import {Settings} from "@domain";
import {ViewModelBase} from "@application";

export abstract class WindowBase extends ViewModelBase {
    protected readonly settings: Readonly<Settings> = resolve(Settings);
    private observerLocator: IObserverLocator;

    protected constructor() {
        super(resolve(ILogger));
        this.observerLocator = resolve(IObserverLocator);
        this.logger = resolve(ILogger).scopeTo((this as Record<string, unknown>).constructor.name);
    }

    protected get classes() {
        return `netpad-${this.settings.appearance.theme.toLowerCase()} icon-theme-${this.settings.appearance.iconTheme.toLowerCase()}`;
    }

    public override attaching() {
        const observers = [
            this.observerLocator.getObserver(this, x => x.settings.styles.enabled),
            this.observerLocator.getObserver(this, x => x.settings.styles.customCss),
        ];

        const handler = {
            handleChange: () => this.applyCustomCss()
        };

        for (const observer of observers) {
            observer.subscribe(handler)
            this.addDisposable(() => observer.unsubscribe(handler));
        }

        this.applyCustomCss();
    }

    private applyCustomCss() {
        const css = this.settings.styles.enabled ? (this.settings.styles.customCss ?? null) : null;

        let styleElement = document.getElementById("user-styles");

        if (css) {
            const cssTextNode = document.createTextNode(css);

            if (!styleElement) {
                styleElement = document.createElement("style");

                const head = document.head || document.getElementsByTagName('head')[0];
                head.appendChild(styleElement);

                styleElement.id = "user-styles";
                styleElement.setAttribute("type", "text/css");
                styleElement.appendChild(cssTextNode);
            } else {
                styleElement.replaceChildren(cssTextNode);
            }

        } else if (styleElement) {
            styleElement.remove();
        }
    }
}

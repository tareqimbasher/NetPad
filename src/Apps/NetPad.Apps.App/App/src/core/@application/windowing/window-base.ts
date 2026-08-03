import {ILogger, IObserverLocator, resolve} from "aurelia";
import {Settings, ViewModelBase} from "@application";
import {IKeybindingManager} from "@application/keybindings/ikeybinding-manager";
import {AppTheme} from "@application/themes/app-theme";
import {ThemeBootCache} from "@application/themes/theme-boot-cache";
import {CustomCss} from "@application/themes/custom-css";

export abstract class WindowBase extends ViewModelBase {
    protected readonly settings: Readonly<Settings> = resolve(Settings);
    private observerLocator: IObserverLocator;
    private readonly keybindingManager = resolve(IKeybindingManager);

    protected constructor() {
        super(resolve(ILogger));
        this.observerLocator = resolve(IObserverLocator);
        this.logger = resolve(ILogger).scopeTo(this.constructor.name);
    }

    public override attaching() {
        super.attaching();

        this.keybindingManager.initialize();

        this.observe([
            x => x.settings.styles.enabled,
            x => x.settings.styles.customCss,
        ], () => this.applyCustomCss());

        this.observe([
            x => x.settings.appearance.themeFamily,
            x => x.settings.appearance.mode,
            x => x.settings.appearance.background,
        ], () => this.applyTheme());

        // When in System mode, apply the theme when machine preference changes.
        this.addDisposable(AppTheme.onSystemGroundChanged(() => {
            if (this.settings.appearance.mode === "System") {
                this.applyTheme();
            }
        }));

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

    private applyTheme() {
        const appearance = this.settings.appearance;

        AppTheme.applyToDocument(appearance.themeFamily, appearance.mode, appearance.background);
        ThemeBootCache.writeFor(appearance.themeFamily, appearance.mode, appearance.background);
    }

    private applyCustomCss() {
        CustomCss.apply(this.settings.styles.enabled ? this.settings.styles.customCss : null);
    }
}

import {resolve} from "aurelia";
import {Settings} from "@domain";

export abstract class WindowBase {
    protected readonly settings: Readonly<Settings> = resolve(Settings);

    protected get classes() {
        return `netpad-${this.settings.appearance.theme.toLowerCase()} icon-theme-${this.settings.appearance.iconTheme.toLowerCase()}`;
    }
}

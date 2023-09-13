import {bindable} from "aurelia";
import {Env} from "@domain";

/**
 * A custom attribute that removes the element it's applied on from the DOM on unsupported platforms.
 * Usage: <div platforms="Electron"></a> will only keep this element in the DOM when the platform is Electron.
 *
 * The value of the platforms attribute must be a comma delimited string.
 * Possible values (case-sensitive):
 *      - Electron
 *      - Web
 */
export class PlatformsCustomAttribute {
    @bindable supportedPlatforms?: string;

    constructor(private readonly element: Element) {
    }

    public bound() {
        if (!this.supportedPlatforms) return;

        const runningInElectron = Env.isRunningInElectron();

        const supported = this.supportedPlatforms.split(",").map(x => x.trim());

        if (runningInElectron && supported.indexOf("Electron") < 0)
            this.element.remove();

        if (!runningInElectron && supported.indexOf("Browser") < 0)
            this.element.remove();
    }
}

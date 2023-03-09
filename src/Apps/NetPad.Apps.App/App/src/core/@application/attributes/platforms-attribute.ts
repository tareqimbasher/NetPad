import {bindable} from "aurelia";
import {Env} from "@domain";

/**
 * Used to mark an element to show only if the current running platform is supported.
 * Usage: <div platforms="Electron"></a> will only show this div when the platform is Electron.
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

        if (runningInElectron && this.supportedPlatforms.indexOf("Electron") < 0)
            this.element.remove();

        if (!runningInElectron && this.supportedPlatforms.indexOf("Web") < 0)
            this.element.remove();
    }
}

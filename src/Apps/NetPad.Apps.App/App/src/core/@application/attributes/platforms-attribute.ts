import {bindable} from "aurelia";
import {Env} from "@application";
import {System} from "@common";

/**
 * A custom attribute that removes the element it's applied to from the DOM on unsupported platforms.
 * Usage: <div platforms="Electron"></a> will only keep this element in the DOM when the platform is Electron.
 *
 * The value of the platforms attribute must be a comma delimited string.
 * Possible values (case-insensitive):
 *      - Electron
 *      - Web
 *      - Any type in NodeJS.Platform (win32, darwin, linux...etc). These values also accept "!" in front
 *      to signify "NOT". ie. !win32 = show element on all except win32.
 */
export class PlatformsCustomAttribute {
    @bindable requirements?: string;

    constructor(private readonly element: Element) {
    }

    public bound() {
        if (!this.requirements) return;

        const requirements = this.requirements.split(",").map(x => x.trim().toLowerCase());


        const runtimeReqs = requirements.filter(x => ["electron", "browser"].indexOf(x) >= 0);
        if (runtimeReqs.length) {
            const runningInElectron = Env.isRunningInElectron();

            if (runningInElectron && runtimeReqs.indexOf("electron") < 0) {
                this.element.remove();
                return
            }

            if (!runningInElectron && runtimeReqs.indexOf("browser") < 0) {
                this.element.remove();
                return;
            }
        }

        const osReqs = requirements.filter(x => ["darwin", "win32", "linux"].indexOf(x) >= 0);
        if (osReqs.length) {
            const currentPlat = System.getPlatform()?.toLowerCase();

            if (currentPlat) {
                if (osReqs.indexOf(currentPlat) < 0) {
                    this.element.remove();
                    return;
                }

                // If inverse is specified (ex. !win32) and we are on win32, remove element
                if (osReqs.indexOf("!" + currentPlat) >= 0) {
                    this.element.remove();
                    return;
                }
            }
        }
    }
}

import {bindable} from "aurelia";
import {Env} from "@application/env";

/**
 * A custom attribute that removes the element it's applied to from the DOM on unsupported shells.
 * Usage: <div shells="electron"></a> will only keep target element in the DOM when app is hosted in an Electron shell.
 *
 * The value of the shells attribute must be a comma seperated string.
 * Possible values (case-insensitive):
 *      - electron
 *      - browser
 *
 * Prepending any of these values with "!" signifies "NOT". ie. !electron = show element on all shells except Electron.
 */
export class ShellsCustomAttribute {
    @bindable requirements?: string;

    constructor(private readonly element: Element) {
    }

    public bound() {
        const currentShell = Env.isRunningInElectron() ? "electron" : "browser";

        if (!ShellsCustomAttribute.areRequirementsMet(this.requirements, currentShell)) {
            this.element.remove();
        }
    }

    public static areRequirementsMet(requirements: string | undefined, currentShell: "browser" | "electron"): boolean {
        if (!requirements) {
            return true;
        }

        const shellRequirements = requirements
            .split(",")
            .map(x => x.trim().toLowerCase());

        const allowedShells = new Set<string>();
        const disallowedShells = new Set<string>();

        shellRequirements.forEach(sh => {
            if (sh.startsWith('!')) {
                disallowedShells.add(sh.slice(1));
            } else {
                allowedShells.add(sh);
            }
        });

        if (disallowedShells.has(currentShell)) {
            return false;
        }

        if (allowedShells.size > 0 && !allowedShells.has(currentShell)) {
            return false;
        }

        return true;
    }
}

import {bindable} from "aurelia";
import {ShellType} from "@application/windows/shell-type";
import {WindowParams} from "@application/windows/window-params";

/**
 * A custom attribute that removes the element it's applied to from the DOM on unsupported shells.
 * Usage: <div shells="electron"></a> will only keep target element in the DOM when app is hosted in an Electron shell.
 *
 * The value of the shells attribute must be a comma seperated string.
 * Possible values (case-insensitive):
 *      - browser
 *      - electron
 *      - tauri
 *
 * Prepending any of these values with "!" signifies "NOT". ie. !electron = show element on all shells except Electron.
 */
export class ShellsCustomAttribute {
    @bindable requirements?: string;

    constructor(private readonly element: Element, private readonly windowParams: WindowParams) {
    }

    public bound() {
        if (!ShellsCustomAttribute.areRequirementsMet(this.requirements, this.windowParams.shell)) {
            this.element.remove();
        }
    }

    public static areRequirementsMet(requirements: string | undefined, currentShell: ShellType): boolean {
        if (!requirements) {
            return true;
        }

        const shellRequirements = requirements
            .split(",")
            .map(x => x.trim().toLowerCase());

        const whitelist = new Set<string>();
        const blacklist = new Set<string>();

        for (const shell of shellRequirements) {
            if (shell.startsWith('!')) {
                blacklist.add(shell.slice(1));
            } else {
                whitelist.add(shell);
            }
        }

        if (blacklist.has(currentShell)) {
            return false;
        }

        if (whitelist.size > 0 && !whitelist.has(currentShell)) {
            return false;
        }

        return true;
    }
}

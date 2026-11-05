import {CommandIds} from "@application/commands/command-ids";
import {OpenDialogs} from "./open-dialogs";

/**
 * Whether an open dialog stops an app command from running, by key or from a menu. Toggle Developer
 * Tools still runs, so a stuck dialog can be debugged.
 * @param commandId The command to run, or undefined for a menu item that runs its own action.
 */
export function isBlockedByOpenDialog(commandId: string | undefined): boolean {
    return OpenDialogs.any && commandId !== CommandIds.toggleDeveloperTools;
}

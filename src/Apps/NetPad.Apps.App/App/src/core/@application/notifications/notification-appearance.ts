import {AppStatusMessageSeverity} from "../api";
import {IconName} from "../ui/np-icon/icons";

interface SeverityAppearance {
    readonly icon: IconName;
    readonly text: string;
}

/**
 * Single source of truth mapping a message severity to the icon and text-color class used to
 * represent it on the UI.
 */
const SEVERITY_APPEARANCE: Record<AppStatusMessageSeverity, SeverityAppearance> = {
    Info: {icon: "info", text: "text-info"},
    Success: {icon: "check-circle", text: "text-success"},
    Warning: {icon: "warning", text: "text-warning"},
    Error: {icon: "error", text: "text-danger"},
};

/**
 * Maps a message severity to the icon used to represent it on the UI.
 */
export function severityIcon(severity: AppStatusMessageSeverity): IconName {
    return (SEVERITY_APPEARANCE[severity] ?? SEVERITY_APPEARANCE.Info).icon;
}

/**
 * Maps a message severity to the text-color class used to represent it on the UI.
 */
export function severityTextClass(severity: AppStatusMessageSeverity): string {
    return (SEVERITY_APPEARANCE[severity] ?? SEVERITY_APPEARANCE.Info).text;
}

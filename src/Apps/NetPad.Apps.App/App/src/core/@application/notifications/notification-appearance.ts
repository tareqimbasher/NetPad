import {AppStatusMessageSeverity} from "../api";

interface SeverityAppearance {
    readonly icon: string;
    readonly text: string;
}

/**
 * Single source of truth mapping a message severity to the icon and text-color classes used to
 * represent it on the UI.
 */
const SEVERITY_APPEARANCE: Record<AppStatusMessageSeverity, SeverityAppearance> = {
    Info: {icon: "info-icon", text: "text-blue"},
    Success: {icon: "check-circle-icon", text: "text-success"},
    Warning: {icon: "warning-icon", text: "text-warning"},
    Error: {icon: "error-icon", text: "text-danger"},
};

/**
 * Maps a message severity to the icon class used to represent it on the UI.
 */
export function severityIconClass(severity: AppStatusMessageSeverity): string {
    return (SEVERITY_APPEARANCE[severity] ?? SEVERITY_APPEARANCE.Info).icon;
}

/**
 * Maps a message severity to the text-color class used to represent it on the UI.
 */
export function severityTextClass(severity: AppStatusMessageSeverity): string {
    return (SEVERITY_APPEARANCE[severity] ?? SEVERITY_APPEARANCE.Info).text;
}

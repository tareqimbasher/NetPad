import {ScriptKind} from "@application/api";

/**
 * How a script's language is named wherever a script is listed. Undefined for a kind that has no
 * badge of its own.
 */
export function scriptKindBadge(kind: ScriptKind): string | undefined {
    if (kind === "Program" || kind === "Expression") return "C#";
    if (kind === "SQL") return "SQL";
    return undefined;
}

/**
 * The "universe" the command palette is operating in. It is determined by the prefix the query starts with.
 */
export enum PaletteMode {
    /** Search for scripts. */
    Scripts = "scripts",
    /** Search for commands. */
    Commands = "commands",
    /** Go to line/column in editor. */
    Line = "line",
    /** Go to symbol in editor. */
    Symbol = "symbol",
    /** Command palette help. */
    Help = "help",
}

/** The prefix used to enable each mode. An empty prefix means Scripts mode. */
export const palettePrefixes: ReadonlyMap<string, PaletteMode> = new Map([
    [">", PaletteMode.Commands],
    [":", PaletteMode.Line],
    ["@", PaletteMode.Symbol],
    ["?", PaletteMode.Help],
]);

/** The prefix that opens a mode, or an empty string for the mode that needs none. */
export function prefixOf(mode: PaletteMode): string {
    for (const [prefix, prefixMode] of palettePrefixes) {
        if (prefixMode === mode) return prefix;
    }

    return "";
}

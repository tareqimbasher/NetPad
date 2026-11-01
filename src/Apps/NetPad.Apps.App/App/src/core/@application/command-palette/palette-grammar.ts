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

/** A palette mode, the prefix that opens it, and how the palette's footer and help rows describe it. */
export interface PaletteModeInfo {
    mode: PaletteMode;
    /** Empty for the mode a query starts in. */
    prefix: string;
    /** The mode's name in the footer legend. */
    label: string;
    /** The title of the mode's help row. The help mode has none. */
    help?: string;
    /** The detail of the mode's help row. */
    detail?: string;
}

/** Every palette mode, in the order the help rows and the footer legend list them. */
export const paletteModes: readonly PaletteModeInfo[] = [
    {
        mode: PaletteMode.Scripts,
        prefix: "",
        label: "scripts",
        help: "Go to script — just start typing",
        detail: "open · recent · library",
    },
    {mode: PaletteMode.Commands, prefix: ">", label: "commands", help: "Run a command", detail: "application · editor"},
    {mode: PaletteMode.Line, prefix: ":", label: "line", help: "Go to line / column", detail: "→ editor quick input"},
    {mode: PaletteMode.Symbol, prefix: "@", label: "symbol", help: "Go to symbol", detail: "→ editor quick input"},
    {mode: PaletteMode.Help, prefix: "?", label: "help"},
];

/** The prefix used to enable each mode. An empty prefix means Scripts mode. */
export const palettePrefixes: ReadonlyMap<string, PaletteMode> = new Map(paletteModes
    .filter(info => info.prefix)
    .map(info => [info.prefix, info.mode]));

/** The prefix that opens a mode, or an empty string for the mode that needs none. */
export function prefixOf(mode: PaletteMode): string {
    return paletteModes.find(info => info.mode === mode)?.prefix ?? "";
}

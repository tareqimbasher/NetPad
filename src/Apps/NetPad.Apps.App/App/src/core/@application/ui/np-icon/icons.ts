/**
 * The app's glyph set.
 *
 * Marks are drawn from Lucide (https://lucide.dev), ISC licensed:
 *
 *   Copyright (c) for portions of Lucide are held by Cole Bemis 2013-2022 as part of Feather
 *   (MIT). All other copyright (c) for Lucide are held by Lucide Contributors 2022.
 *
 * A few are simplified variants of their Lucide originals so they still read at 11px.
 *
 * Keys are NetPad's vocabulary, not Lucide's: a surface asks for `delete`, never for `trash-2`,
 * so the drawing behind a meaning can change without touching call sites. Each entry
 * records the mark it came from, so the set can be re-synced or audited against upstream later.
 *
 * To see the whole set rendered at every size in both themes, run the dev server and open
 * http://localhost:9000/icon-gallery.html. Adding a glyph: docs/technical-docs/Adding-an-Icon.md.
 */
export interface Icon {
    /** Children of a 24×24 `viewBox`. */
    readonly body: string;
    /** Overrides the stroked default. */
    readonly filled?: boolean;
    /** Overrides the default weight. Surfaces can override it again with `--icon-stroke`. */
    readonly strokeWidth?: number;
}

const HEAVY = 2;

// Drawings that two, or more, different icons share.
// lucide: list-ordered, simplified
const ORDERED_LIST = `<path d="M10 6h11M10 12h11M10 18h11"/><path d="M4 6h1v4M4 10h2"/><path d="M6 18H3c0-1 2-2 2-3s-1-1.5-2-1"/>`;

// lucide: hash
const HASH = `<path d="M4 9h16M4 15h16M10 3 8 21M16 3l-2 18"/>`;

// lucide: table, simplified
const TABLE = `<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 10h18M9 10v10"/>`;

// lucide: lightbulb, simplified
const LIGHTBULB = `<path d="M15 14c.2-1 .7-1.7 1.5-2.5A5.7 5.7 0 0 0 18 8 6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.8.8 1.3 1.5 1.5 2.5"/><path d="M9 18h6M10 22h4"/>`;

export const icons = {
    // Generic actions
    add: {body: `<path d="M12 5v14M5 12h14"/>`, strokeWidth: HEAVY}, // lucide: plus
    check: {body: `<path d="M20 6 9 17l-5-5"/>`}, // lucide: check
    "check-circle": {body: `<circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/>`}, // lucide: circle-check
    close: {body: `<path d="M18 6 6 18M6 6l12 12"/>`, strokeWidth: HEAVY}, // lucide: x
    copy: { // lucide: copy
        body: `<rect x="8" y="8" width="14" height="14" rx="2"/><path d="M4 16a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2"/>`,
    },
    delete: { // lucide: trash-2, simplified
        body: `<path d="M3 6h18"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/><path d="M10 11v6M14 11v6"/>`,
    },
    duplicate: { // lucide: copy-plus
        body: `<path d="M15 12v6M12 15h6"/><rect x="8" y="8" width="14" height="14" rx="2"/><path d="M4 16a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2"/>`,
    },
    edit: { // lucide: pencil, simplified
        body: `<path d="M21.2 6.8a1 1 0 0 0-4-4L3.8 16.2a2 2 0 0 0-.5.8l-1.3 4.4a.5.5 0 0 0 .6.6l4.4-1.3a2 2 0 0 0 .8-.5z"/><path d="m15 5 4 4"/>`,
    },
    error: {body: `<circle cx="12" cy="12" r="10"/><path d="m15 9-6 6M9 9l6 6"/>`}, // lucide: circle-x
    info: {body: `<circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/>`}, // lucide: info
    minus: {body: `<path d="M5 12h14"/>`, strokeWidth: HEAVY}, // lucide: minus
    properties: { // lucide: wrench
        body: `<path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>`,
    },
    redo: {body: `<path d="m15 14 5-5-5-5"/><path d="M20 9H9.5A5.5 5.5 0 0 0 9.5 20H13"/>`}, // lucide: redo-2, simplified
    refresh: {body: `<path d="M21 12a9 9 0 1 1-2.64-6.36L21 8"/><path d="M21 3v5h-5"/>`}, // lucide: rotate-cw, simplified
    rename: { // lucide: square-pen, simplified
        body: `<path d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.4 2.6a1 1 0 0 1 3 3l-9 9-3.9 1 1-3.9z"/>`,
    },
    reset: {body: `<path d="M3 12a9 9 0 1 0 2.64-6.36L3 8"/><path d="M3 3v5h5"/>`}, // lucide: rotate-ccw, simplified
    save: { // lucide: save
        body: `<path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><path d="M17 21v-8H7v8M7 3v5h8"/>`,
    },
    search: {body: `<circle cx="11" cy="11" r="7"/><path d="m21 21-4.35-4.35"/>`}, // lucide: search, simplified
    settings: { // lucide: settings
        body: `<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33 1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82 1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>`,
    },
    star: { // lucide: star, simplified
        body: `<path d="m12 3 2.9 5.9 6.5.9-4.7 4.6 1.1 6.5-5.8-3-5.8 3 1.1-6.5L2.6 9.8l6.5-.9z"/>`,
    },
    undo: {body: `<path d="M9 14 4 9l5-5"/><path d="M4 9h10.5a5.5 5.5 0 0 1 0 11H11"/>`}, // lucide: undo-2, simplified
    warning: {body: `<path d="m21.7 18-8-14a2 2 0 0 0-3.4 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.7-3"/><path d="M12 9v4M12 17h.01"/>`}, // lucide: triangle-alert, simplified

    // Direction and disclosure. The chevron pair states the disclosure contract on its face:
    // down = expanded, right = collapsed.
    "arrow-down": {body: `<path d="M12 5v14"/><path d="m19 12-7 7-7-7"/>`}, // lucide: arrow-down
    "arrow-up": {body: `<path d="M12 19V5"/><path d="m5 12 7-7 7 7"/>`}, // lucide: arrow-up
    "chevron-down": {body: `<path d="m6 9 6 6 6-6"/>`}, // lucide: chevron-down
    "chevron-right": {body: `<path d="m9 18 6-6-6-6"/>`}, // lucide: chevron-right
    "chevron-up": {body: `<path d="m18 15-6-6-6 6"/>`}, // lucide: chevron-up
    "collapse-all": {body: `<path d="m7 20 5-5 5 5"/><path d="m7 4 5 5 5-5"/>`}, // lucide: chevrons-down-up
    "expand-all": {body: `<path d="m7 15 5 5 5-5"/><path d="m7 9 5-5 5 5"/>`}, // lucide: chevrons-up-down
    "navigate-bottom": {body: `<path d="m7 6 5 5 5-5"/><path d="m7 13 5 5 5-5"/>`}, // lucide: chevrons-down
    "navigate-top": {body: `<path d="m17 11-5-5-5 5"/><path d="m17 18-5-5-5 5"/>`}, // lucide: chevrons-up

    // Files, folders and scripts
    folder: {body: `<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>`}, // lucide: folder
    "folder-open": { // lucide: folder-open, simplified
        body: `<path d="M6 14l1.5-2.9A2 2 0 0 1 9.2 10H20a2 2 0 0 1 1.9 2.5l-1.5 6a2 2 0 0 1-2 1.5H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h3.9a2 2 0 0 1 1.7.9l.8 1.2a2 2 0 0 0 1.7.9H18a2 2 0 0 1 2 2v2"/>`,
    },
    "open-external": { // lucide: external-link
        body: `<path d="M15 3h6v6"/><path d="M10 14 21 3"/><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/>`,
    },
    script: {body: `<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6"/>`}, // lucide: file

    // Code and data
    code: {body: `<path d="m16 18 6-6-6-6M8 6l-6 6 6 6"/>`}, // lucide: code
    database: { // lucide: database, simplified
        body: `<ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v14c0 1.66 3.58 3 8 3s8-1.34 8-3V5"/><path d="M4 12c0 1.66 3.58 3 8 3s8-1.34 8-3"/>`,
    },
    "database-server": { // lucide: server, simplified
        body: `<rect x="2" y="2" width="20" height="8" rx="2"/><rect x="2" y="14" width="20" height="8" rx="2"/><path d="M6 6h.01M6 18h.01"/>`,
    },
    "db-column": {body: `<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 3v18M15 3v18"/>`}, // lucide: columns-3
    "db-foreign-key": { // lucide: link
        body: `<path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>`,
    },
    "db-navigation": {body: `<path d="M17 7 7 17M7 7h10v10"/>`}, // lucide: arrow-up-right
    "db-index": {body: ORDERED_LIST}, // lucide: list-ordered, simplified
    "db-query": {body: `<path d="M13 2 4 14h7l-1 8 9-12h-7z"/>`}, // lucide: zap, simplified
    "db-primary-key": {body: `<circle cx="7.5" cy="15.5" r="4.5"/><path d="M21 2l-9.6 9.6M15.5 7.5l3 3"/>`}, // lucide: key, simplified
    "db-schema": {body: `<path d="M2 7l10-5 10 5-10 5z"/><path d="M2 17l10 5 10-5M2 12l10 5 10-5"/>`}, // lucide: layers
    "db-table": {body: TABLE},
    html: {body: `<path d="m18 16 4-4-4-4"/><path d="m6 8-4 4 4 4"/><path d="m14.5 4-5 16"/>`}, // lucide: code-xml
    "line-numbers": {body: ORDERED_LIST}, // lucide: list-ordered, simplified
    "syntax-node": {body: `<path d="M12 5.5 18.5 12 12 18.5 5.5 12z"/>`, filled: true}, // custom; lucide: diamond
    "syntax-value": {body: HASH},
    namespaces: { // lucide: braces, simplified
        body: `<path d="M8 3H7a2 2 0 0 0-2 2v4a2 2 0 0 1-2 2 2 2 0 0 1 2 2v4a2 2 0 0 0 2 2h1M16 3h1a2 2 0 0 1 2 2v4a2 2 0 0 0 2 2 2 2 0 0 0-2 2v4a2 2 0 0 1-2 2h-1"/>`,
    },

    // Running
    run: {body: `<path d="M7 4.5v15l13-7.5z"/>`, filled: true}, // lucide: play, simplified to one filled triangle
    stop: {body: `<rect x="6" y="6" width="12" height="12" rx="1.5"/>`, filled: true}, // custom — a filled rounded square; lucide: square is stroked only

    // Panes and window chrome
    clipboard: { // lucide: clipboard
        body: `<rect x="8" y="2" width="8" height="4" rx="1"/><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/>`,
    },
    "mem-cache": { // lucide: memory-stick, simplified
        body: `<path d="M2 7a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2z"/><path d="M2 14h20"/><path d="M7 9v2M12 9v2M17 9v2"/>`,
    },
    "main-menu": {body: `<path d="M4 6h16M4 12h16M4 18h16"/>`}, // lucide: menu
    notifications: {body: `<path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.7 21a2 2 0 0 1-3.4 0"/>`}, // lucide: bell
    output: {body: `<path d="m4 17 6-6-6-6M12 19h8"/>`}, // lucide: terminal
    "pane-collapse": {body: `<path d="M5 12h14"/>`, strokeWidth: HEAVY}, // lucide: minus
    "reverse-flow": {body: `<path d="m21 16-4 4-4-4M17 20V4"/><path d="m3 8 4-4 4 4M7 4v16"/>`}, // lucide: arrow-up-down
    "pop-out": { // lucide: external-link
        body: `<path d="M15 3h6v6"/><path d="M10 14 21 3"/><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/>`,
    },
    secrets: {body: `<circle cx="7.5" cy="15.5" r="4.5"/><path d="M21 2l-9.6 9.6M15.5 7.5l3 3"/>`}, // lucide: key, simplified
    "window-always-on-top": {body: `<path d="M12 17v5M9 3h6l1 7 3 2H5l3-2z"/>`}, // lucide: pin, simplified
    "window-close": {body: `<path d="M18 6 6 18M6 6l12 12"/>`, strokeWidth: HEAVY}, // lucide: x
    "window-maximize": {body: `<rect x="4" y="4" width="16" height="16" rx="2"/>`}, // lucide: square
    "window-minimize": {body: `<path d="M5 12h14"/>`, strokeWidth: HEAVY}, // lucide: minus
    "window-restore": {body: `<rect x="8" y="3" width="13" height="13" rx="2"/><path d="M16 19a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-9a2 2 0 0 1 2-2"/>`}, // lucide: copy, simplified

    // Output pane toolbar
    "clear-output": {body: `<circle cx="12" cy="12" r="9"/><path d="m5.6 5.6 12.8 12.8"/>`}, // lucide: ban, simplified
    "excel-file": { // lucide: sheet
        body: `<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M3 15h18M9 9v12M15 9v12"/>`,
    },
    "scroll-on-output": {body: `<path d="M12 17V3"/><path d="m6 11 6 6 6-6"/><path d="M19 21H5"/>`}, // lucide: arrow-down-to-line
    "text-wrap": { // lucide: wrap-text, simplified
        body: `<path d="M3 6h18"/><path d="M3 12h15a3 3 0 1 1 0 6h-4"/><path d="m16 16-2 2 2 2"/><path d="M3 18h7"/>`,
    },

    // Settings, help and app-level
    "app-deps-check": {body: `<rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>`}, // lucide: monitor
    "assembly-headers": {body: `<path d="M3 5v14M8 5v14M12 5v14M17 5v14M21 5v14"/>`}, // lucide: barcode, simplified
    "app-update": { // lucide: cloud-download, simplified
        body: `<path d="M12 13v8l-4-4M12 21l4-4"/><path d="M4.4 15.3A7 7 0 1 1 15.7 8h1.8a4.5 4.5 0 0 1 2.4 8.3"/>`,
    },
    "case-sensitive": {body: `<path d="m3 15 4-8 4 8M4 13h6"/><circle cx="18" cy="12" r="3"/><path d="M21 9v6"/>`}, // lucide: case-sensitive
    cloud: {body: `<path d="M17.5 19H9a7 7 0 1 1 6.71-9h1.79a4.5 4.5 0 1 1 0 9"/>`}, // lucide: cloud
    "code-intelligence": {body: LIGHTBULB},
    "custom-css": { // lucide: pen-tool
        body: `<path d="m12 19 7-7 3 3-7 7-3-3z"/><path d="M18 13l-1.5-7.5L2 2l3.5 14.5L13 18l5-5z"/><path d="m2 2 7.586 7.586"/><circle cx="11" cy="11" r="2"/>`,
    },
    github: { // custom — the GitHub brand mark; lucide ships no brand icons
        body: `<path d="M12 .5C5.37.5 0 5.87 0 12.5c0 5.3 3.44 9.8 8.21 11.39.6.11.82-.26.82-.58 0-.29-.01-1.05-.02-2.06-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.34-1.76-1.34-1.76-1.09-.75.08-.73.08-.73 1.21.09 1.84 1.24 1.84 1.24 1.07 1.84 2.81 1.31 3.5 1 .11-.78.42-1.31.76-1.61-2.67-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.12-.3-.54-1.52.12-3.17 0 0 1.01-.32 3.3 1.23a11.5 11.5 0 0 1 6 0c2.29-1.55 3.3-1.23 3.3-1.23.66 1.65.24 2.87.12 3.17.77.84 1.24 1.91 1.24 3.22 0 4.61-2.81 5.63-5.49 5.93.43.37.81 1.1.81 2.22 0 1.6-.01 2.89-.01 3.28 0 .32.21.7.83.58A12.01 12.01 0 0 0 24 12.5C24 5.87 18.63.5 12 .5z"/>`,
        filled: true,
    },
    keyboard: { // lucide: keyboard, simplified
        body: `<rect x="2" y="4" width="20" height="16" rx="2"/><path d="M6 8h.01M10 8h.01M14 8h.01M18 8h.01M8 12h.01M12 12h.01M16 12h.01M7 16h10"/>`,
    },
    "monaco-settings": {body: `<path d="M21 6H3M15 12H3M17 18H3"/>`}, // lucide: align-left
    "quick-tips": {body: LIGHTBULB},
    results: {body: TABLE},
    "serialization-settings": { // lucide: box
        body: `<path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="m3.3 7 8.7 5 8.7-5M12 22V12"/>`,
    },
    sponsor: { // lucide: heart
        body: `<path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.51 4.04 3 5.5l7 7Z"/>`,
    },
    theme: {body: `<circle cx="12" cy="12" r="10"/><path d="M12 18a6 6 0 0 0 0-12z"/>`}, // lucide: contrast
    wiki: { // lucide: book-open
        body: `<path d="M12 7v14"/><path d="M3 18a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h5a4 4 0 0 1 4 4 4 4 0 0 1 4-4h5a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-6a3 3 0 0 0-3 3 3 3 0 0 0-3-3z"/>`,
    },
    "zoom-in": {body: `<circle cx="11" cy="11" r="7"/><path d="m21 21-4.35-4.35"/><path d="M11 8v6M8 11h6"/>`}, // lucide: zoom-in, simplified
    "zoom-out": {body: `<circle cx="11" cy="11" r="7"/><path d="m21 21-4.35-4.35"/><path d="M8 11h6"/>`}, // lucide: zoom-out, simplified

    // Packages and references
    references: { // lucide: layers
        body: `<path d="M2 7l10-5 10 5-10 5z"/><path d="M2 17l10 5 10-5M2 12l10 5 10-5"/>`,
    },
    package: { // lucide: package, simplified
        body: `<path d="M16.5 9.4 7.55 4.24"/><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.27 6.96 12 12.01l8.73-5.05M12 22.08V12"/>`,
    },
    "package-version": {body: HASH},
    "package-download": {body: `<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><path d="m7 10 5 5 5-5M12 15V3"/>`}, // lucide: download

    // Data-connection actions
    "use-connection-current-script": {body: `<circle cx="12" cy="12" r="10"/><path d="M8 12h8m-4-4 4 4-4 4"/>`}, // lucide: circle-arrow-right

    // Table column header for a column that is meant for row actions
    actions: {body: `<circle cx="5" cy="12" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/>`}, // lucide: ellipsis
} as const satisfies Record<string, Icon>;

export type IconName = keyof typeof icons;

const DEFAULT_STROKE_WIDTH = 1.7;

const STROKE = (width: number) => `style="stroke-width: var(--icon-stroke, ${width})"`;

/**
 * Renders a glyph as standalone SVG markup. Prefer the `<np-icon>` element. This exists for building an
 * SVG in JS instead of HTML.
 */
export function iconSvgMarkup(name: IconName | string): string {
    const icon: Icon | undefined = (icons as Record<string, Icon>)[name];
    if (!icon) return "";

    const paint = icon.filled
        ? `fill="currentColor"`
        : `fill="none" stroke="currentColor" ${STROKE(icon.strokeWidth ?? DEFAULT_STROKE_WIDTH)} stroke-linecap="round" stroke-linejoin="round"`;

    return `<svg class="np-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false" ${paint}>${icon.body}</svg>`;
}

/**
 * Builds an icon as a detached element.
 */
export function createIconElement(name: IconName | string): SVGElement | null {
    const markup = iconSvgMarkup(name);
    if (!markup) return null;

    const template = document.createElement("template");
    template.innerHTML = markup;
    return template.content.firstElementChild as SVGElement;
}

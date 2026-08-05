# Styling

You can change the way NetPad looks by defining your own CSS styles in <kbd><kbd>Settings</kbd> > <kbd>
Custom CSS</kbd></kbd>.

## Styling Output

Use the `.dump-container` class to define custom styles for the output container.

```css
.dump-container {
    font-size: 1.25rem;
}
```

You can also define your own CSS classes and use them when you `Dump()` a value:

```csharp
myObj.Dump(css: "important");
```

```css
.important {
    background-color: salmon;
}
```

### Classes on dumped values

A dumped object or collection is rendered as a table, and every cell holding a value is tagged with
what that value is. The tags are derived from the .NET type, so they are exact — a `decimal` is
tagged as a number whatever your culture renders it as.

| Class | Applied to |
|---|---|
| `property-name` | a cell naming a property, a column or a row |
| `property-value` | a cell holding a value |
| `item-count` | the number of items a table is showing, in its header |
| `numeric` | the numeric primitives, `decimal` and `nint`/`nuint`. Enums are labels, not numbers. |
| `negative` | a number below zero, alongside `numeric`. Zero and positive numbers are unmarked. |
| `boolean-true` / `boolean-false` | a `bool`, which renders as `true` / `false` |
| `enum` | an enum member |
| `temporal` | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| `null` | the `null` placeholder inside a cell |

A cell holding `null` carries no kind class — there is no value to describe.

NetPad styles `numeric`, `boolean-true` and `boolean-false`. The kind classes it does not use are
emitted for you:

```css
.dump-container .negative {
    color: crimson;
}
```

## Theming NetPad

NetPad ships five **palette families**, each with a dark and a light **ground** — ten themes in
all. Pick the family under <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Palette</kbd></kbd>
and the ground under **Mode** (or let Mode follow your desktop). A third setting,
[Background](#background), keeps the backgrounds neutral under whichever palette you pick.

| Family       | Dark ground | Light ground | Accent     |
|--------------|-------------|--------------|------------|
| **inkwell**  | ink         | vellum       | iris       |
| **cobalt**   | cobalt      | frost        | steel blue |
| **gunmetal** | gunmetal    | porcelain    | patina     |
| **rosewood** | rosewood    | magnolia     | rose       |
| **graphite** | graphite    | chalk        | tonal      |

A theme is a set of CSS variables defined on the class `.theme-<family>-<ground>` —
`.theme-inkwell-dark` (inkwell/ink, the default), `.theme-graphite-light` (graphite/chalk), and so
on.

Every family defines **the same variables with the same meanings**; only the values differ. So a
custom style written against these names works in all ten themes, and redefining a variable in your
custom styles rethemes the app.

> :warning: **These variables changed with the UI refresh.** The old names (`--background-color`,
> `--active-color`, `--tab-background`, …) no longer exist. If you have custom styles from an
> earlier version, see [Migrating from the old variables](#migrating-from-the-old-variables) below.

### Background

A third setting, <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Background</kbd></kbd>,
decides where the backgrounds come from:

- **Match palette** (the default) — every surface, line and text tier comes from the family. This is
  the palette as designed.
- **Neutral** — one neutral set of backgrounds and text is used under *every* family, the same in
  each, so switching palettes changes the accent, the semantic colors and the syntax colors but
  leaves the room they sit in alone. There is one neutral set for dark and one for light.

Neutral is an override, not a theme: it adds the class `.theme-neutral-bg` next to the theme class
on the root element and redefines the surface, line and text variables there. Because that selector
carries two classes, a custom rule scoped to the theme class alone is outranked while Neutral is on.
Name both classes to override a background under Neutral:

```css
/* applies whichever background setting is in use */
.theme-inkwell-dark { --bg0: #101010; }

/* needed as well, to win while Background is set to Neutral */
.theme-neutral-bg.theme-inkwell-dark { --bg0: #101010; }
```

Variables the family still owns under Neutral — `--accent`, the semantic colors, the `--syntax-*`
set — take a single class as usual.

### The variables

Colors are layered: **surfaces** (`bg0`–`bg3`, `editor`) stack from the window ground up to raised
controls, **lines** separate them, **text** comes in three tiers, and the rest is semantic — one
accent plus run/ok/warn/err. The accent marks selection, focus and links; green means running;
amber means production or caution; red means error. That vocabulary is the same in every family:
switching palettes never changes what a color *means*.

The values below are the default family. To read another family's values, open the dev tools on the
window and inspect its theme class.

```css
.theme-inkwell-dark {
    /* SURFACES */
    --bg0: #121116;                        /* ground — the window body            */
    --bg1: #19181f;                        /* panel — sidebars, bars, cards       */
    --bg2: #201f28;                        /* control — inputs, buttons, chips    */
    --bg3: #2a2933;                        /* raised — hover, active cell         */
    --editor: #141319;                     /* editor & results surface            */
    /* LINES */
    --line: #353342;                       /* borders on controls                 */
    --line-soft: #262430;                  /* hairlines — panel seams, table rows */
    /* TEXT */
    --text: #e9e7f0;
    --text-2: #a8a5b6;
    --text-3: #716e80;                     /* labels, disabled                    */
    /* ACCENT — selection, focus, active, links. Never "go". */
    --accent: #a89df0;
    --accent-bright: #c4bbf8;              /* emphasis                            */
    --accent-dim: #37325a;                 /* selection fill                      */
    --accent-text: #15121f;                /* text on an accent fill              */
    --accent-wash: rgba(168, 157, 240, 0.1);  /* selection wash                   */
    --accent-rgb: 168, 157, 240;           /* channels, for rgba() tints          */
    /* SEMANTICS */
    --run: #7cc98a;
    --run-text: #0d1a12;
    --run-text-rgb: 13, 26, 18;            /* channels, for tints on a run fill    */
    --run-rgb: 124, 201, 138;
    --ok: #7cc98a;
    --ok-rgb: 124, 201, 138;
    --warn: #d9a24e;
    --warn-rgb: 217, 162, 78;
    --err: #e0796d;
    --err-text: #ffffff;                   /* text/glyphs on an err fill           */
    --err-rgb: 224, 121, 109;
    /* OVERLAYS */
    --hover: rgba(255, 255, 255, 0.06);
    --hover-strong: rgba(255, 255, 255, 0.12);
    --backdrop: rgba(0, 0, 0, 0.55);       /* dialog scrim                        */
    /* SYNTAX ACCENTS — the editor theme is built from these; they also tint code chrome (SQL log) */
    --syntax-keyword: #9d8ff0;
    --syntax-type: #66c9b3;
    --syntax-string: #cfa675;
    --syntax-number: #d78ab0;
    --syntax-method: #84aff2;
    --syntax-comment: #5b5870;
    --syntax-value: #e4e2ec;
    --syntax-punctuation: #8b8899;
    --syntax-regex: #d9bd85;               /* regex literals, escape sequences     */
    /* SHADOWS */
    --shadow: 0 24px 70px rgba(0, 0, 0, 0.55), 0 4px 18px rgba(0, 0, 0, 0.4);
    --shadow-sm: 0 10px 30px rgba(0, 0, 0, 0.45);
    --shadow-edge: 0 6px 8px -8px rgba(0, 0, 0, 0.55);
    --shadow-ambient: 0 0 8px 2px rgba(0, 0, 0, 0.45);
    /* CHECKBOX / RADIO / SELECT GLYPHS — inline SVG, so the color is baked into the URL */
    --check-mark-image: url("data:image/svg+xml,...");
    --radio-mark-image: url("data:image/svg+xml,...");
    --select-caret-image: url("data:image/svg+xml,...");
}

.theme-inkwell-light {
    /* SURFACES */
    --bg0: #eceaf1;
    --bg1: #f6f5f9;
    --bg2: #ffffff;
    --bg3: #e7e4f0;
    --editor: #fdfdfe;
    /* LINES */
    --line: #d3d0de;
    --line-soft: #e2dfe9;
    /* TEXT */
    --text: #27242f;
    --text-2: #5d5a6b;
    --text-3: #928fa0;
    /* ACCENT */
    --accent: #6a5bd6;
    --accent-bright: #5647c4;
    --accent-dim: #ddd7f6;
    --accent-text: #ffffff;
    --accent-wash: rgba(106, 91, 214, 0.1);
    --accent-rgb: 106, 91, 214;
    /* SEMANTICS */
    --run: #2f9550;
    --run-text: #ffffff;
    --run-text-rgb: 255, 255, 255;         /* channels, for tints on a run fill    */
    --run-rgb: 47, 149, 80;
    --ok: #2f9550;
    --ok-rgb: 47, 149, 80;
    --warn: #b07514;
    --warn-rgb: 176, 117, 20;
    --err: #c44f42;
    --err-text: #ffffff;                   /* text/glyphs on an err fill           */
    --err-rgb: 196, 79, 66;
    /* OVERLAYS */
    --hover: rgba(53, 45, 90, 0.07);
    --hover-strong: rgba(53, 45, 90, 0.14);
    --backdrop: rgba(53, 45, 90, 0.35);
    /* SYNTAX ACCENTS */
    --syntax-keyword: #6a58d8;
    --syntax-type: #177f6b;
    --syntax-string: #a4692a;
    --syntax-number: #b1447e;
    --syntax-method: #2c66c9;
    --syntax-comment: #9c99a8;
    --syntax-value: #2c2935;
    --syntax-punctuation: #716e80;
    --syntax-regex: #a3832a;
    /* SHADOWS */
    --shadow: 0 24px 70px rgba(53, 45, 90, 0.18), 0 4px 18px rgba(53, 45, 90, 0.12);
    --shadow-sm: 0 10px 30px rgba(53, 45, 90, 0.14);
    --shadow-edge: 0 5px 8px -8px rgba(53, 45, 90, 0.3);
    --shadow-ambient: 0 0 8px 2px rgba(53, 45, 90, 0.18);
    /* CHECKBOX / RADIO / SELECT GLYPHS */
    --check-mark-image: url("data:image/svg+xml,...");
    --radio-mark-image: url("data:image/svg+xml,...");
    --select-caret-image: url("data:image/svg+xml,...");
}
```

There are also two font stacks, shared by both themes: `--font-sans` and `--font-mono`.

To override a variable, define it in your custom styles.

```css
.theme-inkwell-dark {
    --accent: red;
}
```

The `*-rgb` variables hold bare channel values so you can build your own tints:

```css
.my-highlight {
    background: rgba(var(--warn-rgb), 0.12);
    border: 1px solid rgba(var(--warn-rgb), 0.35);
}
```

Here's an example of customizing the dark theme using the Dracula color palette, which goes great
with the Dracula editor theme in <kbd><kbd>Settings</kbd> > <kbd>Editor</kbd></kbd>. Because every
surface in the app is drawn from these variables, retinting the scale retints the whole window:

```css
.theme-inkwell-dark {
    --bg0: #22222c;
    --bg1: #282a36;
    --bg2: #343746;
    --bg3: #424456;
    --editor: #282a36;

    --line: #4a4c60;
    --line-soft: #343746;

    --text: #f8f8f2;
    --text-2: #b8b8b0;
    --text-3: #6272a4;

    --accent: #bd93f9;
    --accent-bright: #dbb5fa;
    --accent-dim: #44475a;
    --accent-text: #22222c;
    --accent-rgb: 189, 147, 249;

    --run: #50fa7b;
    --run-text: #22222c;
    --run-rgb: 80, 250, 123;
    --ok: #50fa7b;
    --warn: #ffb86c;
    --err: #ff5555;
}
```

### Migrating from the old variables

Older NetPad versions used a different, larger set of variable names. They were replaced by the
scale above — several old names collapsed onto one new one (every panel background is now `--bg1`,
for example). Map your custom styles like this:

| Old variable                                                                 | Replace with                                                                   |
|------------------------------------------------------------------------------|--------------------------------------------------------------------------------|
| `--background-color`                                                         | `--bg0`                                                                        |
| `--script-toolbar-background`                                                | `--bg0`                                                                        |
| `--list-background`                                                          | `--bg1`                                                                        |
| `--pane-background`, `--pane-ribbon-background`, `--pane-toolbar-background` | `--bg1`                                                                        |
| `--titlebar-background`, `--statusbar-background`                            | `--bg1`                                                                        |
| `--context-menu-background`, `--dialog-background`                           | `--bg1`                                                                        |
| `--find-text-box-background`, `--kbd-background-color`                       | `--bg1`                                                                        |
| `--tab-background`                                                           | `--bg1`                                                                        |
| `--form-control-background-color`                                            | `--bg2`                                                                        |
| `--context-menu-item-hover-background`                                       | `--bg2`                                                                        |
| `--background-lighter-color`, `--list-item-clicked-background`               | `--bg3`                                                                        |
| `--form-control-disabled-background-color`                                   | `--bg1`                                                                        |
| `--tab-active-background`                                                    | `--editor`                                                                         |
| `--border-color`, `--tab-border-color`, `--splitter-color`                   | `--line-soft`                                                                  |
| `--border-contrast-color`, `--scrollbar-thumb-color`                         | `--line`                                                                       |
| `--text-color`, `--text-strong-color`, `--text-hover-color`                  | `--text`                                                                         |
| `--tab-active-text-color`, `--action-icon-hover-color`                       | `--text`                                                                         |
| `--tab-inactive-text-color`, `--action-icon-color`                           | `--text-2`                                                                        |
| `--active-color`, `--splitter-hover-color`                                   | `--accent`                                                                        |
| `--hover-color`                                                              | `--hover`                                                                      |
| `--hover-color-contrast`                                                     | `--hover-strong`                                                               |
| `--script-status-running-color`                                              | `--run`                                                                        |
| `--script-status-success-color`                                              | `--ok`                                                                         |
| `--script-status-stopping-color`                                             | `--warn`                                                                       |
| `--script-status-error-color`                                                | `--err`                                                                        |
| `--script-status-running-background`                                         | `rgba(var(--run-rgb), 0.18)`                                                   |
| `--script-status-stopping-background`                                        | `rgba(var(--warn-rgb), 0.18)`                                                  |
| `--script-status-error-background`                                           | `rgba(var(--err-rgb), 0.18)`                                                   |
| `--box-shadow-bottom-sm`                                                     | `--shadow-edge`                                                                |
| `--box-shadow-bottom-and-sides`                                              | `--shadow-ambient`                                                             |
| `--color-full`, `--color-contrast-full`, `--box-shadow-bottom`               | removed — style the element directly                                           |
| `--scrollbar-track-color`                                                    | removed — the track is transparent; style `::-webkit-scrollbar-track` directly |

### Theming Anything

You can style pretty much anything in NetPad. If you find something you'd like to style that isn't covered by
the pre-defined CSS variables open the Developer Console (`CTRL + SHIFT + I`), locate the element(s) you'd like to
customize and add them to your custom styles!

Example:

```css
.save-icon {
    color: orange;
}
```

!> **Note** that breaking changes to theme CSS variables and to DOM structure can occur in NetPad updates. If that
happens, it will be announced.

## Styling the Editor

### Predefined Themes

NetPad uses the Monaco editor. You can customize the look and feel of the editor by going to <kbd><kbd>
Settings</kbd> > <kbd>Editor</kbd></kbd>. There you'll find a number of themes to select from (powered by
the [monaco-themes](https://github.com/brijeshb42/monaco-themes) project).

The default, **Auto — match NetPad theme**, paints the editor in the palette family and mode you
picked under General. It uses that family's **aurora** theme, which is built from the theme
variables above rather than from a palette of its own, so the editor always matches the rest of the
window. The `--syntax-*` variables are the ones it reads: redefine those in your custom styles and
the editor follows along.

The picker also offers:

- **Visual Studio** — the editor colors NetPad used before the UI refresh. Like Auto it follows
  light/dark mode, but its colors are its own, so it looks the same whichever palette family the
  app is in.
- **Visual Studio (palette background)** — Visual Studio's syntax colors, everything around them
  from your palette: the editor surface, gutter, selection, current line and widgets are the
  aurora ones. Like Auto it follows the family, the mode and the Background setting.
- **Aurora: Ink** … **Aurora: Chalk** — one aurora theme per ground, pinned. Picking one keeps the
  editor in that palette even when the app is in another.
- Everything from the `monaco-themes` library.

Picking anything other than Auto opts the editor out of following the app's palette.

> :warning: Values the editor reads must be **6-digit hex** (`#a89df0`). The editor cannot use
> `rgb()`, `color-mix()` or named colors, and falls back to a neutral gray for anything else — so a
> variable written that way will retint the rest of the app but not the editor.

Nested brackets are tinted by depth from the same variables: the outermost pair stays in
`--syntax-punctuation` and deeper pairs take keyword, type, string, number and method in turn. To
re-color or flatten them, set `editorBracketHighlight.foreground1` through `foreground6` under
`themeCustomizations.colors` (see below).

### Custom Styles

You can customize a selected theme using the `themeCustomizations` property.

```jsonc
{
    "cursorBlinking": "smooth",
    "lineNumbers": "on",
    "wordWrap": "off",
    "mouseWheelZoom": true,
    "minimap": {
        "enabled": false
    },
    "themeCustomizations": {
        // General editor colors
        "colors": {
          "editor.background": "#282a36"
        },
    
        // Semantic highlighting token styles
        "rules": [
          {
            "token": "interface",
            "foreground": "50fa7b",
            "fontStyle": "underline"
          }
        ]
    }
}
```

> :bulb: See [IColors](https://microsoft.github.io/monaco-editor/typedoc/types/editor.IColors.html) and
> [ITokenThemeRule](https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor.ITokenThemeRule.html) for the
> definition of the `colors` and `rules` properties respectively.

The value keys that can be added to the `colors` property are not all clearly defined by the Monaco project but here are
some:

```json
{
    "colors": {
        "editor.foreground": "#f6f8fa",
        "editor.background": "#24292e",
        "editor.selectionBackground": "#4c2889",
        "editor.inactiveSelectionBackground": "#444d56",
        "editor.lineHighlightBackground": "#444d56",
        "editorCursor.foreground": "#ffffff",
        "editorWhitespace.foreground": "#6a737d",
        "editorIndentGuide.background": "#6a737d",
        "editorIndentGuide.activeBackground": "#f6f8fa",
        "editor.selectionHighlightBorder": "#444d56"
    }
}
```

Token names for use in the `rules` property are also not clearly defined. However, you can find the ones NetPad uses
in [aurora-theme.ts](https://github.com/tareqimbasher/NetPad/blob/main/src/Apps/NetPad.Apps.App/App/src/core/%40application/editor/monaco/aurora-theme.ts).

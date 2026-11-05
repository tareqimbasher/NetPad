# Styling

You can change the way NetPad looks by defining your own CSS styles in <kbd><kbd>Settings</kbd> > <kbd>
Custom CSS</kbd></kbd>.

Your styles take precedence over NetPad's own whatever their specificity: NetPad keeps its
stylesheets in a cascade layer (`@layer app`) and your custom styles sit outside it, so a plain class
selector is enough. Two exceptions:

- NetPad's own `!important` declarations (mostly Bootstrap's utility classes, such as
  `text-bg-warning` or `p-3`) win over yours even when yours are `!important` too. To restyle what
  a utility sets, use a class of your own in its place.
- The code editor's own styles sit outside the layer, as yours do, so a rule aimed inside the editor
  has to be at least as specific as the editor's rule it overrides.

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
what that value is. The tags are derived from the .NET type, so they are exact: a `decimal` is
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

A cell holding `null` carries no kind class, since there is no value to describe.

NetPad styles `numeric`, `boolean-true` and `boolean-false`. The kind classes it does not use are
emitted for you:

```css
.dump-container .negative {
    color: crimson;
}
```

## Theming NetPad

NetPad ships four **palette families**, each in a dark and a light version, for eight themes in all.
Pick the family under <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Palette</kbd></kbd> and
dark or light under **Mode** (or let Mode follow your desktop). A third setting,
[Background](#background), decides whether the backgrounds come from the palette or stay neutral.

| Family       | Accent     |
|--------------|------------|
| **steel**    | steel blue |
| **inkwell**  | iris       |
| **gunmetal** | patina     |
| **rosewood** | rose       |

Steel is the default.

A theme is a set of CSS variables defined on the class `.theme-<family>-<dark|light>`:
`.theme-steel-dark`, `.theme-rosewood-light`, and so on.

Every family defines **the same variables with the same meanings**; only the values differ. So a
custom style written against these names works in all eight themes, and redefining a variable in your
custom styles rethemes the app.

> :warning: **These variables changed with the UI refresh.** The old names (`--background-color`,
> `--active-color`, `--tab-background`, …) no longer exist. If you have custom styles from an
> earlier version, see [Migrating from the old variables](#migrating-from-the-old-variables) below.

### Background

A third setting, <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Background</kbd></kbd>,
decides where the backgrounds come from:

- **Neutral** (the default): one neutral set of backgrounds and text is used under *every* family,
  the same in each, so switching palettes changes the accent, the semantic colors and the syntax
  colors but leaves the room they sit in alone. There is one neutral set for dark and one for light.
- **Match palette**: every surface, line and text tier comes from the family. This is the palette as
  designed.

Neutral is an override, not a theme: it adds the class `.theme-neutral-bg` next to the theme class
on the root element and redefines the surface, line, text and frame variables there. Your custom styles
still win over it, so a rule on the theme class alone applies whichever background setting is in
use:

```css
.theme-steel-dark { --bg0: #101010; }
```

### The variables

Colors are layered: **surfaces** (`bg0`–`bg3`, `editor`) stack from the window ground up to raised
controls, **lines** separate them, **text** comes in three tiers, and the rest is semantic: one
accent plus run/ok/warn/err. The accent marks selection, focus and links; green means running;
amber means production or caution; red means error. That vocabulary is the same in every family:
switching palettes never changes what a color *means*.

The values below are the default family's own (Background set to Match palette). To read another
family's values, or the neutral ones, open the dev tools on the window and inspect its theme class.

```css
.theme-steel-dark {
    /* SURFACES */
    --bg0: #191b1e;                        /* ground: the window body             */
    --bg1: #24262a;                        /* panel: side panes, tab strip, cards */
    --bg2: #2a2d32;                        /* control: inputs, buttons, chips     */
    --bg3: #35383e;                        /* raised: hover, active cell          */
    --editor: #1e2023;                     /* editor & results surface            */
    /* FRAME: the titlebar, pane rails and status bar around the content */
    --frame: color-mix(in srgb, var(--bg2), var(--bg3));                   /* midway, bg2 to bg3 */
    --frame-hover: color-mix(in srgb, var(--frame), var(--text) 12%);      /* a hovered control  */
    --frame-open: color-mix(in srgb, var(--frame), var(--text) 22%);       /* an open pane       */
    --frame-focus: color-mix(in srgb, var(--frame), var(--accent) 30%);    /* the focused pane   */
    /* LINES */
    --line: #464b52;                       /* borders on controls                 */
    --line-soft: #303338;                  /* hairlines: panel seams, table rows  */
    /* TEXT */
    --text: #e8ebf0;
    --text-2: #a3aab6;
    --text-3: #6d7480;                     /* labels, disabled                    */
    /* ACCENT: selection, focus, active, links. Never "go". */
    --accent: #64a7e8;
    --accent-bright: #92c4f2;              /* emphasis                            */
    --accent-dim: #1d3a58;                 /* selection fill                      */
    --accent-text: #0b1420;                /* text on an accent fill              */
    --accent-wash: rgba(100, 167, 232, 0.1);  /* selection wash                   */
    --accent-rgb: 100, 167, 232;           /* channels, for rgba() tints          */
    /* SEMANTICS */
    --run: #7bc88f;
    --run-text: #0d1a12;
    --run-text-rgb: 13, 26, 18;            /* channels, for tints on a run fill    */
    --run-rgb: 123, 200, 143;
    --ok: #7bc88f;
    --ok-rgb: 123, 200, 143;
    --warn: #d9a154;
    --warn-rgb: 217, 161, 84;
    --err: #e0796d;
    --err-text: #ffffff;                   /* text/glyphs on an err fill           */
    --err-rgb: 224, 121, 109;
    /* OVERLAYS */
    --hover: rgba(255, 255, 255, 0.06);
    --hover-strong: rgba(255, 255, 255, 0.12);
    --backdrop: rgba(0, 0, 0, 0.55);       /* dialog scrim                        */
    /* SYNTAX ACCENTS: the editor theme is built from these; they also tint code chrome (SQL log) */
    --syntax-keyword: #6ca6de;
    --syntax-type: #58c4ae;
    --syntax-string: #cd9a76;
    --syntax-number: #a9c99b;
    --syntax-method: #d8d4a0;
    --syntax-comment: #679758;
    --syntax-value: #9fd0f0;
    --syntax-punctuation: #8a8f98;
    --syntax-regex: #d7bc85;               /* regex literals, escape sequences     */
    /* SHADOWS */
    --shadow: 0 24px 70px rgba(0, 0, 0, 0.55), 0 4px 18px rgba(0, 0, 0, 0.4);
    --shadow-sm: 0 10px 30px rgba(0, 0, 0, 0.45);
    /* CHECKBOX / RADIO / SELECT GLYPHS: inline SVG, so the color is baked into the URL */
    --check-mark-image: url("data:image/svg+xml,...");
    --radio-mark-image: url("data:image/svg+xml,...");
    --select-caret-image: url("data:image/svg+xml,...");
}

.theme-steel-light {
    /* SURFACES */
    --bg0: #edf0f4;
    --bg1: #f6f8fa;
    --bg2: #ffffff;
    --bg3: #e2e7ed;
    --editor: #fbfcfe;
    /* FRAME */
    --frame: var(--bg3);
    --frame-hover: color-mix(in srgb, var(--frame), var(--text) 12%);
    --frame-open: color-mix(in srgb, var(--frame), var(--text) 22%);
    --frame-focus: color-mix(in srgb, var(--frame), var(--accent) 25%);
    /* LINES */
    --line: #d0d5dc;
    --line-soft: #e0e5eb;
    /* TEXT */
    --text: #23272e;
    --text-2: #575e69;
    --text-3: #8b929c;
    /* ACCENT */
    --accent: #2e6ec6;
    --accent-bright: #1f5aad;
    --accent-dim: #d6e4f8;
    --accent-text: #ffffff;
    --accent-wash: rgba(46, 110, 198, 0.1);
    --accent-rgb: 46, 110, 198;
    /* SEMANTICS */
    --run: #2f9550;
    --run-text: #ffffff;
    --run-text-rgb: 255, 255, 255;         /* channels, for tints on a run fill    */
    --run-rgb: 47, 149, 80;
    --ok: #2f9550;
    --ok-rgb: 47, 149, 80;
    --warn: #ac7014;
    --warn-rgb: 172, 112, 20;
    --err: #c44f42;
    --err-text: #ffffff;                   /* text/glyphs on an err fill           */
    --err-rgb: 196, 79, 66;
    /* OVERLAYS */
    --hover: rgba(40, 50, 60, 0.07);
    --hover-strong: rgba(40, 50, 60, 0.14);
    --backdrop: rgba(40, 50, 60, 0.35);
    /* SYNTAX ACCENTS */
    --syntax-keyword: #2159c2;
    --syntax-type: #26798f;
    --syntax-string: #a84a3b;
    --syntax-number: #128557;
    --syntax-method: #7d6226;
    --syntax-comment: #3f8b40;
    --syntax-value: #29418f;
    --syntax-punctuation: #6e747d;
    --syntax-regex: #a88b3b;
    /* SHADOWS */
    --shadow: 0 24px 70px rgba(40, 50, 60, 0.18), 0 4px 18px rgba(40, 50, 60, 0.12);
    --shadow-sm: 0 10px 30px rgba(40, 50, 60, 0.14);
    /* CHECKBOX / RADIO / SELECT GLYPHS */
    --check-mark-image: url("data:image/svg+xml,...");
    --radio-mark-image: url("data:image/svg+xml,...");
    --select-caret-image: url("data:image/svg+xml,...");
}
```

There are also two font stacks, shared by both themes: `--font-sans` and `--font-mono`.

To override a variable, define it in your custom styles.

```css
.theme-steel-dark {
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
.theme-steel-dark {
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
scale above; several old names collapsed onto one new one (every panel background is now `--bg1`,
for example). Map your custom styles like this:

| Old variable                                                                 | Replace with                                                                   |
|------------------------------------------------------------------------------|--------------------------------------------------------------------------------|
| `--background-color`                                                         | `--bg0`                                                                        |
| `--script-toolbar-background`                                                | `--editor`                                                                     |
| `--list-background`                                                          | `--bg1`                                                                        |
| `--pane-background`, `--pane-toolbar-background`                             | `--bg1`                                                                        |
| `--titlebar-background`, `--statusbar-background`, `--pane-ribbon-background` | `--frame`                                                                      |
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
| `--tab-inactive-text-color`                                                  | `--text-2`                                                                        |
| `--action-icon-color`                                                        | `--text-2`                                                                        |
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
| `--color-full`, `--color-contrast-full`, `--box-shadow-bottom`               | removed: style the element directly                                            |
| `--box-shadow-bottom-sm`, `--box-shadow-bottom-and-sides`                    | removed: style the element directly                                            |
| `--scrollbar-track-color`                                                    | removed: the track is transparent; style `::-webkit-scrollbar-track` directly  |

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

The default, **Auto (match palette)**, paints the editor in the palette family and mode you
picked under General. It uses that family's **aurora** theme, which is built from the theme
variables above rather than from a palette of its own, so the editor always matches the rest of the
window. The `--syntax-*` variables are the ones it reads: redefine those in your custom styles and
the editor follows along.

The picker also offers:

- **Visual Studio**: the editor colors NetPad used before the UI refresh. Like Auto it follows
  light/dark mode, but its colors are its own, so it looks the same whichever palette family the
  app is in.
- **Visual Studio (palette background)**: Visual Studio's syntax colors, everything around them
  from your palette: the editor surface, gutter, selection, current line and widgets are the
  aurora ones. Like Auto it follows the family, the mode and the Background setting.
- **Aurora: Steel Dark** … **Aurora: Rosewood Light**: one aurora theme per family and mode,
  pinned. Picking one keeps the editor in that palette even when the app is in another.
- Everything from the `monaco-themes` library.

Picking anything other than Auto opts the editor out of following the app's palette.

> :warning: Values the editor reads must be **6-digit hex** (`#a89df0`). The editor cannot use
> `rgb()`, `color-mix()` or named colors, and falls back to a neutral gray for anything else, so a
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

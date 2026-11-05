# Adding a Theme

A NetPad theme is a **palette family**: one set of colors in a dark and a light **ground**. The app
ships four (steel, inkwell, gunmetal, rosewood), and everything downstream (the app
chrome, the Monaco editor theme, highlight.js, the settings cards, the pre-boot paint) is generated
from the family's two color maps. Adding one is four small edits and a contrast check.

## Concepts

| Piece | Responsibility | File |
|---|---|---|
| Family palette | The ~30 colors a family actually chooses | `App/src/styles/themes/families/_<family>.scss` |
| `build-theme()` | Turns a palette into the full token set (tint channels, overlays, shadows, glyph URIs) | `themes/common/_build.scss` |
| SCSS registry | The family ids, in canonical order; the `.theme-<family>-<ground>` class names | `themes/_registry.scss` |
| Neutral grounds | The one ground-and-text set the Background setting slides under any family | `themes/_neutral-grounds.scss` |
| `AppTheme.families` | The same list for the TypeScript side | `core/@application/themes/app-theme.ts` |
| highlight.js picks | One shipped hljs theme per family and ground | `styles/vendors/_highlightjs.scss` |

A family's id is the middle part of its CSS classes (`.theme-steel-dark`), the value stored in
settings (`appearance.themeFamily`), and what the settings picker calls the family.

## The rules a family must keep

The palette changes; **what a color means does not**. Every family keeps the same semantics, because
the whole app reads them:

- **Green = run.** A green dot means "running" on every surface. `run` / `ok` stay green.
- **Amber = production or caution.** The PROD pill, restart chips and warnings are `warn`.
- **Red = error.** `err` is error and destructive, nothing else.
- **The accent is selection, focus, active and links, never "go".** It is the only color a family
  is really free to choose.

That freedom is narrower than it looks: green, amber/orange/yellow and red are reserved by the rules
above, so an accent must not land in those hue lanes. A selected row that is also production-flagged
would otherwise mush into one color. What is left is violet, blue, teal and pink, which is exactly
the shipped slate. A new family is therefore usually a new *character* (different
grounds, different syntax, a different temperature), not simply a new accent hue.

**The accent must have a hue.** A colorless (grey, white or black) accent does not work: the pane
rail tells the focused pane from the other open ones by mixing the frame toward the accent rather
than toward the text, and the two mixes differ only in hue. With a grey accent they come out the
same color, and the focused pane cannot be seen.

One more constraint comes from the **Background** setting: a user can replace any family's grounds
with the shared neutral set, so a family's accent, semantic and syntax colors have to clear the bars
and read well on those grounds too, not only on the ones the family drew them against. A hue that
only works because it sits on its own tinted ground is a family that half-breaks the moment someone
picks Neutral.

## 1. Write the palette

Copy an existing family file (`families/_gunmetal.scss` is the most neutral starting point) and
fill in the two grounds. Only the keys in `$palette-keys` (`themes/common/_build.scss`) are yours to
choose; a missing one is a build error, so there is no way to half-add a family:

```scss
// "brass": warm neutral grounds, an amber-free gold-adjacent accent
$brass: (
    dark: (
        bg0: #16130f,        // ground (window body)
        bg1: #1d1a15,        // panels: side panes, tab strip, cards
        bg2: #251f19,        // controls: inputs, buttons, chips
        bg3: #2f2921,        // raised: hover, active cell
        editor: #181410,     // one step darker than panels, so the editor reads as the stage
        line: #3f382e,       // control borders
        line-soft: #2b251e,  // hairlines: panel seams, rows
        text: #f0eae0, text-2: #b0a696, text-3: #7a7264,
        accent: #…, accent-bright: #…, accent-dim: #…, accent-text: #…,
        run: #…, run-text: #…, ok: #…, warn: #…, err: #…, err-text: #…,
        syntax-keyword: #…, syntax-type: #…, syntax-string: #…, syntax-number: #…,
        syntax-method: #…, syntax-comment: #…, syntax-value: #…, syntax-punctuation: #…,
        syntax-regex: #…,
    ),
    light: (
        // … the same keys, plus:
        shade: #3a3020,      // light grounds only: what overlays and shadows are tinted with
    ),
);
```

Notes from the shipped families:

- **`accent-dim` is a fill, not a tint of the accent.** On dark grounds it is a deep, desaturated
  version of the accent (selection background); on light grounds a pale one. Body text sits on it.
- **`accent-bright` is emphasis on dark fills**: lighter than `accent` on a dark ground, *darker*
  on a light one.
- **`err-text` / `run-text` / `accent-text` are what sits on those fills**, usually white on light
  grounds and a near-black of the family's own hue on dark ones.
- **`syntax-regex` has no counterpart in the mockups**: take the family's `syntax-string` hue and
  step it toward gold (hue ≈ 40° on dark grounds, ≈ 44° on light, keeping saturation and lightness),
  so regex literals read as a sibling of strings without being mistaken for them.
- **Comments are meant to be the quietest text in the editor**, around 2.6:1 against `editor`.
- Everything else (`*-rgb` channel triplets, `accent-wash`, `hover` / `hover-strong`, `backdrop`,
  the two shadows, the checkbox/radio/select glyph data URIs, the window `frame` and its hover, open
  and focus states) is derived. Do not hand-write them.

## 2. Register the family

Two lists, which must stay in the same order (the order every family picker shows):

```scss
// App/src/styles/themes/_registry.scss
$theme-family-ids: steel, inkwell, gunmetal, rosewood, brass;
```

```scss
// App/src/styles/themes/_index.scss
@use "families/brass";

$palettes: (
    // …
    brass: brass.$brass,
);
```

```ts
// App/src/core/@application/themes/app-theme.ts
public static readonly families: readonly ThemeFamily[] = [
    // …
    {id: "brass"},
];
```

The id is also what the editor picker calls the family's two aurora themes ("Aurora: Brass Dark",
"Aurora: Brass Light").

## 3. Pick a highlight.js theme per ground

highlight.js colors the code blocks in dump output and the IL view. NetPad only uses themes that
already ship with the `highlight.js` package (no new dependency, no hand-written vendor CSS):

```scss
// App/src/styles/vendors/_highlightjs.scss
$hljs-themes: (
    // …
    brass: (dark: "kimbie-dark", light: "kimbie-light"),
);
```

Pick by palette, not by name: the closest shipped theme to the family's own `syntax-*` accents.

## 4. Check contrast

A family that only differs in values can introduce exactly one new failure mode: unreadable
combinations. Check every load-bearing pair, in both grounds, before calling the family done:

| Pair | Bar | Carries |
|---|---|---|
| `text` on `bg0` / `bg2`, `text-2` on `bg1`, `text` on `accent-dim`, `syntax-value` on `editor` | 4.5:1 | body text |
| `accent` / `accent-bright` on `bg0`–`bg2` | 3:1 | selection, focus, links |
| `ok` / `warn` / `err` on `bg1` | 3:1 | status marks, chips, dots |
| every other `syntax-*` on `editor` | 3:1 | code |
| `accent-text` on `accent`, `run-text` on `run`, `err-text` on `err` | 4:1 / 3.5:1 / 2.9:1 | labels on filled buttons |
| `text-3` on `bg1` | 2.8:1 | labels and disabled text |
| `syntax-comment` on `editor` | 2.5:1 | comments, deliberately quiet |
| `text-2` on `frame` | 4.5:1 | resting controls and status text on the titlebar, rails and status bar |
| `accent-bright` on `frame-focus` | 3:1 | the focused pane's rail button |
| `frame` against `bg1` / `editor` | 1.12:1 / 1.15:1 | the frame's edge with the content |
| `frame-hover` / `frame-open` / `frame-focus` against `frame` | 1.2:1 / 1.45:1 / 1.3:1 | frame states |

The comment, label and filled-button rows sit under the generic bars on purpose: they are where the
shipped families already sit, and matching them keeps a new family from being quieter than the
design accepts. The frame rows are tonal steps, not text, so their bars are the steps the shipped
families make. The frame is derived (the midpoint of `bg2` and `bg3` on a dark ground, `bg3` on a
light one, with its states mixed toward `text` or the accent), so compute it before checking it.

Run the same table twice: once on the family's own grounds, once with the neutral grounds from
`themes/_neutral-grounds.scss` substituted for the family's surface, line and text values. Neutral
is the default background, so both runs take the same bars, with no allowance.

## What you do *not* have to touch

The rest follows from the registry, and adding a family here is the test that it does:

- **The Monaco editor theme.** `aurora-theme.ts` builds the editor theme by reading the design
  tokens off a probe element carrying the theme class, so a family's editor theme *is* its palette.
  `MonacoThemeManager` derives one aurora entry per family and ground from `AppTheme.families`.
- **The settings cards.** The Palette row in <kbd>Settings > General</kbd> repeats over
  `AppTheme.families` and previews each family in its own two theme classes.
- **The pre-boot paint.** `ThemeBootCache` mirrors the active family's two ground colors into
  `localStorage`, and the inline script in `index.html` resolves them before the SPA loads.
- **The Background setting.** Its neutral grounds are family-independent and ship as an override
  class, so a new family gets them for free, but see the constraint above.
- **Bootstrap's palette, the app chrome, dialogs, dumps.** They all name tokens, never colors.

## The custom-CSS contract

`docs/wiki/Styling.md` documents the variables a theme defines. That page is the promise NetPad
makes to users who write custom CSS: *every* family defines the same variables, with the same
meanings, so one custom stylesheet works in all of them. Adding a family does not change that page.

Adding, renaming or revaluing a **token** does: update the page's variable listing (it documents the
default family's values) in the same change, and check what you shipped against what you documented:

```
cd src/Apps/NetPad.Apps.App/App
npx sass --quiet --load-path=node_modules --load-path=src src/styles/main.scss /tmp/out.css
grep -A80 '^\.theme-steel-dark {' /tmp/out.css
```

A token that only some families define is the failure this catches: `build-theme()` guarantees the
derived ones, but a `$palette-keys` addition has to be filled in by every family file.

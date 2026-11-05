# Adding an Icon

NetPad's icons are inline SVG rendered by the `<np-icon>` custom element from a vendored registry.
There is no icon font and no icon package; the whole set lives in one TypeScript file.

## Concepts

| Piece | Responsibility | File |
|---|---|---|
| `icons.ts` | The glyph set: name → SVG body, plus `iconSvgMarkup()` / `createIconElement()` | `core/@application/ui/np-icon/icons.ts` |
| `IconName` | Union of every key in the registry, so an unknown name is a compile error | same file |
| `<np-icon>` | Renders a glyph into its host element and mirrors the name onto `data-icon` | `core/@application/ui/np-icon/np-icon.ts` |
| Base styles | `np-icon` box + the `1em`-square `svg` | `styles/base/_icons.scss` |

Marks are drawn from [Lucide](https://lucide.dev) (ISC licensed). They are **vendored, not
imported**: part of the set is custom or simplified for small sizes, and vendoring pins the drawing
so an upstream redraw can never silently change the UI.

Registry keys are **NetPad's vocabulary, not Lucide's**. A surface asks for `delete`, never for
`trash-2`, so the drawing behind a meaning can be swapped without touching a single call site.

## Using an icon

```html
<np-icon name="save"></np-icon>
<np-icon name="delete" class="action-icon row-action"></np-icon>
<np-icon name.bind="expanded ? 'folder-open' : 'folder'" class="tree-glyph glyph-folder"></np-icon>
```

**Size with `font-size`, tint with `color`.** The glyph is drawn `1em` square in `currentColor`, so
it behaves exactly like text:

```scss
.pane-icon {
    font-size: 16px;
    color: var(--text-2);
}
```

Do not size a glyph with `width`/`height`. (A clickable icon takes the `action-icon` class, whose
`width`/`height` size the hit box around the glyph, not the glyph.) Sizes in this design system run
**11–16px**; 24px is for display contexts only.

### Sizing through the `icon-size` mixin

A stroke is drawn in the glyph's own 24-unit space, so its rendered weight shrinks along with the
glyph. Glyphs draw at a stroke of **2**, about a pixel at 13px, and at 11px that falls to 0.9px and
the mark goes faint. Glyphs rendered at **12px or less** carry **2.35** to compensate, and the
`icon-size` mixin (`styles/_variables.scss`) applies the rule so the size and the weight cannot be
set apart:

```scss
.tree-glyph {
    @include icon-size(12px);   // font-size: 12px + the compensated stroke
}
```

Use it wherever a rule decides what size a glyph renders at, including on a container the glyphs
inherit their size from, such as a row's meta slot. Setting a bare `font-size` on a small icon is
the bug this mixin exists to prevent.

The weight comes from the `--icon-stroke` custom property, which the mixin sets and every glyph
reads, falling back to its own drawn weight. A surface with a reason to override the rule can set
the property directly; the explorer's compound add buttons keep the heavy stroke at 13px because
two glyphs sitting a few pixels apart fuse at the normal weight.

To style one specific glyph, select the mirrored attribute rather than inventing a class:

```scss
np-icon[data-icon="app-update"] {
    color: var(--accent);
}
```

### From code, outside a template

Anything that builds DOM imperatively (dump output arrives as raw HTML and never passes through a
template) uses the registry directly. It is the same source, so it cannot drift:

```typescript
import {createIconElement, iconSvgMarkup} from "@application/ui/np-icon/icons";

const caret = createIconElement("chevron-down");   // SVGElement | null
const markup = iconSvgMarkup("chevron-down");      // string, "" if the name is unknown
```

## Category tinting

Glyphs are monochrome; meaning comes from the tint of the surface they sit on, not from the glyph:

| Category | Token | Where |
|---|---|---|
| Data / connections | `--syntax-type` (teal) | `.glyph-db` |
| Scripts | `--accent` (iris) | `.glyph-script` |
| Folders, keys | `--warn` (amber) | `.glyph-folder`, `.glyph-key` |
| States | `--ok` / `--err` / `--warn` | run status, validation, notifications |
| Everything else | `--text-2` at rest on a control that stands on its own, `--text` on hover. `--text-3` only on a glyph you cannot click, or on one inside a row or control whose label already carries the contrast (a disclosure caret, a row or menu-row icon, a select's caret). Clickable text is never `--text-3` | default |

Glyphs are outline. A glyph is filled only where the filled shape carries its meaning, and two do:
the GitHub mark, a brand mark that has no outline form, and the syntax tree's node diamond, an 11px
marker whose fill is what tells node rows from token rows. Run and Stop are outline too: Run is
Lucide's `play`, and Stop is Lucide's `square`, the same drawing as the window's Maximize. Maximize
only appears in the window controls and Stop always carries its label, so the shared shape does
not confuse.

## Adding a glyph

1. **Find the mark.** Search [lucide.dev](https://lucide.dev) and copy the SVG. Use the path
   elements only; the registry supplies the `<svg>` wrapper, stroke width, and paint.

2. **Check it at 11px.** Open the gallery (below) after adding it. Marks with fine interior detail
   turn to mush at tree-row size; simplify by dropping detail rather than by scaling up. Several
   entries are already simplified variants for this reason.

3. **Add the entry**, keyed by what it *means* in NetPad, not by Lucide's name:

   ```typescript
   "db-view": {body: `<rect x="3" y="4" width="18" height="16" rx="2"/>…`}, // lucide: eye
   ```

   The trailing note is required: it is the only record of where the drawing came from, and
   re-syncing or auditing against upstream is impossible without it. Use `// custom` for marks with
   no upstream (`github`).

   Options:

   - `filled: true`: only for a mark whose fill carries its meaning (`github`, `syntax-node`).
     Default is stroked at 2.
   - `strokeWidth: HEAVY` (2.35): for marks too sparse to read at the default weight (`add`,
     `minus`, `close`).

4. **If two meanings share one drawing, share one const.** `ORDERED_LIST`, `HASH` and `SQUARE`
   (`stop` and `window-maximize`) exist for exactly this. Duplicating the body invites a rename of
   one meaning to silently empty the other, which is a real bug this codebase has already had.

5. **Use it.** `IconName` is inferred from the registry, so a typo in TypeScript fails to compile.
   Names written as template strings are checked by
   `test/core/application/ui/np-icon.spec.ts`, which asserts every `name="…"` literal and every
   inline-ternary branch in every template resolves to a real glyph. **This test is the safety net
   for the one failure mode that is otherwise silent.** An unknown name renders nothing at all: no
   error, no console warning, just a gap on a screen nobody happened to open.

## The gallery

```bash
just web-run-frontend      # or: npm run start-web
```

Then open <http://localhost:9000/icon-gallery.html>.

It renders every glyph at 11 / 13 / 15 / 24px, in **both themes side by side**, with each name and
its `filled` / heavy-stroke flags. It draws through `iconSvgMarkup()` and loads the shipped
stylesheet, so it shows exactly what the app draws.

The gallery is a development aid: its webpack entry is only registered in non-production builds
(`webpack.config.js`), so no gallery page or chunk exists in a packaged app.

## Removing an icon

Delete the entry and fix the call sites the compiler and `np-icon.spec.ts` point at. Check
`grep -rn "data-icon=" src` too, since a CSS rule selecting a removed glyph fails silently.

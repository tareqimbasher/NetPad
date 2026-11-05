# Stylesheets

The SPA's shared styles live in `App/src/styles`; each component keeps its own `.scss` next to its
template. Everything is Sass modules (`@use`); only `vendors/_bootstrap.scss` still `@import`s,
because Bootstrap's SCSS is not module-clean.

## Layout and the emission rule

`main.scss` is the only entry point, and it reads as a table of contents: `vendors/`, `themes/`,
`base/` (document, `<np-icon>`, utilities), `primitives/` (badges, controls, lists, text) and
`layout/` (window, rail window, cards, forms, dialog). **Only `main.scss` emits shared CSS.** A
component stylesheet loads two non-emitting modules and nothing else:

```scss
@use "styles/variables" as *;    // sizes, type scale, radii, fonts, z-scale, the icon-size mixin
@use "styles/placeholders";      // shared rule bundles
```

Loading anything that emits (a partial with rules in it) copies that CSS into every component
bundle that loads it.

## The `app` cascade layer

Every stylesheet the build compiles lands in `@layer app`. A PostCSS plugin (`postcss-layer.js`,
configured in `webpack.config.js`) wraps each file, so a new stylesheet is layered without doing
anything. User custom CSS is not layered, which is why it beats any app rule whatever its
specificity. Two things stay outside the layer, both because Monaco injects unlayered rules at
runtime that would beat any layered rule:

- Monaco's own CSS. The plugin skips everything under `node_modules`, and in practice only Monaco
  imports its CSS that way. Vendor CSS that the app's stylesheets pull in (Bootstrap, dragula, the
  highlight.js themes) is part of the layer.
- `text-editor-monaco.scss`. A rule that restyles Monaco's DOM goes there, never in a layered file,
  or it silently stops applying.

An `!important` in the layer outranks a user's `!important`, so a user cannot override it at all.

## Placeholders and mixins

A **placeholder** (`%label-caps`, `%meta`, `%floating-surface`, …) is a named bundle a rule *is*:
`@extend` it. It emits nothing unless extended, and `@extend` only reaches placeholders loaded
into the same compilation, which is why components load the module themselves. The extended rule
is emitted where the placeholder is defined, *before* the extending rule, so a placeholder loses
ties against the extending rule's own block. When a bundle must win such a tie (a `.selected`
against the same block's `:hover`), write a **mixin** (`selected-spine`), which emits in place. A
mixin is also the choice for anything parameterized (`icon-size($size)`).

## Custom-property hooks

Per-surface tuning goes through custom properties rather than overriding a primitive's rules:
`--action-icon-size` and `--action-icon-hover` (the clickable-icon box and its hover fill),
`--icon-stroke` (glyph weight; set by `icon-size`), `--tree-depth` (tree indentation),
`--group-label-inset` (a group label's side margin, as a length: `0px`, not `0`), `--row-glyph`
(see below), `--rail-nav-glyph`, `--vs-glyph-color`.

## Shared bundles worth knowing

- **Command menus** (the titlebar menus and their submenus, the output pane's Export and Format, every
  context menu) are one set of placeholders in `_placeholders.scss`: `%command-menu` (the surface),
  `%command-menu-item` (the 28px row with its hover), `%command-menu-glyph`, `%command-menu-label`,
  `%command-menu-shortcut` and `%command-menu-divider`. Each menu extends them onto its own markup and
  keeps only what is specific to it. They are placeholders, not a mixin: nothing in a menu's own block
  has to beat them, and they take no parameters. A menu's glyph takes its label's color unless it is
  tinted, and a menu in which no row has a glyph renders no glyph column. Option lists, such as the
  value pickers, are not command menus and keep their own rows.
- **A dimmed glyph brightens with its row.** A text-3 glyph or disclosure caret inside a row extends
  `%row-glyph` (`color: var(--row-glyph, var(--text-3))`), and the row sets
  `--row-glyph: currentcolor` in the states that brighten its label (hover, selected), so the glyph
  takes the label's color. Tinted glyphs set their own color and are unaffected; a tint hook such as
  `--rail-nav-glyph` or `--vs-glyph-color` is read before `--row-glyph`. Meta text (counts, types,
  chips) never reads it. Dump headers set the property to `var(--text)` instead, because the type
  line's own text stays dim on hover.

## Conventions

- **State classes are bare words:** `active`, `selected`, `open`, `disabled`, `dirty`, `current`.
  No `is-` prefix; Bootstrap's `is-invalid` is the one exception. Where `active` would be
  ambiguous, pick a distinct word rather than a prefix.
- **Class names are full words.** Component-scoped prefixes that read in context are fine
  (`vs-*` inside the value select); abbreviations are not.
- **No pointer cursor on app chrome.** Buttons, icons and rows keep the default arrow, as in a
  desktop app; only a link (an anchor with an `href`) shows the pointer. Bootstrap's button pointer
  is switched off (`$enable-button-pointers`), and `base/_document.scss` gives the arrow to `.btn`
  and to anchors without an `href`, which are actions.
- **No text cursor over chrome text.** `body` sets `cursor: default`, so labels, rows and captions
  show the arrow instead of the I-beam, and a new surface needs nothing. Text fields and Monaco set
  the text cursor themselves. Output content opts back in with `cursor: auto` (dump output, the IL
  view), because it is a document the user selects from. Dump output's collapsible headers keep the
  pointer for the same reason; the output also exports as a standalone page.
- **No focus ring on a focusable container.** An element that takes focus but is not a control (a
  pane, a viewer, a dialog host) extends `%focusable-surface`. It draws no ring, and the controls
  inside it keep theirs. Unlike the cursor rule this is opt-in: a new focusable container needs it.
- **Recolor a Bootstrap button through its `--bs-btn-*` properties.** Bootstrap's hover, focus,
  pressed and disabled rules outrank a component's own selector and paint from those properties,
  so a plain `background` or `color` on a `.btn` holds only at rest. Set the rest, hover, active
  and disabled variants instead (`.btn-link` in `vendors/_bootstrap.scss` is the model).
- **Colors come from theme tokens** (`var(--bg1)`, `rgba(var(--accent-rgb), …)`), never literals.
  The token set is documented in `docs/wiki/Styling.md`.
- **A stylelint disable comment carries its reason after ` -- `**
  (`// stylelint-disable-next-line selector-class-pattern -- highlight.js names this class`).
  With a single dash, stylelint reads the reason as part of the rule name and the disable does
  nothing.
- **Audit before adding a global class.** Monaco renders its widgets inside the app DOM and owns
  generic names (`.tree`, `.monaco-*`), and the shared stylesheets already define primitives such
  as `.row-action`. Grep `node_modules/monaco-editor/min/vs` and `src/styles` for a name before
  adding it globally.

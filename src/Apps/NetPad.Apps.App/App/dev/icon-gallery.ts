/**
 * Dev-only gallery of the whole `<np-icon>` glyph set, at every size the design system uses.
 * Reachable only from the dev server (http://localhost:9000/icon-gallery.html)
 */
import {Icon, icons, iconSvgMarkup} from "@application/ui/np-icon/icons";
import "../src/styles/main.scss";
import "./icon-gallery.scss";

const SIZES = [11, 13, 15, 24];
const THEMES = [
    {name: "inkwell (dark)", cls: "theme-netpad-dark"},
    {name: "vellum (light)", cls: "theme-netpad-light"},
];

const entries = Object.entries(icons as Record<string, Icon>)
    .sort(([a], [b]) => a.localeCompare(b));

function flags(icon: Icon): string {
    const marks = [
        icon.filled ? "filled" : null,
        icon.strokeWidth ? `stroke ${icon.strokeWidth}` : null,
    ].filter(Boolean);
    return marks.length ? marks.join(" · ") : "";
}

function card(name: string, icon: Icon): string {
    const sizes = SIZES
        .map(px => `<span class="size size-${px}" title="${px}px">${iconSvgMarkup(name)}</span>`)
        .join("");

    return `
        <div class="card">
            <div class="sizes">${sizes}</div>
            <div class="meta">
                <code class="name">${name}</code>
                <span class="flags">${flags(icon)}</span>
            </div>
        </div>`;
}

function pane(themeName: string, themeClass: string): string {
    return `
        <section class="pane ${themeClass}">
            <header>
                <h1>${themeName}</h1>
                <span class="count">${entries.length} glyphs · ${SIZES.join(" / ")}px</span>
            </header>
            <div class="grid">${entries.map(([name, icon]) => card(name, icon)).join("")}</div>
        </section>`;
}

document.body.innerHTML = `<main class="gallery">${THEMES.map(t => pane(t.name, t.cls)).join("")}</main>`;
document.title = `NetPad icons — ${entries.length}`;

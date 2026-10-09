import {AppTheme} from "@application/themes/app-theme";
import {CustomCss} from "@application/themes/custom-css";

export class HtmlExportService {
    /**
     * Builds a standalone HTML document from rendered output, carrying the styles it needs to look
     * the way it does in the app: the output pane's stylesheets, the active theme, the shared rules
     * (Bootstrap's included) for the classes the output uses, and the user's custom CSS.
     */
    public static export(title: string, output: Element): string {
        const metas = [...new Set<string>(
            Array.from(document.head.querySelectorAll("meta"))
                .map(s => s.outerHTML)
        )].join("\n");

        const outputSheets = Array.from(document.head.querySelectorAll("style"))
            .filter(s => s.textContent?.includes("output-pane"));

        const themeClasses = Array.from(document.documentElement.classList)
            .filter(c => c.startsWith(AppTheme.cssClassPrefix));

        const fragment = document.createRange().createContextualFragment(output.outerHTML);
        fragment.querySelectorAll("np-icon, .np-icon").forEach(x => x.remove());

        const outputClasses = new Set(Array.from(fragment.querySelectorAll("[class]"))
            .flatMap(e => Array.from(e.classList)));

        const customCss = CustomCss.element;

        const sharedCss = HtmlExportService.collectRules(
            selector => HtmlExportService.isScopedToTheme(selector, themeClasses)
                || HtmlExportService.isStyledByClasses(selector, outputClasses),
            new Set<Node | null>([...outputSheets, customCss]));

        // The export's own rules join the app's layer, so custom CSS outranks them as it does the app's.
        const documentCss = `@layer app {
html { font-size: ${getComputedStyle(document.documentElement).fontSize}; }
body {
    margin: 0;
    background: var(--bg0);
    color: var(--text);
    font-family: var(--font-sans);
}
output-pane { display: block; }
}`;

        const pinnedCss = HtmlExportService.pinUndefinedProperties(
            [sharedCss, documentCss, ...outputSheets.map(s => s.textContent ?? "")].join("\n"));

        return `<!DOCTYPE html>
<html lang="en" class="${themeClasses.join(" ")}">
<head>
<title>${title}</title>
${metas}
<style>
${pinnedCss}
${sharedCss}
${documentCss}
</style>
${[...new Set(outputSheets.map(s => s.outerHTML))].join("\n")}
${customCss?.outerHTML ?? ""}
</head>
<body>
<output-pane>${fragment.firstElementChild?.outerHTML}</output-pane>
</body></html>`;
    }

    /**
     * Each collected rule stays inside the `@layer`, `@media` and `@supports` blocks that hold it,
     * so it applies with the same precedence and under the same conditions as in the app.
     */
    private static collectRules(include: (selector: string) => boolean, skip: Set<Node | null>): string {
        const walk = (rules: CSSRuleList): string[] => {
            const collected: string[] = [];

            for (const rule of Array.from(rules)) {
                if (rule instanceof CSSStyleRule) {
                    if (rule.selectorText.split(",").some(s => include(s.trim()))) {
                        collected.push(rule.cssText);
                    }
                    continue;
                }

                const prelude = HtmlExportService.groupPrelude(rule);
                if (prelude === null) continue;

                const inner = walk((rule as CSSGroupingRule).cssRules);
                if (inner.length) collected.push(`${prelude} {\n${inner.join("\n")}\n}`);
            }

            return collected;
        };

        const collected: string[] = [];

        for (const sheet of Array.from(document.styleSheets)) {
            if (skip.has(sheet.ownerNode)) continue;

            let rules: CSSRuleList;
            try {
                rules = sheet.cssRules;
            } catch {
                continue;
            }

            collected.push(...walk(rules));
        }

        return collected.join("\n");
    }

    private static groupPrelude(rule: CSSRule): string | null {
        if (rule instanceof CSSLayerBlockRule) return `@layer ${rule.name}`;
        if (rule instanceof CSSMediaRule) return `@media ${rule.media.mediaText}`;
        if (rule instanceof CSSSupportsRule) return `@supports ${rule.conditionText}`;
        return null;
    }

    /**
     * The theme's tokens can be spread over more than one class on the root (the palette's, plus
     * the background modifier), so a rule travels with the export when the export has every class
     * it is scoped to.
     */
    private static isScopedToTheme(selector: string, themeClasses: string[]): boolean {
        if (themeClasses.length === 0) return false;

        const scope = selector.split(" ")[0];

        return /^(\.[\w-]+)+$/.test(scope)
            && scope.split(".").filter(Boolean).every(c => themeClasses.includes(c));
    }

    /**
     * Every class the selector names must be on an element of the output. This brings a dump's own
     * `Dump(css: ...)` classes into the export and nothing else of Bootstrap's.
     */
    private static isStyledByClasses(selector: string, classes: Set<string>): boolean {
        const required = selector
            .replace(/:not\([^)]*\)/g, "")
            .match(/\.-?[_a-zA-Z][\w-]*/g);

        return !!required && required.every(c => classes.has(c.slice(1)));
    }

    /**
     * Rules without a class never travel, so the `:root` variables Bootstrap and the app define are
     * missing from the export; each one the CSS reads is pinned to the value it computes to in the app.
     */
    private static pinUndefinedProperties(css: string): string {
        const defined = new Set(Array.from(css.matchAll(/(--[\w-]+)\s*:/g), m => m[1]));
        const read = new Set(Array.from(css.matchAll(/var\(\s*(--[\w-]+)/g), m => m[1]));
        const root = getComputedStyle(document.documentElement);

        const declarations = [...read]
            .filter(name => !defined.has(name))
            .map(name => [name, root.getPropertyValue(name).trim()])
            .filter(([, value]) => value)
            .map(([name, value]) => `    ${name}: ${value};`);

        return declarations.length ? `@layer app {\n:root {\n${declarations.join("\n")}\n}\n}` : "";
    }
}

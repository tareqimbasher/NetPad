import {readFileSync, readdirSync} from "node:fs";
import {join} from "node:path";
import {icons, iconSvgMarkup} from "@application/ui/np-icon/icons";

function templates(dir: string): string[] {
    return readdirSync(dir, {withFileTypes: true}).flatMap(entry => {
        const path = join(dir, entry.name);
        if (entry.isDirectory()) return templates(path);
        return entry.name.endsWith(".html") ? [path] : [];
    });
}

/**
 * A glyph name that no longer exists renders nothing at all. Templates name glyphs as
 * plain strings that the compiler cannot check, so they are checked here instead.
 */
describe("np-icon glyph names in templates", () => {
    const named = templates(join(__dirname, "../../../../src"))
        .flatMap(path => {
            const html = readFileSync(path, "utf8");
            const literals = [...html.matchAll(/<np-icon[^>]*\sname="([^"${}]+)"/g)].map(m => m[1]);
            // Names picked by an inline ternary, e.g. name.bind="x ? 'folder-open' : 'folder'"
            const bound = [...html.matchAll(/<np-icon[^>]*\sname\.bind="([^"]+)"/g)]
                .flatMap(m => [...m[1].matchAll(/[?:]\s*'([^']+)'/g)].map(q => q[1]));
            return [...literals, ...bound].map(name => [path, name] as const);
        });

    test("every template names at least one glyph", () => {
        expect(named.length).toBeGreaterThan(0);
    });

    test.each(named)("%s renders '%s'", (_path, name) => {
        expect(Object.keys(icons)).toContain(name);
        expect(iconSvgMarkup(name)).not.toBe("");
    });
});

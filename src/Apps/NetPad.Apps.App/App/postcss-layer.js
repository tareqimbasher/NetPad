/**
 * A PostCSS plugin that wraps a stylesheet in a cascade layer: `@layer <name> { ... }`.
 *
 * `@charset` and `@import` stay at the top, where CSS requires them; an `@import` that names no
 * layer gets `layer(<name>)`.
 *
 * @param {{name: string, exclude?: RegExp[]}} options `exclude` matches the paths of stylesheets to
 * leave as they are.
 */
module.exports = ({name, exclude = []}) => ({
    postcssPlugin: "postcss-layer",
    Once(root, {AtRule}) {
        const file = root.source?.input.file ?? "";
        if (exclude.some(pattern => pattern.test(file))) return;

        const isPrelude = node => node.type === "atrule" && (node.name === "charset" || node.name === "import");

        for (const node of root.nodes.filter(isPrelude)) {
            if (node.name === "import" && !/\blayer\b/.test(node.params)) {
                // The grammar puts layer() right after the URL, ahead of any supports() or media condition.
                node.params = node.params.replace(
                    /^(\s*(?:url\([^)]*\)|"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'))/,
                    `$1 layer(${name})`);
            }
        }

        const body = root.nodes.filter(node => !isPrelude(node));
        if (body.length) {
            root.append(new AtRule({name: "layer", params: name}).append(body));
        }
    }
});

module.exports.postcss = true;

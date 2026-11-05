import postcss from "postcss";

// eslint-disable-next-line @typescript-eslint/no-var-requires
const postcssLayer = require("../postcss-layer");

const run = async (css: string, from = "/app/src/component.scss") =>
    (await postcss([postcssLayer({name: "app", exclude: [/excluded\.scss$/]})]).process(css, {from})).root;

const topLevel = (root: postcss.Root) =>
    root.nodes.map(n => n.type === "atrule" ? `@${n.name} ${n.params}` : n.type);

describe("postcss-layer", () => {
    test("wraps the stylesheet's rules in the layer", async () => {
        const root = await run(".a { color: red } @media (min-width: 1px) { .b { color: blue } }");

        expect(topLevel(root)).toEqual(["@layer app"]);
        expect((root.first as postcss.AtRule).nodes?.map(n => n.type)).toEqual(["rule", "atrule"]);
    });

    test("keeps @charset and @import ahead of the layer, and puts a layerless import in it", async () => {
        const root = await run(`@charset "UTF-8"; @import "x.css"; .a { color: red }`);

        expect(topLevel(root)).toEqual([`@charset "UTF-8"`, `@import "x.css" layer(app)`, "@layer app"]);
    });

    test("puts the layer right after the url, ahead of an import's conditions", async () => {
        const root = await run(`@import "x.css" screen; .a { color: red }`);

        expect(topLevel(root)).toEqual([`@import "x.css" layer(app) screen`, "@layer app"]);
    });

    test("leaves an import that already names a layer alone", async () => {
        const root = await run(`@import "x.css" layer(vendor); .a { color: red }`);

        expect(topLevel(root)).toEqual([`@import "x.css" layer(vendor)`, "@layer app"]);
    });

    test("leaves an excluded stylesheet unlayered", async () => {
        const root = await run(".a { color: red }", "/app/src/excluded.scss");

        expect(topLevel(root)).toEqual(["rule"]);
    });

    test("adds no empty layer to a stylesheet with no rules", async () => {
        const root = await run(`@import "x.css";`);

        expect(topLevel(root)).toEqual([`@import "x.css" layer(app)`]);
    });
});

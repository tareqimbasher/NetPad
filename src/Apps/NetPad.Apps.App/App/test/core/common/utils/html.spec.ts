import {html} from "@common/utils/html";

describe("html", () => {
    test.each([
        ["&", "&amp;"],
        ["<", "&lt;"],
        [">", "&gt;"],
        ["\"", "&quot;"],
        ["'", "&#39;"],
    ])("escapes '%s' in an interpolated value", (char, entity) => {
        expect(html`<b>${char}</b>`).toBe(`<b>${entity}</b>`);
    });

    test("leaves the template's own markup as written", () => {
        expect(html`<code class="key">x</code> & <i>y</i>`).toBe(`<code class="key">x</code> & <i>y</i>`);
    });

    test("escapes markup in a value so it renders as text", () => {
        expect(html`Delete <b>${"<b>x</b> & y"}</b>?`).toBe("Delete <b>&lt;b&gt;x&lt;/b&gt; &amp; y</b>?");
    });

    test("interpolates null and undefined as nothing", () => {
        expect(html`[${null}][${undefined}]`).toBe("[][]");
    });

    test("interpolates numbers and booleans as their text", () => {
        expect(html`${42} ${0} ${-1.5} ${false}`).toBe("42 0 -1.5 false");
    });

    test("escapes a nested html result like any other value", () => {
        expect(html`<p>${html`<b>${"x"}</b>`}</p>`).toBe("<p>&lt;b&gt;x&lt;/b&gt;</p>");
    });
});

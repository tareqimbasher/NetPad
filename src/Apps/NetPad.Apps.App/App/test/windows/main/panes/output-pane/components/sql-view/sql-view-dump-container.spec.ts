import {Settings} from "@application/api";
import {SqlViewDumpContainer} from "../../../../../../../src/windows/main/panes/output-pane/components/sql-view/sql-view-dump-container";

class TestSqlViewDumpContainer extends SqlViewDumpContainer {
    public prepare(html: string): DocumentFragment {
        const template = document.createElement("template");
        template.innerHTML = this.mutateHtmlBeforeAppend(html);
        this.beforeAppendHtml(template.content);
        return template.content;
    }
}

describe("SqlViewDumpContainer", () => {
    let container: TestSqlViewDumpContainer;

    beforeEach(() => container = new TestSqlViewDumpContainer(new Settings()));
    afterEach(() => container.dispose());

    test.each([
        ["a&lt;b and c&gt;d", "a<b and c>d"],
        ["AlbumId&lt;ArtistId", "AlbumId<ArtistId"],
        ["x &amp;amp; y", "x &amp; y"],
    ])("keeps logged text %s as the text %s", (escaped, text) => {
        const fragment = container.prepare(`<div class="group text">${escaped}</div>`);

        expect(fragment.querySelector(".group.text")?.textContent).toBe(text);
    });
});

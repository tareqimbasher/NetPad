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

    // Logged lines arrive with their spaces as `&nbsp;`.
    const entry = (category: string, message: string) =>
        `<div class="group text">${`dbug: [11/3/2026 03:07:07.561] (${category}) ${message}`.replaceAll(" ", "&nbsp;")}</div>`;
    const command = "Microsoft.EntityFrameworkCore.Database.Command";
    const connection = "Microsoft.EntityFrameworkCore.Database.Connection";

    const oneQuery = [
        entry(connection, "Opening connection to database 'main'."),
        entry(command, "Creating DbCommand for 'ExecuteReader'."),
        entry(command, "Executing DbCommand [Parameters=[], CommandType='Text'] SELECT 1"),
        entry(command, "Executed DbCommand (1ms) [Parameters=[], CommandType='Text'] SELECT 1"),
        entry(connection, "Closed connection to database 'main'."),
    ];

    test("counts one query per executed command, not per logged entry", () => {
        for (const html of [...oneQuery, ...oneQuery, ...oneQuery]) {
            container.prepare(html);
        }

        expect(container.defaultLogLength).toBe(3);
        expect(container.logLength).toBe(15);
    });

    test("counts the same when the whole log arrives at once", () => {
        container.prepare([...oneQuery, ...oneQuery].join(""));

        expect(container.defaultLogLength).toBe(2);
    });

    test("has no queries for a log without commands", () => {
        container.prepare(oneQuery[0] + oneQuery[4]);

        expect(container.defaultLogLength).toBe(0);
    });
});

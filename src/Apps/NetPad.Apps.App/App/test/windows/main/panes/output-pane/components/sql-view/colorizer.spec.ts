import {Colorizer} from "../../../../../../../src/windows/main/panes/output-pane/components/sql-view/colorizer";

// Logged entries arrive with their spaces as `&nbsp;`, their quotes as `&quot;` and `&apos;` and their
// line breaks as `<br/>`.
const entry = (...lines: string[]) => {
    const escaped = lines.map(line => line
        .replaceAll(" ", "&nbsp;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&apos;"));
    return `<div class="group text">${escaped.join("<br/>")}<br/></div>`;
};

const render = (html: string) => {
    const element = document.createElement("div");
    element.innerHTML = Colorizer.colorize(html);
    return element;
};

describe("Colorizer", () => {
    const category = "(Microsoft.EntityFrameworkCore.Infrastructure)";
    const initialized = (time: string) => entry(
        `dbug: ${time} CoreEventId.ContextInitialized[10403] ${category} `,
        "      Entity Framework Core initialized 'Program'");

    const headerParts = (element: Element) => Array.from(
        element.querySelectorAll(".log-level, .log-time, .log-event, .logger-name"),
        part => `${part.className}=${part.textContent!.replaceAll("\u00a0", " ")}`);

    test("splits the first line of an entry into its level, time, event and logger name", () => {
        const element = render(initialized("11/3/2026 17:21:03.047"));

        expect(headerParts(element)).toEqual([
            "log-level=dbug: ",
            "log-time=11/3/2026 17:21:03.047",
            "log-event= CoreEventId.ContextInitialized[10403] ",
            `logger-name=${category}`,
        ]);
    });

    test("finds the time of a header whose time Date.parse rejects", () => {
        const time = "3.11.2026 г. 17:21:03.047";
        expect(Date.parse(time)).toBeNaN();

        const element = render(initialized(time));

        expect(headerParts(element)).toEqual([
            "log-level=dbug: ",
            `log-time=${time}`,
            "log-event= CoreEventId.ContextInitialized[10403] ",
            `logger-name=${category}`,
        ]);
    });

    test.each([
        ["plain text", entry("Hello world")],
        ["a line without an event id", entry(`dbug: 11/3/2026 17:21:03.047 ${category} Opening connection`)],
        ["a category that is not the last token", entry(`info: 11/3/2026 17:21:03.047 CoreEventId.X[1] ${category} more`)],
        ["a header on a later line", entry("Hello", `dbug: 11/3/2026 17:21:03.047 CoreEventId.X[1] ${category}`)],
    ])("leaves %s untouched", (_, html) => {
        expect(Colorizer.colorize(html)).toBe(html);
    });

    test("puts a header on every entry when several arrive at once", () => {
        const element = render(initialized("11/3/2026 17:21:03.047") + initialized("11/3/2026 17:21:03.050"));

        expect(Array.from(element.querySelectorAll(".log-time"), time => time.textContent))
            .toEqual(["11/3/2026\u00a017:21:03.047", "11/3/2026\u00a017:21:03.050"]);
        expect(element.querySelectorAll(".logger-name")).toHaveLength(2);
    });

    test("leaves an entry it already colored as it is", () => {
        const once = Colorizer.colorize(initialized("11/3/2026 17:21:03.047"));

        expect(Colorizer.colorize(once)).toBe(once);
    });

    const executed = (parameters: string, ...sql: string[]) => entry(
        "info: 11/3/2026 17:21:03.047 RelationalEventId.CommandExecuted[20101] (Microsoft.EntityFrameworkCore.Database.Command) ",
        `      Executed DbCommand (1ms) [Parameters=[${parameters}], CommandType='Text', CommandTimeout='30']`,
        ...sql.map(line => `      ${line}`));

    const colored = (element: Element, selector: string) =>
        Array.from(element.querySelectorAll(selector), e => e.textContent!.replaceAll("\u00a0", " "));

    test("colors parameters and keywords", () => {
        const element = render(executed("@__p_0='2'", "SELECT a.AlbumId FROM albums AS a WHERE a.ArtistId = @__p_0"));

        expect(colored(element, ".query-params")).toEqual(["@__p_0='2'", "@__p_0"]);
        expect(colored(element, ".sql-keyword")).toEqual(["SELECT", "FROM", "AS", "WHERE"]);
        expect(element.querySelector(".sql-select")?.textContent).toBe("SELECT");
    });

    test("colors each command's own parameters when several arrive at once", () => {
        const element = render(
            executed("@__p_0='2'", "SELECT a.Id FROM albums AS a WHERE a.ArtistId = @__p_0")
            + executed("@p0='x', @p1='y'", "INSERT INTO tags (Name, Note) VALUES (@p0, @p1)", "-- @__p_0"));

        expect(colored(element, ".query-params")).toEqual([
            "@__p_0='2'", "@__p_0",
            "@p0='x', @p1='y'", "@p0", "@p1",
        ]);
    });

    test("colors a parameter's whole name and not the start of a longer one", () => {
        const element = render(executed("@p1='a', @p10='b'", "SELECT @p10, @p1"));

        expect(colored(element, ".query-params")).toEqual(["@p1='a', @p10='b'", "@p10", "@p1"]);
    });

    test.each([
        "INTEGER",
        "ASSET",
        "INDEXES",
    ])("leaves a keyword at the start of the word %s uncolored", word => {
        const element = render(executed("", `CREATE TABLE t (Id ${word})`));

        expect(colored(element, ".sql-keyword")).toEqual(["CREATE TABLE"]);
    });

    test.each([
        ["before a parenthesis", "WHERE a.Id IN (1, 2)", "IN"],
        ["before a quote", "SELECT 1 AS \"a\"", "AS"],
        ["before a space", "SELECT 1 FROM t", "FROM"],
    ])("colors a keyword %s", (_, sql, keyword) => {
        expect(colored(render(executed("", sql)), ".sql-keyword")).toContain(keyword);
    });

    test("colors a keyword at the end of a line", () => {
        expect(colored(render(entry(" ORDER BY", " DESC")), ".sql-keyword")).toEqual(["ORDER BY", "DESC"]);
    });

    test("colors a keyword at the end of an entry", () => {
        const html = `<div class="group text">&nbsp;GROUP&nbsp;BY</div>`;

        expect(colored(render(html), ".sql-keyword")).toEqual(["GROUP BY"]);
    });

    test("colors a keyword at the end of the text", () => {
        expect(colored(render("&nbsp;HAVING"), ".sql-keyword")).toEqual(["HAVING"]);
    });

    test("leaves a command without parameters uncolored", () => {
        const html = executed("", "SELECT 1");

        expect(render(html).querySelector(".query-params")).toBeNull();
    });

    test("leaves an entry it already colored as it is when it comes back as markup", () => {
        const element = render(
            executed("@__p_0='2'", "SELECT a.Id FROM albums AS a WHERE a.ArtistId = @__p_0")
            + executed("@p0='x'", "DELETE FROM tags WHERE Name = @p0"));
        const markup = element.innerHTML;

        expect(Colorizer.colorize(markup)).toBe(markup);
        expect(element.querySelector(".query-params .query-params")).toBeNull();
    });
});

import {Colorizer} from "../../../../../../../src/windows/main/panes/output-pane/components/sql-view/colorizer";

// Logged entries arrive with their spaces as `&nbsp;` and their line breaks as `<br/>`.
const entry = (...lines: string[]) =>
    `<div class="group text">${lines.map(line => line.replaceAll(" ", "&nbsp;")).join("<br/>")}<br/></div>`;

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

    test("colors parameters and keywords", () => {
        const element = render(entry(
            "info: 11/3/2026 17:21:03.047 RelationalEventId.CommandExecuted[20101] (Microsoft.EntityFrameworkCore.Database.Command) ",
            "      Executed DbCommand (1ms) [Parameters=[@__p_0='2'], CommandType='Text', CommandTimeout='30']",
            "      SELECT a.AlbumId FROM albums AS a WHERE a.ArtistId = @__p_0"));

        expect(Array.from(element.querySelectorAll(".query-params"), e => e.textContent))
            .toEqual(["@__p_0='2'", "@__p_0", "@__p_0"]);
        expect(Array.from(element.querySelectorAll(".sql-keyword"), e => e.textContent))
            .toEqual(["SELECT", "FROM", "AS", "WHERE"]);
        expect(element.querySelector(".sql-select")?.textContent).toBe("SELECT");
    });
});

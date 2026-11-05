import {ScriptOutput, Settings} from "@application/api";
import {DumpContainer} from "../../../../../../src/windows/main/panes/output-pane/components/dump-container";

describe("DumpContainer", () => {
    let container: DumpContainer;

    const show = (body: string, elapsedMs: number) => {
        container.appendOutput(new ScriptOutput({kind: "Result", order: 0, body, format: "Html", elapsedMs}));
        jest.runAllTimers();
    };

    beforeEach(() => {
        jest.useFakeTimers();
        container = new DumpContainer(new Settings());
    });

    afterEach(() => {
        container.dispose();
        jest.useRealTimers();
    });

    test("stamps a dump's timing on its type line, after the type", () => {
        show(`<div class="group"><table><thead><tr class="table-info-header"><th>Car[]</th></tr></thead><tbody></tbody></table></div>`, 12);

        const row = container.element.querySelector(".table-info-header > th > .dump-type-line")!;
        expect(Array.from(row.children, child => child.className)).toEqual(["dump-type", "dump-elapsed"]);
        expect(row.querySelector(".dump-elapsed")!.textContent).toBe("at 12 ms");
    });

    test("does not stamp output without a type line", () => {
        show(`<div class="group text">Hello</div>`, 12);

        expect(container.element.querySelector(".dump-elapsed")).toBeNull();
    });
});

import {ApiException} from "@application/api";
import {IlView} from "../../../../../../src/windows/main/panes/code-pane/il-view/il-view";

// The barrel loads components, which need their templates, and these tests do not render one.
jest.mock("@application", () => {
    const {DI} = jest.requireActual("aurelia");
    return {
        ...jest.requireActual("@application/error-message"),
        ...jest.requireActual("@application/view-model-base"),
        ICodeService: DI.createInterface("ICodeService"),
        IEventBus: DI.createInterface("IEventBus"),
        ISession: DI.createInterface("ISession"),
    };
});
jest.mock("../../../../../../src/windows/main/panes/code-pane/il-view/il-view.html", () => ({name: "il-view", template: ""}));

const FALLBACK = "Could not load IL code. Check logs for more info.";

const apiException = (body: string) =>
    new ApiException("An unexpected server error occurred.", 500, body, {}, null);

const settle = () => new Promise(resolve => setTimeout(resolve, 5));

describe("IlView", () => {
    const logger = {scopeTo: () => logger};

    const load = async (failure: unknown) => {
        const session = {active: {script: {id: "script-1", code: "Console.WriteLine(1);"}}};
        const codeService = {getIntermediateLanguage: jest.fn().mockRejectedValue(failure)};
        const view = new IlView(
            document.createElement("div"),
            session as never,
            codeService as never,
            {} as never,
            logger as never);

        view.pane = {isOpen: true} as never;
        view.isActive = true;
        view["current"] = "IL from an earlier load";

        view["loadIL"]();
        await settle();

        return view;
    };

    test.each([
        ["an empty body", ""],
        ["a body that isn't JSON", "<html>Bad gateway</html>"],
    ])("a failed request with %s shows the fallback in place of the code", async (_, body) => {
        const view = await load(apiException(body));

        expect(view["error"]).toBe(FALLBACK);
        expect(view["current"]).toBeNull();
    });

    test("a failed request shows the backend's message", async () => {
        const view = await load(apiException(JSON.stringify({message: "(1,1): error CS0103"})));

        expect(view["error"]).toBe("(1,1): error CS0103");
        expect(view["current"]).toBeNull();
    });

    test("an error that isn't a failed request shows the fallback", async () => {
        const view = await load(new TypeError("Failed to fetch"));

        expect(view["error"]).toBe(FALLBACK);
    });
});

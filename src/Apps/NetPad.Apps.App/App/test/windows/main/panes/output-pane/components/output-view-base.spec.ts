import {DI, ILogger, Registration} from "aurelia";
import type {OutputModel} from "../../../../../../src/windows/main/panes/output-pane/output-model";
import {OutputViewBase} from "../../../../../../src/windows/main/panes/output-pane/components/output-view-base";

jest.mock("@application", () => ({
    ViewModelBase: class {},
}));

jest.mock("../../../../../../src/windows/main/panes/output-pane/components/results-view/results-view", () => ({
    ResultsView: class {},
}));

class TestableOutputView extends OutputViewBase {
    public useWrapper(wrapper: Element) {
        this.dumpContainerWrapper = wrapper;
    }

    public changeModel(model?: OutputModel, previous?: OutputModel) {
        return this.modelChanged(model, previous);
    }
}

const fakeModel = () => {
    const sqlDumpContainer = {
        element: document.createElement("div"),
        attachedToDom: jest.fn(),
        detachingFromDom: jest.fn(),
    };
    return {model: {sqlDumpContainer} as unknown as OutputModel, sqlDumpContainer};
};

const settle = () => new Promise(resolve => setTimeout(resolve));

describe("OutputViewBase", () => {
    let view: TestableOutputView;
    let wrapper: Element;

    const show = async (model?: OutputModel, previous?: OutputModel) => {
        await view.changeModel(model, previous);
        await settle();
    };

    beforeEach(() => {
        const logger = {scopeTo: () => logger};
        const container = DI.createContainer().register(Registration.instance(ILogger, logger));
        view = container.invoke(TestableOutputView);
        wrapper = document.createElement("div");
        view.useWrapper(wrapper);
    });

    test("attaching with no model leaves the view empty", async () => {
        view.attached();
        await settle();

        expect(view.dumpContainer).toBeUndefined();
        expect(wrapper.childElementCount).toBe(0);
    });

    test("when the model goes away, detaches its container and empties the view", async () => {
        const {model, sqlDumpContainer} = fakeModel();
        await show(model);

        await show(undefined, model);

        expect(sqlDumpContainer.detachingFromDom).toHaveBeenCalledTimes(1);
        expect(view.dumpContainer).toBeUndefined();
        expect(wrapper.childElementCount).toBe(0);
    });

    test("a new model attaches after the previous one went away", async () => {
        const first = fakeModel();
        const second = fakeModel();
        await show(first.model);
        await show(undefined, first.model);

        await show(second.model);

        expect(view.dumpContainer).toBe(second.sqlDumpContainer);
        expect(wrapper.firstElementChild).toBe(second.sqlDumpContainer.element);
        expect(second.sqlDumpContainer.attachedToDom).toHaveBeenCalledTimes(1);
    });
});

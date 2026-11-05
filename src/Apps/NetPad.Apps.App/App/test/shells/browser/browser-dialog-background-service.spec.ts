import {DialogCloseResult} from "@aurelia/dialog";
import {ConfirmOpenAsDuplicateCommand, ConfirmSaveCommand, RequestScriptSavePathCommand} from "@application/api";
import {BrowserDialogBackgroundService} from "../../../src/shells/browser/services/browser-dialog-background-service";
import {FakeDialogUtil, FakeEventBus, FakeIpcGateway, FakeLogger} from "../../stubs/dialog-fakes";

// The barrel and the dialog modules load components, which need their templates.
jest.mock("@application", () => ({
    ...jest.requireActual("@application/api"),
    ...jest.requireActual("@application/events/channel-info"),
    ...jest.requireActual("@application/events/ievent-bus"),
    ...jest.requireActual("@application/events/iipc-gateway"),
}));
jest.mock("@application/dialogs/dialog-util", () => ({DialogUtil: class {}}));

describe("BrowserDialogBackgroundService", () => {
    let eventBus: FakeEventBus;
    let ipcGateway: FakeIpcGateway;
    let dialogUtil: FakeDialogUtil;
    let logger: FakeLogger;

    beforeEach(async () => {
        eventBus = new FakeEventBus();
        ipcGateway = new FakeIpcGateway();
        dialogUtil = new FakeDialogUtil();
        logger = new FakeLogger();

        const service = new BrowserDialogBackgroundService(
            eventBus as never, ipcGateway as never, dialogUtil as never, logger as never);
        await service.start();
    });

    const requestSavePath = async (result: DialogCloseResult) => {
        dialogUtil.nextResult = result;
        await eventBus.fire(RequestScriptSavePathCommand, {requestId: "r1", scriptName: "Script 1"});
        return ipcGateway.responseTo("r1");
    };

    test("asks for the name in a Save Script prompt, filled in with the current name", async () => {
        await requestSavePath({status: "ok", value: "Script 1"} as DialogCloseResult);

        expect(dialogUtil.prompted).toEqual({title: "Save Script", message: "Script name", defaultValue: "Script 1"});
    });

    test("OK answers the entered name", async () => {
        expect(await requestSavePath({status: "ok", value: "Report"} as DialogCloseResult)).toBe("Report");
    });

    test("Cancel answers null", async () => {
        expect(await requestSavePath({status: "cancel"} as DialogCloseResult)).toBeNull();
    });

    describe("when the dialog can't open, answers as Cancel would, with a warning", () => {
        beforeEach(() => dialogUtil.cannotOpen = true);

        test("the save name: null", async () => {
            expect(await requestSavePath({status: "ok", value: "Report"} as DialogCloseResult)).toBeNull();
            expect(logger.warnings).toHaveLength(1);
        });

        test("save changes: Cancel", async () => {
            await eventBus.fire(ConfirmSaveCommand, {requestId: "r2", scriptName: "Script 1"});

            expect(ipcGateway.responseTo("r2")).toBe("Cancel");
            expect(logger.warnings).toHaveLength(1);
        });

        test("open as duplicate: no, go to the existing tab", async () => {
            await eventBus.fire(ConfirmOpenAsDuplicateCommand, {requestId: "r3", message: "Already open"});

            expect(ipcGateway.responseTo("r3")).toBe(false);
            expect(logger.warnings).toHaveLength(1);
        });
    });
});

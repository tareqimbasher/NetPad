import {DialogCloseResult} from "@aurelia/dialog";
import {ConfirmWithUserCommand, PromptUserCommand} from "@application/api";
import {DialogBackgroundService} from "@application/background-services/dialog-background-service";
import {FakeDialogUtil, FakeEventBus, FakeIpcGateway, FakeLogger} from "../../../stubs/dialog-fakes";

// The barrel and the dialog modules load components, which need their templates.
jest.mock("@application", () => ({
    ...jest.requireActual("@application/api"),
    ...jest.requireActual("@application/events/channel-info"),
    ...jest.requireActual("@application/events/ievent-bus"),
    ...jest.requireActual("@application/events/iipc-gateway"),
}));
jest.mock("@application/dialogs/dialog-util", () => ({DialogUtil: class {}}));
jest.mock("@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog", () => ({AppDependenciesCheckDialog: class {}}));

describe("DialogBackgroundService", () => {
    let eventBus: FakeEventBus;
    let ipcGateway: FakeIpcGateway;
    let dialogUtil: FakeDialogUtil;
    let logger: FakeLogger;

    beforeEach(async () => {
        eventBus = new FakeEventBus();
        ipcGateway = new FakeIpcGateway();
        dialogUtil = new FakeDialogUtil();
        logger = new FakeLogger();

        const service = new DialogBackgroundService(
            eventBus as never, ipcGateway as never, dialogUtil as never, logger as never);
        await service.start();
    });

    const confirm = async (result: DialogCloseResult) => {
        dialogUtil.nextResult = result;
        await eventBus.fire(ConfirmWithUserCommand, {requestId: "r1", message: "Overwrite it?"});
        return ipcGateway.responseTo("r1");
    };

    const prompt = async (result: DialogCloseResult) => {
        dialogUtil.nextResult = result;
        await eventBus.fire(PromptUserCommand, {requestId: "r2", message: "Name", prefillValue: "Script 1"});
        return ipcGateway.responseTo("r2");
    };

    describe("confirm", () => {
        test("asks with the command's message, Cancel and a primary OK", async () => {
            await confirm({status: "ok", value: "Yes"} as DialogCloseResult);

            expect(dialogUtil.asked?.message).toBe("Overwrite it?");
            expect(dialogUtil.asked?.buttons?.map(b => [b.text, !!b.isPrimary])).toEqual([["Cancel", false], ["OK", true]]);
        });

        test("OK answers Yes", async () => {
            expect(await confirm({status: "ok", value: "Yes"} as DialogCloseResult)).toBe("Yes");
        });

        test("Cancel answers No", async () => {
            expect(await confirm({status: "ok", value: "Cancel"} as DialogCloseResult)).toBe("No");
        });

        test("Escape or closing answers No", async () => {
            expect(await confirm({status: "cancel"} as DialogCloseResult)).toBe("No");
        });

        test("answers No, with a warning, when the dialog can't open", async () => {
            dialogUtil.cannotOpen = true;

            expect(await confirm({status: "ok", value: "Yes"} as DialogCloseResult)).toBe("No");
            expect(logger.warnings).toHaveLength(1);
        });
    });

    describe("prompt", () => {
        test("labels the field with the message and fills in the prefill", async () => {
            await prompt({status: "ok", value: "x"} as DialogCloseResult);

            expect(dialogUtil.prompted?.message).toBe("Name");
            expect(dialogUtil.prompted?.defaultValue).toBe("Script 1");
        });

        test("OK answers the entered value", async () => {
            expect(await prompt({status: "ok", value: "Renamed"} as DialogCloseResult)).toBe("Renamed");
        });

        test("OK with an empty value answers null", async () => {
            expect(await prompt({status: "ok", value: ""} as DialogCloseResult)).toBeNull();
        });

        test("Cancel answers null", async () => {
            expect(await prompt({status: "cancel"} as DialogCloseResult)).toBeNull();
        });

        test("answers null, with a warning, when the dialog can't open", async () => {
            dialogUtil.cannotOpen = true;

            expect(await prompt({status: "ok", value: "Renamed"} as DialogCloseResult)).toBeNull();
            expect(logger.warnings).toHaveLength(1);
        });
    });
});

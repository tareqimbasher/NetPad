import {ILogger} from "aurelia";
import {DialogOpenResult} from "@aurelia/dialog";
import {CommandIds} from "@application/commands/command-ids";
import {ICommandRegistry} from "@application/commands/icommand-registry";
import {IEventBus} from "@application/events/ievent-bus";
import {IKeybindingManager} from "@application/keybindings/ikeybinding-manager";
import {ISession} from "@application/sessions/isession";
import {RecentScriptsStore} from "@application/sessions/recent-scripts-store";
import {MainMenuService} from "@application/main-menu/main-menu-service";
import {isBlockedByOpenDialog} from "@application/dialogs/dialog-command-rule";
import {OpenDialogs} from "@application/dialogs/open-dialogs";

// The barrel loads components, which need their templates.
jest.mock("@application", () => ({
    ...jest.requireActual("@application/api"),
    ...jest.requireActual("@application/commands/command-ids"),
    ...jest.requireActual("@application/commands/icommand-registry"),
    ...jest.requireActual("@application/events/ievent-bus"),
    ...jest.requireActual("@application/keybindings/ikeybinding-manager"),
    ...jest.requireActual("@application/sessions/isession"),
    ...jest.requireActual("@application/sessions/recent-scripts-store"),
}));

const logger = {
    scopeTo: () => logger,
    debug: () => undefined,
    error: () => undefined,
} as unknown as ILogger;

afterEach(() => OpenDialogs.clear());

const openDialog = () => OpenDialogs.set("PromptDialog", {} as DialogOpenResult);

describe("isBlockedByOpenDialog", () => {
    test("blocks nothing while no dialog is open", () => {
        expect(isBlockedByOpenDialog(CommandIds.runScript)).toBe(false);
        expect(isBlockedByOpenDialog(undefined)).toBe(false);
    });

    test("blocks commands and menu actions while a dialog is open", () => {
        openDialog();

        expect(isBlockedByOpenDialog(CommandIds.runScript)).toBe(true);
        expect(isBlockedByOpenDialog(undefined)).toBe(true);
    });

    test("lets Toggle Developer Tools run while a dialog is open", () => {
        openDialog();

        expect(isBlockedByOpenDialog(CommandIds.toggleDeveloperTools)).toBe(false);
    });

    test("blocks nothing once the dialog closes", () => {
        openDialog();
        OpenDialogs.delete("PromptDialog");

        expect(isBlockedByOpenDialog(CommandIds.runScript)).toBe(false);
    });
});

describe("MainMenuService.clickMenuItem", () => {
    const execute = jest.fn();
    const click = jest.fn();

    const service = new MainMenuService(
        {get: (id: string) => ({id, title: id}), isEnabled: () => true, execute} as unknown as ICommandRegistry,
        {getKeybinding: () => undefined, onChanged: () => ({dispose: () => undefined})} as unknown as IKeybindingManager,
        {} as ISession,
        {subscribeToServer: () => ({dispose: () => undefined})} as unknown as IEventBus,
        {
            initialize: () => Promise.resolve(),
            onChanged: () => ({dispose: () => undefined}),
            recentScripts: [],
        } as unknown as RecentScriptsStore,
        logger);

    afterEach(() => {
        execute.mockClear();
        click.mockClear();
    });

    test("runs an item while no dialog is open", async () => {
        await service.clickMenuItem({commandId: CommandIds.runScript});
        await service.clickMenuItem({click});

        expect(execute).toHaveBeenCalledWith(CommandIds.runScript);
        expect(click).toHaveBeenCalled();
    });

    test("runs nothing but Toggle Developer Tools while a dialog is open", async () => {
        openDialog();

        await service.clickMenuItem({commandId: CommandIds.runScript});
        await service.clickMenuItem({click});
        await service.clickMenuItem({commandId: CommandIds.toggleDeveloperTools});

        expect(click).not.toHaveBeenCalled();
        expect(execute.mock.calls).toEqual([[CommandIds.toggleDeveloperTools]]);
    });
});

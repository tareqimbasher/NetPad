import {ILogger} from "aurelia";
import {DialogOpenResult} from "@aurelia/dialog";
import {currentOs} from "@common";
import {KeybindingManager} from "@application/keybindings/keybinding-manager";
import {CommandIds} from "@application/commands/command-ids";
import {ICommandRegistry} from "@application/commands/icommand-registry";
import {IEventBus} from "@application/events/ievent-bus";
import {Settings} from "@application/api";
import {OpenDialogs} from "@application/dialogs/open-dialogs";

const logger = {
    scopeTo: () => logger,
    debug: () => undefined,
    error: () => undefined,
} as unknown as ILogger;

// The manager listens on the document, so the file shares one.
const execute = jest.fn();

new KeybindingManager(
    {
        keyboardShortcuts: {
            shortcuts: [
                {id: CommandIds.toggleDeveloperTools, key: "F12"},
                {id: CommandIds.openSettings, primary: true, key: "A"},
            ]
        }
    } as unknown as Settings,
    {subscribeToServer: () => ({dispose: () => undefined})} as unknown as IEventBus,
    {get: (id: string) => ({id}), execute} as unknown as ICommandRegistry,
    logger
).initialize();

const press = (target: Element, key: string, primary = false) => {
    const event = new KeyboardEvent("keydown", {
        key,
        ctrlKey: primary && currentOs !== "macos",
        metaKey: primary && currentOs === "macos",
        bubbles: true,
        cancelable: true,
    });
    target.dispatchEvent(event);
    return event;
};

const field = document.body.appendChild(document.createElement("input"));
const button = document.body.appendChild(document.createElement("button"));

afterEach(() => {
    execute.mockClear();
    OpenDialogs.clear();
});

describe("KeybindingManager with a dialog open", () => {
    beforeEach(() => OpenDialogs.set("PromptDialog", {} as DialogOpenResult));

    test.each([
        ["F5", "F5", false],
        ["Ctrl+S", "S", true],
    ])("prevents %s and runs nothing", (_, key, primary) => {
        const event = press(document.body, key, primary);

        expect(event.defaultPrevented).toBe(true);
        expect(execute).not.toHaveBeenCalled();
    });

    test("runs Toggle Developer Tools", () => {
        const event = press(document.body, "F12");

        expect(event.defaultPrevented).toBe(true);
        expect(execute).toHaveBeenCalledWith(CommandIds.toggleDeveloperTools);
    });

    test("leaves a bound editing key to the field that has focus", () => {
        const event = press(field, "a", true);

        expect(event.defaultPrevented).toBe(false);
        expect(execute).not.toHaveBeenCalled();
    });

    test("prevents a bound editing key outside a field", () => {
        const event = press(button, "a", true);

        expect(event.defaultPrevented).toBe(true);
        expect(execute).not.toHaveBeenCalled();
    });
});

describe("KeybindingManager with no dialog open", () => {
    test.each([
        ["F5", document.body, "F5", false, CommandIds.runScript],
        ["a bound editing key in a field", field, "a", true, CommandIds.openSettings],
    ])("runs the command bound to %s", (_, target, key, primary, commandId) => {
        const event = press(target, key, primary);

        expect(event.defaultPrevented).toBe(true);
        expect(execute).toHaveBeenCalledWith(commandId);
    });
});

import {ILogger} from "aurelia";
import {KeybindingManager} from "@application/keybindings/keybinding-manager";
import {CommandIds} from "@application/commands/command-ids";
import {ICommandRegistry} from "@application/commands/icommand-registry";
import {IEventBus} from "@application/events/ievent-bus";
import {Settings} from "@application/api";

const logger = {
    scopeTo: () => logger,
    debug: () => undefined,
    error: () => undefined,
} as unknown as ILogger;

describe("KeybindingManager.describe", () => {
    test("a given title replaces the command's name", () => {
        const manager = new KeybindingManager(
            {keyboardShortcuts: {shortcuts: []}} as unknown as Settings,
            {subscribeToServer: () => ({dispose: () => undefined})} as unknown as IEventBus,
            {get: () => ({title: "Save"})} as unknown as ICommandRegistry,
            logger);
        manager.initialize();

        const keys = manager.keysFor(CommandIds.saveScript);

        expect(keys).toBeDefined();
        expect(manager.describe(CommandIds.saveScript, "Keep")).toBe(`Keep (${keys})`);
    });
});

import {IContainer, ILogger} from "aurelia";
import {AppCommand} from "@application/commands/command";
import {CommandIds} from "@application/commands/command-ids";
import {CommandRegistry} from "@application/commands/command-registry";
import {createBuiltinCommands} from "@application/commands/builtin-commands";
import {ShellType} from "@application/windowing/shell-type";
import {WindowParams} from "@application/windowing/window-params";

const logger = {
    scopeTo: () => logger,
    debug: () => undefined,
    info: () => undefined,
    warn: () => undefined,
    error: () => undefined,
} as unknown as ILogger;

function createRegistry(container: Partial<IContainer> = {}) {
    return new CommandRegistry({get: () => undefined, ...container} as unknown as IContainer, logger);
}

beforeAll(() => WindowParams.init(new URLSearchParams()));

describe("the built-in commands", () => {
    test("every id is unique", () => {
        const ids = createBuiltinCommands().map(c => c.id);

        expect(new Set(ids).size).toBe(ids.length);
    });

    test("editor wrappers carry the action they wrap and take no keybinding", () => {
        const wrappers = createBuiltinCommands().filter(c => c.category === "Edit");

        expect(wrappers.length).toBeGreaterThan(0);
        for (const wrapper of wrappers) {
            expect(wrapper.monacoCommandId).toBeTruthy();
            expect(wrapper.keybindable).toBe(false);
        }
    });

    test("no editor action is wrapped twice, so a federated listing can dedupe by identity", () => {
        const wrapped = createBuiltinCommands().map(c => c.monacoCommandId).filter(Boolean);

        expect(new Set(wrapped).size).toBe(wrapped.length);
    });
});

describe("CommandRegistry", () => {
    test("shell-gated commands are left out of the shells they do not belong to", () => {
        const registry = createRegistry();

        expect(WindowParams.shell).toBe(ShellType.Browser);
        expect(registry.get(CommandIds.saveScriptAs)).toBeUndefined();
        expect(registry.get(CommandIds.exit)).toBeUndefined();
        expect(registry.get(CommandIds.saveScript)).toBeDefined();
    });

    test("executes a registered command", async () => {
        const registry = createRegistry();
        const execute = jest.fn();

        registry.register(new AppCommand({id: "test.run", title: "Run", category: "Tools", execute}));
        await registry.execute("test.run");

        expect(execute).toHaveBeenCalledTimes(1);
    });

    test("hands the command the argument the caller supplied", async () => {
        const registry = createRegistry();
        let received: unknown;

        registry.register(new AppCommand({
            id: "test.arg",
            title: "Arg",
            category: "Tools",
            execute: ctx => received = ctx.argAs<string>(),
        }));
        await registry.execute("test.arg", "script-1");

        expect(received).toBe("script-1");
    });

    test("does not execute a disabled command", async () => {
        const registry = createRegistry();
        const execute = jest.fn();

        registry.register(new AppCommand({
            id: "test.disabled",
            title: "Disabled",
            category: "Tools",
            isEnabled: () => false,
            execute,
        }));

        expect(registry.isEnabled("test.disabled")).toBe(false);

        await registry.execute("test.disabled");

        expect(execute).not.toHaveBeenCalled();
    });

    test("a command without an enablement rule is always available", () => {
        const registry = createRegistry();

        registry.register(new AppCommand({id: "test.always", title: "Always", category: "Tools", execute: () => undefined}));

        expect(registry.isEnabled("test.always")).toBe(true);
    });

    test("executing an unknown command is a no-op", async () => {
        await expect(createRegistry().execute("test.nope")).resolves.toBeUndefined();
    });

    test("a command that throws does not take the caller down with it", async () => {
        const registry = createRegistry();

        registry.register(new AppCommand({
            id: "test.throws",
            title: "Throws",
            category: "Tools",
            execute: () => {
                throw new Error("boom");
            },
        }));

        await expect(registry.execute("test.throws")).resolves.toBeUndefined();
    });
});

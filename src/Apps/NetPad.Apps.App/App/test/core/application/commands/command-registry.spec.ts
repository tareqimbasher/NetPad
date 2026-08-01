import {IContainer, ILogger} from "aurelia";
import {AppCommand} from "@application/commands/command";
import {CommandIds} from "@application/commands/command-ids";
import {CommandRegistry} from "@application/commands/command-registry";
import {createBuiltinCommands} from "@application/commands/builtin-commands";
import {ISettingsService} from "@application/configuration/isettings-service";
import {ShellType} from "@application/windowing/shell-type";
import {IWindowDestinations} from "@application/windowing/iwindow-destinations";
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

function inWindow(window: string, assert: (registry: CommandRegistry) => void) {
    WindowParams.init(new URLSearchParams(`win=${window}`));
    try {
        assert(createRegistry());
    } finally {
        WindowParams.init(new URLSearchParams());
    }
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

    test("window-gated commands are left out of the windows they do not belong to", () => {
        inWindow("settings", registry => {
            expect(registry.get(CommandIds.runScript)).toBeUndefined();
            expect(registry.get(CommandIds.undo)).toBeUndefined();
            expect(registry.get(CommandIds.openSettings)).toBeDefined();
            expect(registry.get(CommandIds.openCommandPalette)).toBeDefined();
        });
    });

    test("settings pages are commands everywhere, script-properties tabs only where they apply", () => {
        inWindow("settings", registry => {
            expect(registry.get(CommandIds.settingsShortcuts)).toBeDefined();
            expect(registry.get(CommandIds.openScriptPackages)).toBeUndefined();
        });

        inWindow("script-config", registry => {
            expect(registry.get(CommandIds.openScriptReferences)).toBeDefined();
            expect(registry.get(CommandIds.openScriptPackages)).toBeDefined();
            expect(registry.get(CommandIds.openScriptNamespaces)).toBeDefined();
            expect(registry.get(CommandIds.settingsGeneral)).toBeDefined();
        });
    });

    test("a command another window owns is still listed for keybinding", () => {
        inWindow("settings", registry => {
            const ids = registry.allCommands.map(c => c.id);

            expect(ids).toContain(CommandIds.runScript);
            expect(ids).toContain(CommandIds.openScriptPackages);
            expect(registry.commands.map(c => c.id)).not.toContain(CommandIds.runScript);
        });
    });

    test("a command this shell does not have is listed nowhere", () => {
        const registry = createRegistry();

        expect(registry.allCommands.map(c => c.id)).not.toContain(CommandIds.exit);
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

    test("reaching the settings window moves it when it is the window asking, and opens it otherwise", async () => {
        const cases = [
            {window: "win=settings", command: CommandIds.about, expected: ["goTo about"]},
            {window: "", command: CommandIds.about, expected: ["open about"]},
            {window: "win=settings", command: CommandIds.settingsShortcuts, expected: ["goTo keyboard-shortcuts"]},
            {window: "win=settings", command: CommandIds.openSettings, expected: []},
            {window: "", command: CommandIds.openSettings, expected: ["open the window"]},
        ];

        for (const {window, command, expected} of cases) {
            WindowParams.init(new URLSearchParams(window));
            const calls: string[] = [];

            try {
                const registry = createRegistry({
                    get: (key: unknown) => {
                        if (key === IWindowDestinations) return {goTo: (route: string) => calls.push(`goTo ${route}`)};
                        if (key === ISettingsService) {
                            return {openSettingsWindow: (tab: string | null) => calls.push(`open ${tab ?? "the window"}`)};
                        }
                        return undefined;
                    },
                } as unknown as Partial<IContainer>);

                await registry.execute(command);
            } finally {
                WindowParams.init(new URLSearchParams());
            }

            expect(calls).toEqual(expected);
        }
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

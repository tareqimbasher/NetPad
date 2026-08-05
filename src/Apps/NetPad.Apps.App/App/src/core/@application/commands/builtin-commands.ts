import {CreateScriptDto} from "@application/api";
import {IScriptService} from "@application/scripts/iscript-service";
import {ISettingsService} from "@application/configuration/isettings-service";
import {IWindowService} from "@application/windowing/iwindow-service";
import {IPaneManager} from "@application/panes/ipane-manager";
import {ISystemService} from "@application/system/isystem-service";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {ITextEditorService} from "@application/editor/itext-editor-service";
import {ICommandPalette} from "@application/command-palette/icommand-palette";
import {PaneIds} from "@application/panes/pane-ids";
import {TogglePaneCommand} from "@application/panes/toggle-pane-command";
import {RunScriptCommand} from "@application/scripts/run-script-command";
import {StopScriptCommand} from "@application/scripts/stop-script-command";
import {CloseTabsCommand} from "@application/scripts/close-tabs-command";
import {Settings} from "@application/api";
import {IWindowDestinations} from "@application/windowing/iwindow-destinations";
import {ShellType} from "@application/windowing/shell-type";
import {WindowId} from "@application/windowing/window-id";
import {WindowParams} from "@application/windowing/window-params";
import {AppCommand, CommandContext, CommandDefinition} from "./command";
import {CommandIds} from "./command-ids";

const desktopShells = [ShellType.Electron, ShellType.Tauri];

/** Scripts, editors, tabs and panes live in the main window, and so do the commands that act on them. */
const mainWindow = [WindowId.Main];

/** The script a command acts on: the one a caller named, otherwise the active one. */
function targetScriptId(context: CommandContext): string | undefined {
    return context.argAs<string>() ?? context.session.active?.script.id;
}

/**
 * Resolves the dialog helper without pulling every dialog into the module graph if the caller.
 */
async function dialogUtil(context: CommandContext) {
    const {DialogUtil} = await import("@application/dialogs/dialog-util");
    return context.container.get(DialogUtil);
}

/**
 * Wraps an editor action. The editor owns both the executed action and the key that reaches it. NetPad only
 * offers a second door to it.
 */
function editorAction(
    id: CommandIds,
    title: string,
    monacoCommandId: string,
    icon?: CommandDefinition["icon"]
): AppCommand {
    return new AppCommand({
        id,
        title,
        category: "Edit",
        icon,
        monacoCommandId,
        windows: mainWindow,
        keybindable: false,
        execute: (ctx) => ctx.container.get(ITextEditorService).active?.monaco
            .trigger(null, monacoCommandId, null),
    });
}

/** Reaching the settings window, optionally at one page. */
function settingsDestination(route: string | null): CommandDefinition["execute"] {
    return (ctx) => {
        if (WindowParams.window !== WindowId.Settings) {
            return ctx.container.get(ISettingsService).openSettingsWindow(route);
        }

        if (route) {
            ctx.container.get(IWindowDestinations).goTo(route);
        }

        return undefined;
    };
}

/** A settings page as a command. */
function settingsPage(id: CommandIds, page: string, route: string, icon: CommandDefinition["icon"]): AppCommand {
    return new AppCommand({
        id,
        title: `Settings: ${page}`,
        category: "Settings",
        icon,
        execute: settingsDestination(route),
    });
}

/** A script properties page as a command. */
function scriptConfigPage(id: CommandIds, tab: string, route: string, icon: CommandDefinition["icon"]): AppCommand {
    return new AppCommand({
        id,
        title: `Script Properties: ${tab}`,
        category: "File",
        icon,
        windows: [WindowId.Main, WindowId.ScriptConfig],
        execute: (ctx) => {
            if (WindowParams.window === WindowId.ScriptConfig) {
                return ctx.container.get(IWindowDestinations).goTo(route);
            }

            const scriptId = targetScriptId(ctx);
            return scriptId ? ctx.container.get(IScriptService).openConfigWindow(scriptId, route) : undefined;
        },
    });
}

/**
 * The commands NetPad ships with. This order is what the keyboard-shortcut settings page lists, so
 * it is user-facing: keep related commands adjacent rather than sorting the array.
 */
export function createBuiltinCommands(): AppCommand[] {
    return [
        // File
        new AppCommand({
            id: CommandIds.newScript,
            title: "New",
            category: "File",
            icon: "add",
            windows: mainWindow,
            execute: (ctx) => ctx.container.get(IScriptService).create(new CreateScriptDto()),
        }),

        new AppCommand({
            id: CommandIds.openFile,
            title: "Open File",
            category: "File",
            shells: desktopShells,
            windows: mainWindow,
            execute: async (ctx) => {
                const paths = await ctx.container.get(INativeDialogService).showFileSelectorDialog({
                    title: "Open Script",
                    filters: [{name: "NetPad Script", extensions: ["netpad"]}],
                    multiple: true,
                });

                if (!paths || paths.length === 0) return;

                for (const path of paths) {
                    try {
                        await ctx.session.openByPath(path);
                    } catch (err) {
                        console.error("Failed to open file:", path, err);
                    }
                }
            },
        }),

        new AppCommand({
            id: CommandIds.goToScript,
            title: "Go to Script",
            category: "File",
            windows: mainWindow,
            execute: (ctx) => ctx.container.get(ICommandPalette).open(),
        }),

        new AppCommand({
            id: CommandIds.saveScript,
            title: "Save",
            category: "File",
            icon: "save",
            windows: mainWindow,
            execute: (ctx) => {
                const scriptId = targetScriptId(ctx);
                return scriptId ? ctx.container.get(IScriptService).save(scriptId) : undefined;
            },
        }),

        new AppCommand({
            id: CommandIds.saveScriptAs,
            title: "Save As",
            category: "File",
            icon: "save",
            shells: desktopShells,
            windows: mainWindow,
            execute: (ctx) => {
                const scriptId = targetScriptId(ctx);
                return scriptId ? ctx.container.get(IScriptService).saveAs(scriptId) : undefined;
            },
        }),

        new AppCommand({
            id: CommandIds.saveAllScripts,
            title: "Save All",
            category: "File",
            icon: "save",
            windows: mainWindow,
            execute: async (ctx) => {
                const scriptService = ctx.container.get(IScriptService);
                for (const environment of ctx.session.environments.filter(e => e.script.isDirty)) {
                    await scriptService.save(environment.script.id);
                }
            },
        }),

        new AppCommand({
            id: CommandIds.openScriptProperties,
            title: "Script Properties",
            category: "File",
            icon: "properties",
            windows: mainWindow,
            execute: (ctx) => {
                const scriptId = targetScriptId(ctx);
                return scriptId ? ctx.container.get(IScriptService).openConfigWindow(scriptId, null) : undefined;
            },
        }),

        scriptConfigPage(CommandIds.openScriptReferences, "References", "references", "references"),
        scriptConfigPage(CommandIds.openScriptPackages, "Packages", "packages", "package"),
        scriptConfigPage(CommandIds.openScriptNamespaces, "Namespaces", "namespaces", "namespaces"),

        new AppCommand({
            id: CommandIds.closeScript,
            title: "Close",
            category: "File",
            icon: "close",
            windows: mainWindow,
            execute: (ctx) => {
                const scriptId = targetScriptId(ctx);
                return scriptId ? ctx.session.close(scriptId, false) : undefined;
            },
        }),

        new AppCommand({
            id: CommandIds.switchToLastActiveScript,
            title: "Switch to Last Active Script",
            category: "File",
            windows: mainWindow,
            execute: (ctx) => ctx.session.activateLastActive(),
        }),

        new AppCommand({
            id: CommandIds.openSettings,
            title: "Settings",
            category: "File",
            icon: "settings",
            execute: settingsDestination(null),
        }),

        new AppCommand({
            id: CommandIds.exit,
            title: "Exit",
            category: "File",
            shells: desktopShells,
            execute: (ctx) => ctx.container.get(IWindowService).close(),
        }),

        // Settings
        settingsPage(CommandIds.settingsGeneral, "General", "general", "settings"),
        settingsPage(CommandIds.settingsEditor, "Editor", "editor", "code"),
        settingsPage(CommandIds.settingsResults, "Results", "results", "results"),
        settingsPage(CommandIds.settingsCustomCss, "Custom CSS", "style", "custom-css"),
        settingsPage(CommandIds.settingsShortcuts, "Shortcuts", "keyboard-shortcuts", "keyboard"),
        settingsPage(CommandIds.settingsOmniSharp, "OmniSharp", "omnisharp", "code-intelligence"),

        // Edit
        editorAction(CommandIds.undo, "Undo", "undo", "undo"),
        editorAction(CommandIds.redo, "Redo", "redo", "redo"),
        editorAction(CommandIds.selectAll, "Select All", "editor.action.selectAll"),
        editorAction(CommandIds.find, "Find", "actions.find", "search"),
        editorAction(CommandIds.replace, "Replace", "editor.action.startFindReplaceAction"),
        editorAction(CommandIds.transformToUpperOrLowerCase, "Transform to Upper/Lower Case",
            "netpad.action.transformToUpperOrLowercase"),
        editorAction(CommandIds.transformToUpperCase, "Transform to Upper Case",
            "editor.action.transformToUppercase"),
        editorAction(CommandIds.transformToLowerCase, "Transform to Lower Case",
            "editor.action.transformToLowercase"),
        editorAction(CommandIds.transformToTitleCase, "Transform to Title Case",
            "editor.action.transformToTitlecase"),
        editorAction(CommandIds.transformToCamelCase, "Transform to Camel Case",
            "editor.action.transformToCamelcase"),
        editorAction(CommandIds.transformToKebabCase, "Transform to Kebab Case",
            "editor.action.transformToKebabcase"),
        editorAction(CommandIds.transformToSnakeCase, "Transform to Snake Case",
            "editor.action.transformToSnakecase"),
        editorAction(CommandIds.toggleLineComment, "Toggle Line Comment", "editor.action.commentLine"),
        editorAction(CommandIds.toggleBlockComment, "Toggle Block Comment", "editor.action.blockComment"),

        // View
        new AppCommand({
            id: CommandIds.toggleExplorerPane,
            title: "Explorer",
            category: "View",
            icon: "folder",
            windows: mainWindow,
            execute: (ctx) => ctx.eventBus.publish(new TogglePaneCommand(PaneIds.explorer)),
        }),

        new AppCommand({
            id: CommandIds.toggleOutputPane,
            title: "Output",
            category: "View",
            icon: "output",
            windows: mainWindow,
            execute: (ctx) => ctx.eventBus.publish(new TogglePaneCommand(PaneIds.output)),
        }),

        new AppCommand({
            id: CommandIds.toggleCodePane,
            title: "Code",
            category: "View",
            icon: "code",
            windows: mainWindow,
            execute: (ctx) => ctx.container.get(IPaneManager).toggle(PaneIds.code),
        }),

        new AppCommand({
            id: CommandIds.toggleNamespacesPane,
            title: "Namespaces",
            category: "View",
            icon: "namespaces",
            windows: mainWindow,
            execute: (ctx) => ctx.eventBus.publish(new TogglePaneCommand(PaneIds.namespaces)),
        }),

        new AppCommand({
            id: CommandIds.reloadWindow,
            title: "Reload",
            category: "View",
            execute: () => window.location.reload(),
        }),

        new AppCommand({
            id: CommandIds.toggleDeveloperTools,
            title: "Toggle Developer Tools",
            category: "View",
            execute: (ctx) => ctx.container.get(IWindowService).toggleDeveloperTools(),
        }),

        new AppCommand({
            id: CommandIds.zoomIn,
            title: "Zoom In",
            category: "View",
            icon: "zoom-in",
            execute: (ctx) => ctx.container.get(IWindowService).zoomIn(),
        }),

        new AppCommand({
            id: CommandIds.zoomOut,
            title: "Zoom Out",
            category: "View",
            icon: "zoom-out",
            execute: (ctx) => ctx.container.get(IWindowService).zoomOut(),
        }),

        new AppCommand({
            id: CommandIds.zoomReset,
            title: "Reset Zoom",
            category: "View",
            execute: (ctx) => ctx.container.get(IWindowService).resetZoom(),
        }),

        new AppCommand({
            id: CommandIds.toggleFullScreen,
            title: "Toggle Full Screen",
            category: "View",
            execute: (ctx) => ctx.container.get(IWindowService).toggleFullScreen(),
        }),

        new AppCommand({
            id: CommandIds.toggleVimMode,
            title: "Vim Mode",
            category: "View",
            windows: mainWindow,
            execute: (ctx) => {
                const settings = ctx.container.get(Settings);
                settings.editor.vim.enabled = !settings.editor.vim.enabled;
                return ctx.container.get(ISettingsService).update(settings);
            },
        }),

        new AppCommand({
            id: CommandIds.openCommandPalette,
            title: "Command Palette",
            category: "View",
            execute: (ctx) => ctx.container.get(ICommandPalette).open(">"),
        }),

        // Scripts
        new AppCommand({
            id: CommandIds.runScript,
            title: "Run",
            category: "Scripts",
            icon: "run",
            windows: mainWindow,
            isEnabled: (ctx) => {
                const status = ctx.session.active?.status;
                return !!status && status !== "Running" && status !== "Stopping";
            },
            execute: (ctx) => ctx.eventBus.publish(new RunScriptCommand(ctx.argAs<string>())),
        }),

        new AppCommand({
            id: CommandIds.stopScript,
            title: "Stop",
            category: "Scripts",
            icon: "stop",
            windows: mainWindow,
            isEnabled: (ctx) => ctx.session.active?.status === "Running",
            execute: (ctx) => ctx.eventBus.publish(new StopScriptCommand(ctx.argAs<string>())),
        }),

        // Tabs
        new AppCommand({
            id: CommandIds.closeOtherTabs,
            title: "Close Other Tabs",
            category: "File",
            windows: mainWindow,
            execute: (ctx) => ctx.eventBus.publish(new CloseTabsCommand("others", ctx.argAs<string>())),
        }),

        new AppCommand({
            id: CommandIds.closeAllTabs,
            title: "Close All Tabs",
            category: "File",
            windows: mainWindow,
            execute: (ctx) => ctx.eventBus.publish(new CloseTabsCommand("all")),
        }),

        // Tools
        new AppCommand({
            id: CommandIds.checkAppDependencies,
            title: "App Dependency Check",
            category: "Tools",
            icon: "app-deps-check",
            execute: async (ctx) => {
                const {AppDependenciesCheckDialog} =
                    await import("@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog");
                await (await dialogUtil(ctx)).toggle(AppDependenciesCheckDialog);
            },
        }),

        new AppCommand({
            id: CommandIds.stopRunningScripts,
            title: "Stop Running Scripts",
            category: "Tools",
            icon: "stop",
            description: "Stop all running scripts.",
            windows: mainWindow,
            isEnabled: (ctx) => ctx.session.environments.some(e => e.status === "Running"),
            execute: (ctx) => ctx.container.get(IScriptService).stopAll(false),
        }),

        new AppCommand({
            id: CommandIds.stopScriptsAndRunners,
            title: "Stop Scripts and Runners",
            category: "Tools",
            icon: "stop",
            description: "Stop all running scripts and idle runners that are alive in the background.",
            windows: mainWindow,
            isEnabled: (ctx) => ctx.session.environments.some(e => e.isScriptHostRunning),
            execute: (ctx) => ctx.container.get(IScriptService).stopAll(true),
        }),

        // Help
        new AppCommand({
            id: CommandIds.quickStart,
            title: "Quick Start",
            category: "Help",
            icon: "quick-start",
            description: "The five things that get you from an empty editor to live results.",
            execute: async (ctx) => {
                const {QuickStartDialog} = await import("@application/app/quick-start-dialog/quick-start-dialog");
                await (await dialogUtil(ctx)).toggle(QuickStartDialog);
            },
        }),

        new AppCommand({
            id: CommandIds.openWiki,
            title: "Wiki",
            category: "Help",
            icon: "wiki",
            execute: (ctx) => ctx.container.get(ISystemService)
                .openUrlInBrowser("https://tareqimbasher.github.io/NetPad"),
        }),

        new AppCommand({
            id: CommandIds.openGitHub,
            title: "GitHub",
            category: "Help",
            icon: "github",
            execute: (ctx) => ctx.container.get(ISystemService)
                .openUrlInBrowser("https://github.com/tareqimbasher/NetPad"),
        }),

        new AppCommand({
            id: CommandIds.searchIssues,
            title: "Search Issues",
            category: "Help",
            icon: "github",
            execute: (ctx) => ctx.container.get(ISystemService)
                .openUrlInBrowser("https://github.com/tareqimbasher/NetPad/issues"),
        }),

        new AppCommand({
            id: CommandIds.checkForUpdates,
            title: "Check for Updates",
            category: "Help",
            icon: "app-update",
            execute: async (ctx) => {
                const {AppUpdateDialog} = await import("@application/app/app-update-dialog/app-update-dialog");
                await (await dialogUtil(ctx)).toggle(AppUpdateDialog);
            },
        }),

        new AppCommand({
            id: CommandIds.about,
            title: "About",
            category: "Help",
            icon: "star",
            execute: settingsDestination("about"),
        }),
    ];
}

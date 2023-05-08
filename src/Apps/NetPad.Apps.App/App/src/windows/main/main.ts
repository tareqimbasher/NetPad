import {AppTask, Aurelia, IContainer, ILogger, Registration} from "aurelia";
import {IDialogService} from "@aurelia/dialog";
import * as monaco from "monaco-editor";
import {IQuickInputService} from 'monaco-editor/esm/vs/platform/quickinput/common/quickInput';
import {IBackgroundService} from "@common";
import {
    DataConnectionService,
    IAppService,
    IDataConnectionService,
    IScriptService,
    ISession,
    Script,
    ScriptService,
} from "@domain";
import {
    BuiltinCompletionProvider,
    DataConnectionName,
    DialogBackgroundService,
    IActionProvider,
    ICommandProvider,
    ICompletionItemProvider,
    IPaneManager,
    IShortcutManager,
    IWindowBootstrapper,
    PaneHost,
    PaneManager,
    ShortcutManager,
} from "@application";
import {Window} from "./window";
import {QuickTipsDialog} from "@application/dialogs/quick-tips-dialog/quick-tips-dialog";
import {Workbench} from "./workbench";
import {IStatusbarService, StatusbarService} from "./statusbar/statusbar-service";
import {IMainMenuService, MainMenuService} from "./titlebar/main-menu/main-menu-service";
import {IWorkAreaAppearance, WorkAreaAppearance} from "./work-area/work-area-appearance";
import {IWorkAreaService, WorkAreaService} from "./work-area/work-area-service";
import {ITextEditor, TextEditor} from "@application/editor/text-editor";
import {ITextEditorService, TextEditorService} from "@application/editor/text-editor-service";
import {AppWindows} from "@application/windows/app-windows";
import {ExcelService, IExcelService} from "@application/data/excel-service";

export class Bootstrapper implements IWindowBootstrapper {
    constructor(private readonly logger: ILogger) {
    }

    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(AppWindows, AppWindows),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(ITextEditorService, TextEditorService),
            Registration.singleton(IDataConnectionService, DataConnectionService),
            Registration.singleton(IBackgroundService, DialogBackgroundService),
            Registration.singleton(IWorkAreaService, WorkAreaService),
            Registration.singleton(IMainMenuService, MainMenuService),
            Registration.singleton(IStatusbarService, StatusbarService),
            Registration.singleton(IWorkAreaAppearance, WorkAreaAppearance),
            Registration.singleton(Workbench, Workbench),
            Registration.transient(ITextEditor, TextEditor),
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IActionProvider, BuiltInActionProvider),
            Registration.singleton(ICommandProvider, BuiltInActionProvider),
            Registration.singleton(ICompletionItemProvider, BuiltinCompletionProvider),
            Registration.singleton(IExcelService, ExcelService),
            PaneHost,
            DataConnectionName,
            AppTask.activated(IContainer, async container => {
                container.get(IAppService).notifyClientAppIsReady();
                await QuickTipsDialog.showIfFirstVisit(container.get(IDialogService));
            })
        );

        try {
            this.registerPlugins(app.container);
        } catch (ex) {
            this.logger.error(`Error occurred while registering plugins`, ex);
        }
    }

    private registerPlugins(container: IContainer) {
        const requireContext = require.context('@plugins', true, /plugin\.ts$/);

        const pluginPaths = requireContext
            .keys()
            .filter((k: string) => {
                // For a plugin.ts file in "@plugins/plugin-dir/plugin.ts", require.context will return:
                // 1. "@plugins/plugin-dir/plugin.ts"
                // 2. "./plugin-dir/plugin.ts"
                // 3. "core/@plugins/plugin-dir/plugin.ts"
                //
                // We only want the one that starts with "@plugins"
                const startsWithPluginsRoot = k.startsWith("@plugins");

                // We don't want plugin.ts files that are not direct descendants of @plugins/plugin-dir/
                const directDescendantOfPluginDir = k
                    .replaceAll("\\", "/")
                    .split("/")
                    .length === 3;

                return startsWithPluginsRoot && directDescendantOfPluginDir;
            });

        for (const pluginPath of pluginPaths) {
            try {
                const plugin = requireContext(pluginPath);
                if (plugin.configure) {
                    plugin.configure(container);
                    this.logger.info(`Loaded plugin: ${pluginPath}`);
                }
            } catch (ex) {
                this.logger.error(`Could not load plugin: ${pluginPath}`, ex);
            }
        }
    }
}

class BuiltInActionProvider implements IActionProvider, ICommandProvider {
    constructor(@IContainer private readonly container: IContainer) {
    }

    public provideCommands() {
        return [{
            id: "builtin.commands.quickInput",
            handler: async (accessor, func: (service: unknown) => Promise<void>) => {
                const quickInputService = accessor.get(IQuickInputService)
                await func(quickInputService);
            }
        }];
    }
    public provideActions(): monaco.editor.IActionDescriptor[] {
        return [
            {
                id: "builtin.actions.go-to-script",
                label: "Go to Script",
                keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyT],
                run: (editor) => {
                    editor.trigger("", "builtin.commands.quickInput", async (quickInput) => {

                        const scope = this.container.createChild({inheritParentResources: true});

                        try {
                            const session = scope.get(ISession);
                            const opened = [...session.environments];

                            const toPick = (script: Script, isOpen: boolean) => {
                                return {
                                    type: 'item',
                                    id: script.id,
                                    label: (isOpen ? "$(circle-filled)" : "$(code)") + script.name,
                                    description: !script.path ? "(New)" : ((script.isDirty ? "(Modified) " : "") + script.path),
                                    //detail: "test detail"
                                    //meta: "test meta",
                                    script: script
                                }
                            };

                            const open = (script: Script) => {
                                if (script.path) session.openByPath(script.path);
                                else session.activate(script.id);
                            };

                            const picks: Partial<{
                                type: string,
                                id: string,
                                label: string,
                                meta: string,
                                description: string,
                                detail: string
                            }>[] =
                                opened
                                    .map(env => toPick(env.script, true))
                                    .sort((a, b) => a.id === session.active?.script.id ? -1 : 1);

                            quickInput.pick(picks, {placeholder: "Go to script"}).then((selected) => {
                                if (!selected) {
                                    // Cancelled
                                    return;
                                }

                                open(selected.script);
                            });

                            const service = scope.get(IScriptService);
                            const scripts = await service.getScripts();

                            if (scripts.length) {
                                picks.push({
                                    type: "separator"
                                });

                                picks.push(...scripts
                                    .filter(s => picks.every(p => p.id !== s.id))
                                    .map(script => toPick(script as Script, false))
                                    .sort((a, b) => a.label > b.label ? 1 : -1)
                                );

                                quickInput.pick(picks, {placeholder: "Go to script"}).then((selected) => {
                                    if (!selected) {
                                        // Cancelled
                                        return;
                                    }

                                    open(selected.script);
                                });
                            }
                        } finally {
                            scope.dispose();
                        }
                    });
                }
            }
        ];
    }
}

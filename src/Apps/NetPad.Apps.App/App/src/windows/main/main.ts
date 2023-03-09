import {AppTask, Aurelia, IContainer, IDialogService, ILogger, Registration} from "aurelia";
import {DataConnectionService, IAppService, IDataConnectionService, IScriptService, ScriptService,} from "@domain";
import {Window} from "./window";
import {
    BuiltinCompletionProvider,
    DataConnectionName,
    DialogBackgroundService,
    Editor,
    ICompletionItemProvider,
    IPaneManager,
    IShortcutManager,
    IWindowBootstrapper,
    PaneHost,
    PaneManager,
    ShortcutManager,
} from "@application";
import {IBackgroundService} from "@common";
import {QuickTipsDialog} from "@application/dialogs/quick-tips-dialog/quick-tips-dialog";

export class Bootstrapper implements IWindowBootstrapper {
    constructor(private readonly logger: ILogger) {
    }

    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(ICompletionItemProvider, BuiltinCompletionProvider),
            Registration.singleton(IDataConnectionService, DataConnectionService),
            Registration.singleton(IBackgroundService, DialogBackgroundService),
            PaneHost,
            Editor,
            DataConnectionName,
            AppTask.afterActivate(IContainer, async container => {
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

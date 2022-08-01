import {Aurelia, IContainer, ILogger, Registration} from "aurelia";
import {AppService, IAppService, IScriptService, ScriptService,} from "@domain";
import {Window} from "./window";
import {
    BuiltinCompletionProvider,
    Editor,
    ICompletionItemProvider,
    IPaneManager,
    IShortcutManager,
    IWindowBootstrapper,
    PaneHost,
    PaneManager,
    ShortcutManager,
    WebDialogBackgroundService,
    WebWindowBackgroundService
} from "@application";
import {IBackgroundService, System} from "@common";

export class Bootstrapper implements IWindowBootstrapper {
    constructor(private readonly logger: ILogger) {
    }

    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAppService, AppService),
            Registration.singleton(ICompletionItemProvider, BuiltinCompletionProvider),
            PaneHost,
            Editor
        );

        if (!System.isRunningInElectron()) {
            app.register(
                Registration.transient(IBackgroundService, WebDialogBackgroundService),
                Registration.transient(IBackgroundService, WebWindowBackgroundService)
            );
        }

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

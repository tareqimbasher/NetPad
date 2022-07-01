import {Aurelia, Registration} from "aurelia";
import {
    IAppService,
    IScriptService,
    IShortcutManager,
    AppService,
    ScriptService,
    ShortcutManager,
} from "@domain";
import {Index} from "./index";
import {
    BuiltinCompletionProvider,
    Editor,
    ICompletionItemProvider,
    IPaneManager,
    IWindowBootstrap,
    PaneHost,
    PaneManager
} from "@application";
import {OmniSharpPlugin} from "@plugins/omnisharp";

export class Bootstrapper implements IWindowBootstrap {
    getEntry = () => Index;

    registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAppService, AppService),
            Registration.singleton(ICompletionItemProvider, BuiltinCompletionProvider),
            PaneHost,
            Editor
        );

        OmniSharpPlugin.configure(app.container);
    }
}

import {Aurelia, Registration} from "aurelia";
import {
    IAppService,
    IScriptService,
    IShortcutManager,
    AppService,
    ScriptService,
    ShortcutManager,
} from "@domain";
import {Window} from "./window";
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
    getEntry = () => Window;

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

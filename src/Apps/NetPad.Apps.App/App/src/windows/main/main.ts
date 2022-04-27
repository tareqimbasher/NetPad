import {Aurelia, Registration} from "aurelia";
import {
    IAppService,
    IOmniSharpService,
    IScriptService,
    IShortcutManager,
    AppService,
    OmniSharpService,
    ScriptService,
    ShortcutManager,
} from "@domain";
import {Index} from "./index";
import {Editor, IPaneManager, IWindowBootstrap, PaneHost, PaneManager} from "@application";

export class Bootstrapper implements IWindowBootstrap {
    getEntry = () => Index;

    registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAppService, AppService),
            Registration.singleton(IOmniSharpService, OmniSharpService),
            PaneHost,
            Editor
        );
    }
}

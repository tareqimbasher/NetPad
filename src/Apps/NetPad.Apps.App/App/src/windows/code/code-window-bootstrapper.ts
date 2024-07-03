import {
    ICodeService,
    IPaneManager,
    IShortcutManager,
    IWindowBootstrapper,
    PaneHost,
    PaneManager,
    ShortcutManager
} from "@application";
import {Window} from "./window";
import {Aurelia, Registration} from "aurelia";
import {PaneToolbar} from "@application/panes/pane-toolbar";
import {ITextEditorService, TextEditorService} from "@application/editor/text-editor-service";
import {CodeService} from "@application/code/code-service";

export class CodeWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPaneManager, PaneManager),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(ICodeService, CodeService),
            Registration.singleton(ITextEditorService, TextEditorService),
            PaneHost,
            PaneToolbar,
        );
    }
}

import {all} from "aurelia";
import Split from "split.js";
import * as monaco from "monaco-editor";
import {BuiltinShortcuts, IShortcutManager, Settings, Shortcut} from "@domain";
import {
    ICompletionItemProvider,
    IPaneManager,
    PaneHost,
    PaneHostOrientation
} from "@application";
import {KeyCode} from "@common";
import {ClipboardPane, NamespacesPane} from "./panes";

export class Window {
    public rightPaneHost: PaneHost;

    constructor(
        private readonly settings: Settings,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IPaneManager private readonly paneManager: IPaneManager,
        @all(ICompletionItemProvider) private readonly completionItemProviders: ICompletionItemProvider[]) {
    }

    public async binding() {
        this.shortcutManager.initialize();
        this.registerKeyboardShortcuts();

        for (const completionItemProvider of this.completionItemProviders) {
            monaco.languages.registerCompletionItemProvider("csharp", completionItemProvider);
        }
    }

    public attached() {
        Split(["sidebar", "script-environments"], {
            gutterSize: 6,
            sizes: [14, 86],
            minSize: [100, 300],
        });

        const viewStateController = {
            split: null,
            expand: (paneHost) => {
                viewStateController.split = Split(["#content-left", `pane-host[data-id='${paneHost.id}']`], {
                    gutterSize: 6,
                    sizes: [85, 15],
                    minSize: [300, 100],
                });
            },
            collapse: (paneHost) => {
                viewStateController.split?.destroy();
            }
        };

        this.rightPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Right, viewStateController);
        this.paneManager.addPaneToHost(NamespacesPane, this.rightPaneHost);
        this.paneManager.addPaneToHost(ClipboardPane, this.rightPaneHost);
    }

    private registerKeyboardShortcuts() {
        for (const builtinShortcut of BuiltinShortcuts) {
            this.shortcutManager.registerShortcut(builtinShortcut);
        }

        this.shortcutManager.registerShortcut(
            new Shortcut("Namespaces Pane")
                .withAltKey()
                .withKey(KeyCode.KeyN)
                .hasAction(() => this.paneManager.activateOrCollapse(NamespacesPane))
                .configurable()
                .enabled()
        );
    }
}

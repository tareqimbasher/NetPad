import Split from "split.js";
import {IShortcutManager, Pane, ShortcutIds} from "@application";
import {LocalStorageValue} from "@common";

interface IExplorerSettings {
    flow: string;
}

export class Explorer extends Pane {
    private split: Split.Instance | undefined = undefined;
    private readonly settings = new LocalStorageValue<IExplorerSettings>("explorer");

    constructor(
        private readonly element: HTMLElement,
        @IShortcutManager shortcutManager: IShortcutManager) {
        super("Explorer", "explorer-icon");
        this.hasShortcut(shortcutManager.getShortcut(ShortcutIds.openExplorer));
    }

    public bound() {
        if (!this.settings.value) {
            this.settings.value = {
                flow: "flex-column"
            };
        }
    }

    public async attached() {
        this.initSplit();
    }

    private initSplit() {
        const elements = this.element.getElementsByClassName("explorer-content")[0].children;
        this.split = Split(Array.from(elements) as HTMLElement[], {
            gutterSize: 6,
            direction: "vertical",
            sizes: [50, 50],
            minSize: [100, 100],
        });
    }

    public reversePanelFlow() {
        this.settings.value!.flow = this.settings.value?.flow === "flex-column-reverse"
            ? "flex-column"
            : "flex-column-reverse";

        this.settings.save();
        this.split?.destroy();
        setTimeout(() => this.initSplit(), 0);
    }
}


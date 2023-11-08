import Split from "split.js";
import {IShortcutManager, Pane, ShortcutIds} from "@application";

export class Explorer extends Pane {
    constructor(@IShortcutManager private readonly shortcutManager: IShortcutManager) {
        super("Explorer", "explorer-icon");
        this.hasShortcut(shortcutManager.getShortcut(ShortcutIds.openExplorer));
    }

    public async attached() {
        Split(["#connection-list", "#script-list"], {
            gutterSize: 6,
            direction: "vertical",
            sizes: [50, 50],
            minSize: [100, 100],
        });
    }
}


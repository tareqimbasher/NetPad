import Split from "split.js";
import {IShortcutManager, Pane} from "@application";

export class Explorer extends Pane {
    constructor(@IShortcutManager private readonly shortcutManager: IShortcutManager) {
        super("Explorer", "explorer-icon");
        const shortcut = shortcutManager.getShortcutByName("Explorer");
        if (shortcut) this.hasShortcut(shortcut);
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


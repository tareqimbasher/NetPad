import {Pane, PaneAction} from "@application";

export class ClipboardPane extends Pane {
    public history: Set<string>;
    public selected?: string;
    private readonly maxHistorySize = 200;

    constructor() {
        super("Clipboard", "clipboard-icon");
        this.history = new Set<string>();
        this._actions.push(new PaneAction(
            "<i class=\"delete-icon\"></i> Clear all",
            "Remove all entries",
            () => this.history.clear())
        );
    }

    public binding() {
        document.addEventListener("copy", ev => this.addHistory());
        document.addEventListener("cut", ev => this.addHistory());
    }

    public async select(entry: string, event: MouseEvent) {
        await navigator.clipboard.writeText(entry);
        this.selected = entry;
        setTimeout(() => this.selected = undefined, 1000);
    }

    public removeEntry(entry: string) {
        this.history.delete(entry);
    }

    private addHistory() {
        navigator.clipboard.readText().then(s => {
            if (!s || !s.trim()) return;
            this.history.add(s);

            if (this.history.size > this.maxHistorySize) {
                Array.from(this.history)
                    .slice(0, this.history.size - this.maxHistorySize)
                    .forEach(e => this.history.delete(e));
            }
        });
    }
}

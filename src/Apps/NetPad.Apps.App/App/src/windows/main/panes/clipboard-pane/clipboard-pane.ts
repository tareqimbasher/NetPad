import {Pane} from "@application";

export class ClipboardPane extends Pane {
    public history: Set<string>;
    public selected?: string;
    public searchTerm = "";
    public caseSensitiveSearch = false;
    private readonly maxHistorySize = 100;

    constructor() {
        super("Clipboard", "clipboard-icon");
        this.history = new Set<string>();
    }

    public get viewableHistory(): Set<string> | Array<string> {
        if (!this.searchTerm) return this.history;

        const term = this.caseSensitiveSearch
            ? this.searchTerm
            : this.searchTerm.toLocaleLowerCase();

        return !term
            ? this.history
            : Array.from(this.history)
                .filter(x => (this.caseSensitiveSearch ? x : x.toLocaleLowerCase()).indexOf(term) >= 0);
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

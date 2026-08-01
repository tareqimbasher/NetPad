import {IContainer, ILogger, observable, watch} from "aurelia";
import {MatchSegment} from "@common";
import {ViewModelBase} from "@application/view-model-base";
import {ITextEditorService} from "@application/editor/itext-editor-service";
import {MonacoEditorUtil} from "@application/editor/monaco/monaco-editor-util";
import {CommandPaletteService} from "./command-palette-service";
import {IPaletteSource} from "./ipalette-source";
import {palettePrefixes, PaletteMode, prefixOf} from "./palette-grammar";
import {PaletteGroup, PaletteItem} from "./palette-item";
import {comparePaletteMatches, matchPaletteItem, orderGroupsByBestMatch, PaletteMatch} from "./palette-matching";

export interface PaletteRow {
    item: PaletteItem;
    segments: MatchSegment[];
    /** The detail, split for rendering. Carries the hits when the query found the row by detail. */
    detailSegments?: MatchSegment[];
    /** Position in the whole list, across groups. */
    index: number;
    /** Whether the row carries no leading mark and needs an empty slot to keep its title aligned. */
    spacer: boolean;
}

interface PaletteRowGroup {
    label?: string;
    rows: PaletteRow[];
}

/**
 * The monaco editor quick input box a prefix leads to. These prefixes close this command palette and
 * open the monaco editor quick input box and hands off control to it.
 */
const editorHandoffs: ReadonlyMap<PaletteMode, { action: string; subject: string }> = new Map([
    [PaletteMode.Line, {action: "editor.action.gotoLine", subject: "Go to line"}],
    [PaletteMode.Symbol, {action: "editor.action.quickOutline", subject: "Go to symbol"}],
]);

const legend: ReadonlyArray<{ key: string; label: string }> = [
    {key: ">", label: "commands"},
    {key: ":", label: "line"},
    {key: "@", label: "symbol"},
    {key: "?", label: "help"},
];

export class CommandPalette extends ViewModelBase {
    /** The grammar character in force, shown ahead of the query. Empty means scripts. */
    public prefix = "";
    @observable public query!: string;
    public groups: PaletteRowGroup[] = [];
    public note?: string;
    public selectedIndex = 0;
    public readonly legend = legend;

    private rows: PaletteRow[] = [];
    private readonly sources: IPaletteSource[];
    private readonly textEditorService?: ITextEditorService;
    private loadedMode?: PaletteMode;
    private loadedGroups: PaletteGroup[] = [];
    private settingQuery = false;
    private previouslyFocused?: HTMLElement;
    private inputElement?: HTMLInputElement;
    private listElement?: HTMLElement;

    constructor(
        public readonly state: CommandPaletteService,
        @IContainer container: IContainer,
        @ILogger logger: ILogger) {
        super(logger);

        this.sources = [...container.getAll(IPaletteSource)].sort((a, b) => a.order - b.order);

        // Some windows don't use the editor
        this.textEditorService = container.has(ITextEditorService, true)
            ? container.get(ITextEditorService)
            : undefined;

        this.setQuery("");
    }

    public get isOpen(): boolean {
        return this.state.isOpen;
    }

    public get mode(): PaletteMode {
        return palettePrefixes.get(this.prefix) ?? PaletteMode.Scripts;
    }

    public get placeholder(): string {
        if (this.mode === PaletteMode.Scripts) return "Search scripts…";
        if (this.mode === PaletteMode.Commands) return "Search commands…";
        return "";
    }

    /** The total shown rows. */
    public get matchCount(): string | undefined {
        if (this.mode === PaletteMode.Help || this.note) return undefined;

        const count = this.rows.length;
        if (this.query) return `${count} ${count === 1 ? "match" : "matches"}`;

        const subject = this.mode === PaletteMode.Scripts ? "script" : "command";
        return `${count} ${subject}${count === 1 ? "" : "s"}`;
    }

    public close() {
        if (!this.state.isOpen) return;

        this.state.close();
        this.loadedMode = undefined;

        if (this.previouslyFocused?.isConnected) this.previouslyFocused.focus();
        this.previouslyFocused = undefined;
    }

    public async run(row: PaletteRow) {
        if (!row.item.keepOpen) this.close();
        await row.item.run();
    }

    public select(index: number) {
        if (!this.rows.length) return;

        this.selectedIndex = Math.min(Math.max(index, 0), this.rows.length - 1);
        this.scrollSelectionIntoView();
    }

    public onKeyDown(event: KeyboardEvent) {
        // The palette owns the keyboard while it is showing. App keybindings resume when it closes.
        event.stopPropagation();

        switch (event.key) {
            case "Escape":
                this.close();
                break;
            case "ArrowDown":
                this.moveSelection(1);
                break;
            case "ArrowUp":
                this.moveSelection(-1);
                break;
            case "Home":
                this.select(0);
                break;
            case "End":
                this.select(this.rows.length - 1);
                break;
            case "Enter":
                void this.runSelected();
                break;
            case "Tab":
                break;
            case "Backspace":
                // Backspacing off the front of the query drops the prefix, landing in scripts.
                if (!this.prefix || this.inputElement?.selectionStart !== 0 || this.inputElement.selectionEnd !== 0) {
                    return;
                }

                this.prefix = "";
                this.refresh();
                break;
            default:
                return;
        }

        // A key the palette consumed/handled should be canceled.
        event.preventDefault();
    }

    public queryChanged() {
        if (this.settingQuery) return;

        // A grammar character typed at the head of the query is the grammar, not a search for it.
        const typed = this.query[0];
        if (palettePrefixes.has(typed)) {
            this.prefix = typed;
            this.setQuery(this.query.substring(1));
        }

        this.refresh();
    }

    @watch<CommandPalette>(vm => vm.state.openCount)
    private opened() {
        if (!this.state.isOpen) return;

        this.previouslyFocused = document.activeElement as HTMLElement;
        this.prefix = this.state.prefix;
        this.setQuery("");
        this.refresh();

        setTimeout(() => this.inputElement?.select(), 0);
    }

    private async runSelected() {
        const row = this.rows[this.selectedIndex];
        if (row) await this.run(row);
    }

    private moveSelection(delta: number) {
        if (!this.rows.length) return;

        this.selectedIndex = (this.selectedIndex + delta + this.rows.length) % this.rows.length;
        this.scrollSelectionIntoView();
    }

    private scrollSelectionIntoView() {
        setTimeout(() => {
            const rows = this.listElement?.querySelectorAll(".pal-row");
            rows?.[this.selectedIndex]?.scrollIntoView({block: "nearest"});
        }, 0);
    }

    private setQuery(value: string) {
        this.settingQuery = true;
        this.query = value;
        this.settingQuery = false;
    }

    private refresh() {
        const mode = this.mode;

        if (this.handOffToEditor(mode)) return;

        if (mode !== this.loadedMode) {
            this.loadedMode = mode;
            this.loadedGroups = this.loadGroups(mode);
        }

        this.buildRows(this.loadedGroups);
    }

    /**
     * If {@link mode} is one that hands off to the editor's quick input box this opens that quick input, handing
     * over whatever was typed.
     *
     * Returns whether the mode was one that hands off to the editor quick input.
     */
    private handOffToEditor(mode: PaletteMode): boolean {
        const handoff = editorHandoffs.get(mode);
        if (!handoff) return false;

        const editor = this.textEditorService?.active?.monaco;

        if (!editor) {
            this.loadedMode = mode;
            this.loadedGroups = [];
            this.buildRows([]);
            this.note = `${handoff.subject} needs an active editor.`;
            return true;
        }

        const carriedOver = prefixOf(mode) + this.query;
        this.close();
        editor.focus();

        try {
            MonacoEditorUtil.getQuickInputService().quickAccess.show(carriedOver);
        } catch (err) {
            this.logger.debug("Quick access was unavailable, triggering the editor action instead", err);
            editor.trigger(null, handoff.action, null);
        }

        return true;
    }

    private loadGroups(mode: PaletteMode): PaletteGroup[] {
        if (mode === PaletteMode.Help) return [{items: this.helpItems()}];

        return this.sources
            .filter(source => source.mode === mode)
            .flatMap(source => source.getGroups());
    }

    private buildRows(groups: PaletteGroup[]) {
        const matched: { group: PaletteRowGroup; best: PaletteMatch }[] = [];

        for (const group of groups) {
            const hits: { item: PaletteItem; match: PaletteMatch }[] = [];

            for (const item of group.items) {
                const match = matchPaletteItem(item, this.query);
                if (match) hits.push({item, match});
            }

            if (!hits.length) continue;

            // A stable sort, so an unqueried list keeps the order its source built it in.
            hits.sort((a, b) => comparePaletteMatches(a.match, b.match));

            matched.push({
                group: {
                    label: group.label,
                    rows: hits.map(hit => ({
                        item: hit.item,
                        segments: hit.match.titleSegments,
                        detailSegments: hit.match.detailSegments,
                        index: 0,
                        spacer: hit.item.badge === undefined && !hit.item.prefixCap && !hit.item.icon,
                    })),
                },
                best: hits[0].match,
            });
        }

        // Put groups holding the best answer first
        const ordered = this.query ? orderGroupsByBestMatch(matched, m => m.best) : matched;

        this.groups = ordered.map(m => m.group);
        this.rows = this.groups.flatMap(group => group.rows);
        this.rows.forEach((row, index) => row.index = index);
        this.selectedIndex = 0;
        this.note = this.rows.length ? undefined : this.emptyNote();

        setTimeout(() => this.listElement?.scrollTo({top: 0}), 0);
    }

    private emptyNote(): string {
        if (this.mode !== PaletteMode.Scripts) return "No matching commands.";

        return this.sources.some(source => source.mode === PaletteMode.Scripts)
            ? "No matching scripts."
            : "Scripts can only be reached from the main window.";
    }

    private helpItems(): PaletteItem[] {
        const modes = [
            {prefix: "", title: "Go to script — just start typing", detail: "open · recent · library"},
            {prefix: ">", title: "Run a command", detail: "application · editor"},
            {prefix: ":", title: "Go to line / column", detail: "→ editor quick input"},
            {prefix: "@", title: "Go to symbol", detail: "→ editor quick input"},
        ];

        return modes.map(mode => ({
            id: `help:${mode.prefix || "scripts"}`,
            title: mode.title,
            prefixCap: mode.prefix || undefined,
            detail: mode.detail,
            keepOpen: true,
            run: () => {
                this.prefix = mode.prefix;
                this.setQuery("");
                this.refresh();
                this.inputElement?.focus();
            },
        }));
    }
}

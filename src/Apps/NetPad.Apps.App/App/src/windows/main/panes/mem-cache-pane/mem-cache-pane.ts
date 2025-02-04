import {IPaneManager, IScriptService, ISession, MemCacheItemInfo, Pane,} from "@application";
import {DisposableCollection} from "@common";
import {OutputPane} from "../output-pane/output-pane";

export class MemCachePane extends Pane {
    public selected?: string;
    public searchTerm = "";
    public caseSensitiveSearch = false;
    public disposables = new DisposableCollection();

    constructor(@IScriptService private readonly scriptService: IScriptService,
                @ISession private readonly session: ISession,
                @IPaneManager private readonly paneManager: IPaneManager) {
        super("MemCache", "mem-cache-icon");
    }

    public get items(): Array<MemCacheItemInfo> {
        const items = this.session.active?.memCacheItems;

        if (!items?.length || !this.searchTerm) {
            return items ?? [];
        }

        const term = this.caseSensitiveSearch
            ? this.searchTerm
            : this.searchTerm.toLocaleLowerCase();

        return items
            .filter(x => (this.caseSensitiveSearch ? x.key : x.key.toLocaleLowerCase()).indexOf(term) >= 0);
    }

    public detaching() {
        this.disposables.dispose();
    }

    public async select(item: MemCacheItemInfo, event: MouseEvent) {
        const scriptId = this.session.active?.script.id;
        if (scriptId) {
            await this.scriptService.dumpMemCacheItem(scriptId, item.key);
            this.paneManager.expand(OutputPane);
        }
    }

    public async removeKey(key: string, ev: MouseEvent) {
        ev.stopPropagation();

        const scriptId = this.session.active?.script.id;
        if (scriptId) {
            await this.scriptService.deleteMemCacheItem(scriptId, key);
        }
    }

    public async clearCache() {
        const scriptId = this.session.active?.script.id;
        if (scriptId) {
            await this.scriptService.clearMemCacheItems(scriptId);
        }
    }
}

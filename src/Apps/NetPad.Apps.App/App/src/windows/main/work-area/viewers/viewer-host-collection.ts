import {ViewerHost} from "./viewer-host";
import {ViewableObject} from "./viewable-object";

export class ViewerHostCollection {
    private _items: ViewerHost[] = [];
    private _active?: ViewerHost | null;

    public get items(): ReadonlyArray<ViewerHost> {
        return this._items;
    }

    public get active(): ViewerHost | undefined | null {
        return this._active;
    }

    public add(viewerHost: ViewerHost) {
        if (this._items.some(x => x.id == viewerHost.id)) {
            throw new Error(`A ${nameof(ViewerHost)} with ID '${viewerHost.id}' already exists`);
        }

        viewerHost.order = this._items.length;
        this._items.push(viewerHost);
    }

    public async activate(viewerHost: ViewerHost | null) {
        this._active = viewerHost;
        await this.active?.activeViewable?.activate(this.active);
    }

    public activateViewable(viewable: ViewableObject) {
        const host = this._items.find(vh => vh.viewables.has(viewable));

        if (!host) {
            throw new Error("No viewer host has this viewable open");
        }

        host.activate(viewable);
    }

    public findViewable(viewableId: string): { viewable: ViewableObject, host: ViewerHost } | undefined {
        for (const host of this._items) {
            const viewable = host.find(viewableId);
            if (viewable)
                return {viewable, host};
        }

        return undefined;
    }
}

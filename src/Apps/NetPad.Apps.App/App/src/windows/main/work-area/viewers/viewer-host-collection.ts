import {ViewerHost} from "./viewer-host";
import {ViewableObject} from "./viewable-object";

export class ViewerHostCollection extends Array<ViewerHost> {
    private _active?: ViewerHost | null;

    public get active(): ViewerHost | undefined | null {
        return this._active;
    }

    public async activate(viewerHost: ViewerHost | null) {
        this._active = viewerHost;
        await this.active?.activeViewable?.activate(this.active);
    }

    public activateViewable(viewable: ViewableObject) {
        const host = this.find(vh => vh.viewables.has(viewable));

        if (!host) {
            throw new Error("No viewer host has this viewable open");
        }

        host.activate(viewable);
    }

    public findViewable(viewableId: string): { viewable: ViewableObject, host: ViewerHost } | undefined {
        for (const host of this) {
            const viewable = host.find(viewableId);
            if (viewable)
                return {viewable, host};
        }

        return undefined;
    }
}

import {DisposableCollection} from "@common";
import {IBackgroundService} from "@application/background-services/ibackground-service";
import {IEventBus} from "@application/events/ievent-bus";
import {WindowDeepLinkRequestedEvent} from "@application/api";
import {IWindowDestinations} from "./iwindow-destinations";
import {WindowParams} from "./window-params";

/**
 * Completes a deep link that reached a window after it was already open. Shells raise an open
 * window rather than re-creating it, so what was asked for arrives over IPC instead of in the URL.
 *
 * Only windows that register {@link IWindowDestinations} run this.
 */
export class WindowDestinationBackgroundService implements IBackgroundService {
    private readonly disposables = new DisposableCollection();

    constructor(
        @IEventBus private readonly eventBus: IEventBus,
        @IWindowDestinations private readonly destinations: IWindowDestinations) {
    }

    public start(): Promise<void> {
        this.disposables.add(
            this.eventBus.subscribeToServer(WindowDeepLinkRequestedEvent, msg => this.serve(msg))
        );

        return Promise.resolve();
    }

    public stop(): void {
        this.disposables.dispose();
    }

    private serve(request: WindowDeepLinkRequestedEvent) {
        if (request.windowId !== WindowParams.window) {
            return;
        }

        // A parameter the request leaves out and one it sends empty both mean "not set".
        const requested = (key: string) => request.params?.[key] || null;

        const identityChanged = this.destinations.identityParams
            .some(param => requested(param) !== (WindowParams.get(param) || null));

        if (identityChanged) {
            // What the window shows is fixed when it starts, so identity changed means
            // we reload the window with new params. Identity the request leaves out is
            // cleared, not carried over.
            const changes: Record<string, string | null> = {};

            for (const param of this.destinations.identityParams) {
                changes[param] = requested(param);
            }

            for (const key of Object.keys(request.params ?? {})) {
                changes[key] = requested(key);
            }

            WindowParams.reloadWith(changes);
            return;
        }

        const tab = requested("tab");
        if (tab) {
            this.destinations.goTo(tab);
        }
    }
}

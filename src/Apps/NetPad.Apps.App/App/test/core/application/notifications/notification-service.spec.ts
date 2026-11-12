import {PLATFORM} from "aurelia";
import {AppStatusMessagePublishedEvent, ScriptStatus} from "@application/api";
import {IEventBus} from "@application/events/ievent-bus";
import {ISession} from "@application/sessions/isession";
import {NotificationService} from "@application/notifications/notification-service";

interface CapturedSubscription {
    messageType: unknown;
    handler: (msg: unknown) => unknown;
}

class FakeEventBus {
    public subscriptions: CapturedSubscription[] = [];

    public subscribe(messageType: unknown, handler: (msg: unknown) => unknown) {
        this.subscriptions.push({messageType, handler});
        return {dispose: () => { /* noop */ }};
    }

    public subscribeToServer(messageType: unknown, handler: (msg: unknown) => unknown) {
        this.subscriptions.push({messageType, handler});
        return {dispose: () => { /* noop */ }};
    }

    public fire(messageType: unknown, msg: unknown) {
        for (const s of this.subscriptions) {
            if (s.messageType === messageType) s.handler(msg);
        }
    }
}

class FakeSession {
    public environments: { script: { id: string }, status: ScriptStatus }[] = [];

    public getScriptName(scriptId: string): string {
        return `Script-${scriptId}`;
    }
}

function message(overrides: Record<string, unknown> = {}) {
    return {
        scriptId: undefined,
        text: "msg",
        kind: "Notice",
        severity: "Info",
        createdDate: new Date(),
        ...overrides,
    };
}

describe("NotificationService", () => {
    let eventBus: FakeEventBus;
    let session: FakeSession;
    let service: NotificationService;

    beforeEach(() => {
        eventBus = new FakeEventBus();
        session = new FakeSession();
        service = new NotificationService(eventBus as unknown as IEventBus, session as unknown as ISession);
    });

    afterEach(() => {
        // Progress/Event publishes schedule a real status-bar clear timer; dispose clears it so it
        // doesn't outlive the test.
        service.dispose();
    });

    function publish(overrides: Record<string, unknown> = {}) {
        eventBus.fire(AppStatusMessagePublishedEvent, {message: message(overrides)});
    }

    it("routes Progress to the status bar only (not logged, not toasted)", () => {
        publish({kind: "Transient", text: "Compiling..."});

        expect(service.statusBarMessage?.text).toBe("Compiling...");
        expect(service.history.length).toBe(0);
        expect(service.toasts.length).toBe(0);
    });

    it("routes Event to the status bar and the history", () => {
        publish({kind: "Notice", severity: "Success", text: "Installed package"});

        expect(service.statusBarMessage?.text).toBe("Installed package");
        expect(service.history.length).toBe(1);
        expect(service.toasts.length).toBe(0);
        expect(service.unreadCount).toBe(1);
    });

    it("routes Alert to a toast and the history, never the status bar", () => {
        publish({kind: "Alert", severity: "Error", text: "OmniSharp failed"});

        expect(service.toasts.length).toBe(1);
        expect(service.history.length).toBe(1);
        expect(service.statusBarMessage).toBeNull();
        expect(service.unreadCount).toBe(1);
    });

    it("enriches the script name from the session", () => {
        publish({scriptId: "abc", kind: "Transient", text: "Running..."});

        expect(service.statusBarMessage?.scriptName).toBe("Script-abc");
    });

    it("does not count unread while the pane is open", () => {
        service.setPaneOpen(true);
        publish({kind: "Notice"});

        expect(service.history.length).toBe(1);
        expect(service.unreadCount).toBe(0);
    });

    it("adds an Alert to the history without a toast while the pane is open", () => {
        service.setPaneOpen(true);
        publish({kind: "Alert"});

        expect(service.history.length).toBe(1);
        expect(service.toasts.length).toBe(0);
    });

    it("dismisses the toasts on screen when the pane opens and keeps them in the history", () => {
        publish({kind: "Alert", text: "first"});
        publish({kind: "Alert", text: "second"});

        service.setPaneOpen(true);

        expect(service.toasts.length).toBe(0);
        expect(service.history.map(n => n.text)).toEqual(["second", "first"]);
    });

    it("toasts an Alert again once the pane closes", () => {
        service.setPaneOpen(true);
        service.setPaneOpen(false);
        publish({kind: "Alert"});

        expect(service.toasts.length).toBe(1);
    });

    it("dismissToast removes the toast but keeps the history item", () => {
        publish({kind: "Alert"});
        service.dismissToast(service.toasts[0]);

        expect(service.toasts.length).toBe(0);
        expect(service.history.length).toBe(1);
    });

    it("clearHistory empties the history and unread count", () => {
        publish({kind: "Notice"});
        service.clearHistory();

        expect(service.history.length).toBe(0);
        expect(service.unreadCount).toBe(0);
    });

    describe("status bar clear timer", () => {
        const clearMs = 15000;

        beforeEach(() => {
            // PLATFORM binds the real timer functions when it is created, so route it through Jest's fake ones.
            jest.useFakeTimers();
            jest.spyOn(PLATFORM, "setTimeout").mockImplementation((handler, timeout) => setTimeout(handler, timeout));
            jest.spyOn(PLATFORM, "clearTimeout").mockImplementation(handle => clearTimeout(handle));
        });

        afterEach(() => {
            jest.restoreAllMocks();
            jest.useRealTimers();
        });

        it("keeps a running script's message past the timeout", () => {
            session.environments.push({script: {id: "abc"}, status: "Running"});
            publish({scriptId: "abc", kind: "Transient", text: "Scaffolding database..."});

            jest.advanceTimersByTime(clearMs * 3);

            expect(service.statusBarMessage?.text).toBe("Scaffolding database...");
        });

        it("keeps a stopping script's message past the timeout", () => {
            session.environments.push({script: {id: "abc"}, status: "Stopping"});
            publish({scriptId: "abc", kind: "Transient", text: "Stopping..."});

            jest.advanceTimersByTime(clearMs);

            expect(service.statusBarMessage?.text).toBe("Stopping...");
        });

        it("clears a script's message on the next check once the script is no longer running", () => {
            const environment = {script: {id: "abc"}, status: "Running" as ScriptStatus};
            session.environments.push(environment);
            publish({scriptId: "abc", kind: "Transient", text: "Scaffolding database..."});

            jest.advanceTimersByTime(clearMs);
            environment.status = "Ready";
            jest.advanceTimersByTime(clearMs - 1);
            expect(service.statusBarMessage?.text).toBe("Scaffolding database...");

            jest.advanceTimersByTime(1);
            expect(service.statusBarMessage).toBeNull();
        });

        it("clears a message without a script after the timeout", () => {
            session.environments.push({script: {id: "abc"}, status: "Running"});
            publish({kind: "Transient", text: "Installing package..."});

            jest.advanceTimersByTime(clearMs);

            expect(service.statusBarMessage).toBeNull();
        });

        it("clears a message for a script that is not in the session after the timeout", () => {
            publish({scriptId: "headless", kind: "Transient", text: "Running..."});

            jest.advanceTimersByTime(clearMs);

            expect(service.statusBarMessage).toBeNull();
        });

        it("restarts the timeout when a new message replaces the current one", () => {
            publish({kind: "Transient", text: "first"});
            jest.advanceTimersByTime(clearMs - 1);
            publish({kind: "Transient", text: "second"});

            jest.advanceTimersByTime(clearMs - 1);
            expect(service.statusBarMessage?.text).toBe("second");

            jest.advanceTimersByTime(1);
            expect(service.statusBarMessage).toBeNull();
        });
    });
});

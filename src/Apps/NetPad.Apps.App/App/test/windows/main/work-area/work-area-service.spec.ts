import {Constructable, DI, IContainer, ILogger, Registration} from "aurelia";
import {EnvironmentsRemovedEvent, ScriptEnvironment} from "../../../../src/core/@application/api";
import {IAppService} from "../../../../src/core/@application/app/iapp-service";
import {IEventBus} from "../../../../src/core/@application/events/ievent-bus";
import {IScriptService} from "../../../../src/core/@application/scripts/iscript-service";
import {RunScriptCommand} from "../../../../src/core/@application/scripts/run-script-command";
import {ISession} from "../../../../src/core/@application/sessions/isession";
import {Viewer} from "../../../../src/windows/main/work-area/viewers/viewer";
import {ViewableObject} from "../../../../src/windows/main/work-area/viewers/viewable-object";
import {ViewerHost} from "../../../../src/windows/main/work-area/viewers/viewer-host";
import {
    IViewerRegistry,
    ViewerRegistry,
} from "../../../../src/windows/main/work-area/viewers/viewer-registry";
import {WorkAreaService} from "../../../../src/windows/main/work-area/work-area-service";
import {
    ViewableScriptDocument,
} from "../../../../src/windows/main/work-area/viewers/script-viewer/viewable-script-document";
import {
    IWorkAreaAppearance,
} from "../../../../src/windows/main/work-area/work-area-appearance";

class TestViewable extends ViewableObject {
    constructor(id: string) { super(id); }
    public get name() { return `v-${this.id}`; }
    public get isDirty() { return false; }
    public open(host: ViewerHost): Promise<void> {
        host.addViewables(this);
        return Promise.resolve();
    }
    public close(_h: ViewerHost): Promise<void> { return Promise.resolve(); }
    public activate(_h: ViewerHost): Promise<void> { return Promise.resolve(); }
}

class TestViewer extends Viewer {
    constructor(@ILogger logger: ILogger) { super(logger); }
    public canOpen(v: ViewableObject): boolean { return v instanceof TestViewable; }
    public open(v: ViewableObject): void { this.viewable = v; }
    public close(): void { /* noop */ }
}

interface CapturedSubscription {
    messageType: Constructable<unknown>;
    handler: (msg: unknown) => unknown;
}

class FakeEventBus {
    public subscriptions: CapturedSubscription[] = [];
    public subscribe(messageType: Constructable<unknown>, handler: (msg: unknown) => unknown) {
        this.subscriptions.push({messageType, handler});
        return {dispose: () => { /* noop */ }};
    }
    public subscribeToServer(messageType: Constructable<unknown>, handler: (msg: unknown) => unknown) {
        this.subscriptions.push({messageType, handler});
        return {dispose: () => { /* noop */ }};
    }
    public publish() { /* noop */ }
    public fire<T>(messageType: Constructable<T>, msg: T) {
        for (const s of this.subscriptions) {
            if (s.messageType === messageType) s.handler(msg);
        }
    }
}

class FakeSession {
    public environments: ScriptEnvironment[] = [];
    public active: ScriptEnvironment | null = null;
}

class FakeScriptService {
    public createCalls = 0;
    public create(): Promise<unknown> {
        this.createCalls++;
        return Promise.resolve({});
    }
}

class FakeAppearance {
    public load(): void { /* noop */ }
    public dispose(): void { /* noop */ }
}

class FakeLogger {
    public errors: unknown[][] = [];
    public scopeTo() { return this; }
    public trace() { /* noop */ }
    public debug() { /* noop */ }
    public info() { /* noop */ }
    public warn() { /* noop */ }
    public fatal() { /* noop */ }
    public error(...args: unknown[]) { this.errors.push(args); }
}

function setup(opts: {initialEnvironments?: ScriptEnvironment[]} = {}): {
    container: IContainer,
    service: WorkAreaService,
    session: FakeSession,
    scriptService: FakeScriptService,
    eventBus: FakeEventBus,
    logger: FakeLogger,
} {
    const container = DI.createContainer();
    const session = new FakeSession();
    session.environments = opts.initialEnvironments ?? [];
    const scriptService = new FakeScriptService();
    const eventBus = new FakeEventBus();
    const logger = new FakeLogger();

    container.register(
        Registration.singleton(IViewerRegistry, ViewerRegistry),
        Registration.instance(IWorkAreaAppearance, new FakeAppearance() as unknown as IWorkAreaAppearance),
        Registration.instance(IScriptService, scriptService as unknown as IScriptService),
        Registration.instance(ISession, session as unknown as ISession),
        Registration.instance(IAppService, {} as IAppService),
        Registration.instance(IEventBus, eventBus as unknown as IEventBus),
        Registration.instance(ILogger, logger as unknown as ILogger),
    );

    const registry = container.get(IViewerRegistry);
    registry.register({id: "test", viewerClass: TestViewer, canHandle: v => v instanceof TestViewable});

    return {container, service: container.invoke(WorkAreaService), session, scriptService, eventBus, logger};
}

describe("WorkAreaService.open", () => {
    it("uses the explicitly-supplied host", async () => {
        const {service, container} = setup();
        await service.initialize();
        const second = container.invoke(ViewerHost);
        service.viewerHosts.add(second);
        const v = new TestViewable("v1");

        await service.open(v, second);

        expect(second.viewables.has(v)).toBe(true);
        expect(service.viewerHosts.items[0].viewables.has(v)).toBe(false);
    });

    it("falls back to the active host when no target is supplied", async () => {
        const {service, container} = setup();
        await service.initialize();
        const second = container.invoke(ViewerHost);
        service.viewerHosts.add(second);
        await service.viewerHosts.activate(second);

        const v = new TestViewable("v1");
        await service.open(v);

        expect(second.viewables.has(v)).toBe(true);
    });

    it("falls back to the first host when no target and no active host", async () => {
        const {service} = setup();
        await service.initialize();
        // initialize() creates the first host but does not activate it.

        const v = new TestViewable("v1");
        await service.open(v);

        expect(service.viewerHosts.items[0].viewables.has(v)).toBe(true);
    });

    it("throws when no viewer hosts exist", async () => {
        const {service} = setup();
        // Do not call initialize() — no hosts have been added yet.
        await expect(service.open(new TestViewable("v1"))).rejects.toThrow(/No viewer host available/);
    });
});

describe("WorkAreaService RunScriptCommand routing", () => {
    it("runs the active viewable when no scriptId is supplied", async () => {
        const {service, eventBus} = setup();
        await service.initialize();

        const host = service.viewerHosts.items[0];
        const v = new TestViewable("v1");
        jest.spyOn(v, "canRun").mockReturnValue(true);
        const runSpy = jest.spyOn(v, "run").mockResolvedValue();
        host.addViewables(v);
        host.activate(v);
        await service.viewerHosts.activate(host);

        eventBus.fire(RunScriptCommand, new RunScriptCommand());
        await Promise.resolve();
        expect(runSpy).toHaveBeenCalled();
    });

    it("runs a specific viewable when scriptId is supplied", async () => {
        const {service, eventBus} = setup();
        await service.initialize();

        const host = service.viewerHosts.items[0];
        const v1 = new TestViewable("v1");
        const v2 = new TestViewable("v2");
        jest.spyOn(v1, "canRun").mockReturnValue(true);
        jest.spyOn(v2, "canRun").mockReturnValue(true);
        const runV1 = jest.spyOn(v1, "run").mockResolvedValue();
        const runV2 = jest.spyOn(v2, "run").mockResolvedValue();
        host.addViewables(v1, v2);

        eventBus.fire(RunScriptCommand, new RunScriptCommand("v2"));
        await Promise.resolve();
        expect(runV1).not.toHaveBeenCalled();
        expect(runV2).toHaveBeenCalled();
    });

    it("does not run a viewable that reports canRun() === false", async () => {
        const {service, eventBus} = setup();
        await service.initialize();

        const host = service.viewerHosts.items[0];
        const v = new TestViewable("v1");
        jest.spyOn(v, "canRun").mockReturnValue(false);
        const runSpy = jest.spyOn(v, "run").mockResolvedValue();
        host.addViewables(v);

        eventBus.fire(RunScriptCommand, new RunScriptCommand("v1"));
        await Promise.resolve();
        expect(runSpy).not.toHaveBeenCalled();
    });
});

describe("WorkAreaService.openScriptViewable", () => {
    const environment = (id: string, name: string) =>
        ({script: {id, name}}) as unknown as ScriptEnvironment;

    it("opens the viewable and reports success", async () => {
        const {service} = setup();
        await service.initialize();
        const viewable = new TestViewable("s1");
        service.createScriptViewable = () => viewable as unknown as ViewableScriptDocument;

        const opened = await service.openScriptViewable(environment("s1", "Good"));

        expect(opened).toBe(true);
        expect(service.viewerHosts.items.some(h => h.viewables.has(viewable))).toBe(true);
    });

    it("reports failure instead of throwing when the viewable cannot be created", async () => {
        // A script whose kind has no editor-language mapping throws here. It must not be allowed to
        // propagate, otherwise the caller abandons setting up the rest of the work area.
        const {service} = setup();
        await service.initialize();
        service.createScriptViewable = () => {
            throw new Error("Unhandled script kind: Expression");
        };

        await expect(service.openScriptViewable(environment("s1", "Bad"))).resolves.toBe(false);
    });

    it("logs the script that could not be opened", async () => {
        const {service, logger} = setup();
        await service.initialize();
        service.createScriptViewable = () => {
            throw new Error("Unhandled script kind: Expression");
        };

        await service.openScriptViewable(environment("s1", "Bad"));

        expect(logger.errors).toHaveLength(1);
        expect(String(logger.errors[0][0])).toContain("s1");
    });
});

describe("WorkAreaService.ensureAtLeastOneScript", () => {
    it("creates another script when EnvironmentsRemovedEvent fires and the session is empty", async () => {
        const {service, scriptService, eventBus, session} = setup({initialEnvironments: []});
        await service.initialize();
        // initialize() already creates one (session started empty); we're testing the
        // subsequent event-driven path.
        const baseline = scriptService.createCalls;

        session.environments = [];
        eventBus.fire(EnvironmentsRemovedEvent, new EnvironmentsRemovedEvent());
        await Promise.resolve();

        expect(scriptService.createCalls).toBe(baseline + 1);
    });
});

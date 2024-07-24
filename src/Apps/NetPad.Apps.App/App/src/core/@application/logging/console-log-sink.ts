import {ConsoleSink as AureliaConsoleSink, IPlatform} from "aurelia";
import {ILogEvent} from "@aurelia/kernel";
import {LogConfig} from "./log-config";

/**
 * Logs events to the console.
 */
export class ConsoleLogSink extends AureliaConsoleSink {
    private readonly baseHandleEvent: (event: ILogEvent) => void;
    public override readonly handleEvent: (event: ILogEvent) => void;

    constructor(@IPlatform p: IPlatform, logConfig: LogConfig) {
        super(p);

        // Capture the handleEvent method on the base class then override
        // that base method so that all calls to it get re-routed through
        // our handler below.
        this.baseHandleEvent = this.handleEvent;

        this.handleEvent = (event) => {
            logConfig.applyRules(event);
            this.baseHandleEvent(event);
        };
    }
}

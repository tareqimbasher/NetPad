import {ConsoleSink as AureliaConsoleSink, IPlatform} from "aurelia";
import {ILogEvent} from "@aurelia/kernel";
import {LogConfig} from "./log-config";

export class ConsoleLogSink extends AureliaConsoleSink {
    private readonly baseHandleEvent: (event: ILogEvent) => void;
    public override readonly handleEvent: (event: ILogEvent) => void;

    constructor(@IPlatform p: IPlatform, logConfig: LogConfig) {
        super(p);
        this.baseHandleEvent = this.handleEvent;

        this.handleEvent = (event) => {
            logConfig.applyRules(event);
            this.baseHandleEvent(event);
        };
    }
}

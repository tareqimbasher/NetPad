import {ISink, ILogEvent, DefaultLogEvent} from "@aurelia/kernel";
import {IAppService, LogLevel, RemoteLogMessage} from "@domain";
import {BufferedQueue} from "@common";
import {Env} from "@application/env";

export class RemoteLogSink implements ISink {
    public readonly handleEvent: (event: ILogEvent) => void;
    private queue: BufferedQueue<RemoteLogMessage>;

    constructor(@IAppService appService: IAppService) {

        this.queue = new BufferedQueue<RemoteLogMessage>({
            flushOnSize: 10,
            flushOnInterval: 10 * 1000,
            onFlush: async (items: RemoteLogMessage[]) => {
                await appService.sendRemoteLog(Env.isRunningInElectron() ? "ElectronApp" : "WebApp", items);
            }
        })

        this.handleEvent = (event) => {
            const now = new Date;

            const msg = event.toString();
            const details: string[] = [];

            if (!!event.optionalParams && event.optionalParams.length > 0) {
                for (const optionalParam of event.optionalParams) {
                    if (typeof optionalParam === "string")
                        details.push(optionalParam);
                    else
                        details.push(JSON.stringify(optionalParam));
                }
            }

            const remoteLogEvent = new RemoteLogMessage({
                logger: this.getLoggerName(event),
                logLevel: this.getLogLevel(event.severity),
                message: msg,
                optionalParams: details,
                date: now
            });

            this.queue.add(remoteLogEvent);
        };
    }

    private getLoggerName(event: ILogEvent) {
        const info = event as DefaultLogEvent;
        if (!info.scope || info.scope.length === 0) return "";
        return info.scope.join(".");
    }

    private getLogLevel(severity: number): LogLevel {
        switch (severity) {
            case 0:
                return "Error";
            case 1:
                return "Debug";
            case 2:
                return "Information";
            case 3:
                return "Warning";
            case 4:
                return "Error";
            case 5:
                return "Critical";
            case 6:
                return "None";
            default:
                return "None";
        }
    }
}

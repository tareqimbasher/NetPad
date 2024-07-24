import {DefaultLogEvent, ILogEvent, ISink} from "@aurelia/kernel";
import {Env, IAppService, LogLevel, RemoteLogMessage} from "@application";
import {BufferedQueue} from "@common";
import {LogConfig} from "./log-config";

/**
 * Sends log events to the backend application.
 */
export class RemoteLogSink implements ISink {
    public readonly handleEvent: (event: ILogEvent) => void;
    private queue: BufferedQueue<RemoteLogMessage>;

    constructor(@IAppService appService: IAppService, logConfig: LogConfig) {

        this.queue = new BufferedQueue<RemoteLogMessage>({
            flushOnSize: 10,
            flushOnInterval: 10 * 1000,
            onFlush: async (items: RemoteLogMessage[]) => {
                await appService.sendRemoteLog(Env.isRunningInElectron() ? "ElectronApp" : "WebApp", items);
            }
        });

        this.handleEvent = (event) => {
            const now = new Date();

            logConfig.applyRules(event);

            if (event.severity === 6) return;

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
            default:
                return "None";
        }
    }
}

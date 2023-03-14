import {IContainer, IRegistry, IResolver, LoggerConfiguration, LogLevel, Registration} from "aurelia";
import {Class, DefaultLogEvent, IConsoleLike, ILogConfig, ILogEvent, ISink} from "@aurelia/kernel";

export class LogConfig {
    private readonly rules: { loggerRegex: RegExp, logLevel: LogLevel }[];

    constructor(rules: { loggerRegex: RegExp, logLevel: LogLevel }[] | undefined) {
        this.rules = rules || [];
    }

    public applyRules(event: ILogEvent): void {
        const info = event as DefaultLogEvent;
        if (info.scope && info.scope.length) {
            const logger = info.scope.join('.');

            for (const rule of this.rules) {
                if (rule.loggerRegex.test(logger))
                    // eslint-disable-next-line @typescript-eslint/no-explicit-any
                    (event as any).severity = rule.logLevel;
            }
        }
    }

    public static register = (config: Partial<ILoggingConfigurationOptions>) => new LogConfigRegistration(config);
}

class LogConfigRegistration implements IRegistry {
    constructor(private config: Partial<ILoggingConfigurationOptions>) {
    }

    register(container: IContainer, ...params: unknown[]): void | IResolver | IContainer {
        container.register(LoggerConfiguration.create(this.config));
        container.register(Registration.instance(LogConfig, new LogConfig(this.config.rules)));
    }
}

interface ILoggingConfigurationOptions extends ILogConfig {
    $console: IConsoleLike;

    /**
     * Logging sinks.
     */
    sinks: (Class<ISink> | IRegistry)[];

    /**
     * Rules that configure log levels for loggers by matching the logger name.
     * If multiple rules match the logger name, the last rule
     * that matches will be respected.
     */
    rules: { loggerRegex: RegExp, logLevel: LogLevel }[];
}

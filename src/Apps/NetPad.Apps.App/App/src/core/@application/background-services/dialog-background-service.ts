import {ILogger} from "aurelia";
import {WithDisposables} from "@common";
import {
    AlertUserAboutMissingAppDependencies,
    AlertUserCommand,
    ConfirmWithUserCommand,
    IBackgroundService,
    IEventBus,
    IIpcGateway,
    PromptUserCommand,
    YesNoCancel
} from "@application";
import {AppDependenciesCheckDialog} from "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {answerBackend} from "./answer-backend";

/**
 * Listens and reacts to dialog-related commands pushed by the Backend application.
 */
export class DialogBackgroundService extends WithDisposables implements IBackgroundService {
    private logger: ILogger;

    constructor(@IEventBus private readonly eventBus: IEventBus,
                @IIpcGateway private readonly ipcGateway: IIpcGateway,
                private readonly dialogUtil: DialogUtil,
                @ILogger logger: ILogger
    ) {
        super();
        this.logger = logger.scopeTo(nameof(DialogBackgroundService));
    }

    public start(): Promise<void> {
        this.addDisposable(
            this.eventBus.subscribeToServer(AlertUserCommand, async msg => await this.alert(msg))
        );

        this.addDisposable(
            this.eventBus.subscribeToServer(ConfirmWithUserCommand, async msg => await this.confirm(msg))
        );

        this.addDisposable(
            this.eventBus.subscribeToServer(PromptUserCommand, async msg => await this.prompt(msg))
        );

        this.addDisposable(
            this.eventBus.subscribeToServer(AlertUserAboutMissingAppDependencies, async msg => await this.alertUserAboutMissingAppDependencies(msg))
        );

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.dispose();
    }

    private async alert(command: AlertUserCommand) {
        await this.dialogUtil.alert({
            message: command.message,
        });
    }

    private async confirm(command: ConfirmWithUserCommand) {
        await this.answer<YesNoCancel>(command.requestId, "No", async () => {
            const result = await this.dialogUtil.ask({
                message: command.message,
                buttons: [
                    {text: "Cancel"},
                    {text: "OK", value: "Yes", isPrimary: true},
                ]
            });

            return result.status === "ok" && result.value === "Yes" ? "Yes" : "No";
        });
    }

    private async prompt(command: PromptUserCommand) {
        await this.answer<string | null>(command.requestId, null, async () => {
            const result = await this.dialogUtil.prompt({
                message: command.message,
                defaultValue: command.prefillValue,
            });

            return (result.status === "ok" && result.value as string | undefined) || null;
        });
    }

    private answer<T>(requestId: string, unanswered: T, ask: () => Promise<T>) {
        return answerBackend(this.ipcGateway, this.logger, requestId, unanswered, ask);
    }

    private async alertUserAboutMissingAppDependencies(command: AlertUserAboutMissingAppDependencies) {
        await this.dialogUtil.open(AppDependenciesCheckDialog, command.dependencyCheckResult);
    }
}

import {
    AlertUserAboutMissingAppDependencies,
    AlertUserCommand,
    ConfirmWithUserCommand,
    IEventBus,
    IIpcGateway,
    PromptUserCommand,
    YesNoCancel
} from "@domain";
import {IBackgroundService} from "@common";
import {IDialogService, IDisposable, ILogger} from "aurelia";
import {DialogBase} from "@application/dialogs/dialog-base";
import {
    AppDependenciesCheckDialog
} from "@application/dialogs/app-dependencies-check-dialog/app-dependencies-check-dialog";

export class DialogBackgroundService implements IBackgroundService {
    private disposables: IDisposable[] = [];
    private logger: ILogger;

    constructor(@IEventBus private readonly eventBus: IEventBus,
                @IIpcGateway private readonly ipcGateway: IIpcGateway,
                @IDialogService private readonly dialogService,
                @ILogger logger: ILogger
    ) {
        this.logger = logger.scopeTo(nameof(DialogBackgroundService));
    }

    public start(): Promise<void> {
        this.disposables.push(
            this.eventBus.subscribeToServer(AlertUserCommand, async msg => await this.alert(msg))
        );

        this.disposables.push(
            this.eventBus.subscribeToServer(ConfirmWithUserCommand, async msg => await this.confirm(msg))
        );

        this.disposables.push(
            this.eventBus.subscribeToServer(PromptUserCommand, async msg => await this.prompt(msg))
        );

        this.disposables.push(
            this.eventBus.subscribeToServer(AlertUserAboutMissingAppDependencies, async msg => await this.alertUserAboutMissingAppDependencies(msg))
        );

        return Promise.resolve(undefined);
    }

    public stop(): void {
        for (const disposable of this.disposables) {
            try {
                disposable.dispose();
            } catch (ex) {
                this.logger.error("Error stopping service", ex);
            }
        }
    }

    private async alert(command: AlertUserCommand) {
        alert(command.message);
    }

    private async confirm(command: ConfirmWithUserCommand) {
        const ync: YesNoCancel = confirm(command.message) ? "Yes" : "No";

        await this.ipcGateway.send("Respond", command.id, ync);
    }

    private async prompt(command: PromptUserCommand) {
        const newName = prompt(command.message, command.prefillValue);

        await this.ipcGateway.send("Respond", command.id, newName || null);
    }

    private async alertUserAboutMissingAppDependencies(command: AlertUserAboutMissingAppDependencies) {
        await DialogBase.toggle(this.dialogService, AppDependenciesCheckDialog, command.dependencyCheckResult);
    }
}

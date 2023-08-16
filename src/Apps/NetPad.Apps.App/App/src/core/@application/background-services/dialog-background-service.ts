import {ILogger} from "aurelia";
import {IDialogService} from "@aurelia/dialog";
import {
    AlertUserAboutMissingAppDependencies,
    AlertUserCommand,
    ConfirmWithUserCommand,
    IEventBus,
    IIpcGateway,
    PromptUserCommand,
    YesNoCancel
} from "@domain";
import {IBackgroundService, WithDisposables} from "@common";
import {DialogBase} from "@application/dialogs/dialog-base";
import {
    AppDependenciesCheckDialog
} from "@application/dialogs/app-dependencies-check-dialog/app-dependencies-check-dialog";

export class DialogBackgroundService extends WithDisposables implements IBackgroundService {
    private logger: ILogger;

    constructor(@IEventBus private readonly eventBus: IEventBus,
                @IIpcGateway private readonly ipcGateway: IIpcGateway,
                @IDialogService private readonly dialogService: IDialogService,
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

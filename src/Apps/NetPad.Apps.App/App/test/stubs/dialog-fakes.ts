import {DialogCloseResult} from "@aurelia/dialog";
import {IAskDialogModel} from "@application/dialogs/ask-dialog/ask-dialog";
import {IPromptDialogModel} from "@application/dialogs/prompt-dialog/prompt-dialog";

export class FakeEventBus {
    private readonly handlers = new Map<unknown, (msg: unknown) => unknown>();

    public subscribeToServer(messageType: unknown, handler: (msg: unknown) => unknown) {
        this.handlers.set(messageType, handler);
        return {dispose: () => this.handlers.delete(messageType)};
    }

    public async fire(messageType: unknown, msg: unknown) {
        await this.handlers.get(messageType)?.(msg);
    }
}

export class FakeIpcGateway {
    private readonly responses = new Map<string, unknown>();

    public send(_channel: unknown, requestId: string, response: unknown) {
        this.responses.set(requestId, response);
        return Promise.resolve();
    }

    public responseTo(requestId: string) {
        return this.responses.get(requestId);
    }
}

export class FakeDialogUtil {
    public nextResult: DialogCloseResult;
    /** Fails the way DialogUtil does when a dialog of the same kind is already open. */
    public cannotOpen = false;
    public asked?: IAskDialogModel;
    public prompted?: IPromptDialogModel;

    public ask(options: IAskDialogModel) {
        this.asked = options;
        return this.cannotOpen ? Promise.reject(new Error("Error opening ask")) : Promise.resolve(this.nextResult);
    }

    public prompt(options: IPromptDialogModel) {
        this.prompted = options;
        return this.cannotOpen ? Promise.reject(new Error("Error opening prompt")) : Promise.resolve(this.nextResult);
    }
}

export class FakeLogger {
    public readonly warnings: unknown[][] = [];

    public scopeTo() {
        return this;
    }

    public warn(...args: unknown[]) {
        this.warnings.push(args);
    }
}

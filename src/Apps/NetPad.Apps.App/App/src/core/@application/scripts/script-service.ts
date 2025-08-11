import {IHttpClient} from "@aurelia/fetch-client";
import {ApiException, IEventBus, IScriptService, Script, ScriptsApiClient} from "@application";
import {ScriptCodeUpdatingEvent} from "@application/scripts/script-code-updating-event";
import {ScriptCodeUpdatedEvent} from "@application/scripts/script-code-updated-event";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class ScriptService extends ScriptsApiClient implements IScriptService {

    constructor(
        @IEventBus private readonly eventBus: IEventBus,
        private readonly dialogUtil: DialogUtil,
        baseUrl?: string, @IHttpClient http?: IHttpClient) {
        super(baseUrl, http);
    }

    public override async updateCode(id: string, code: string, signal?: AbortSignal | undefined): Promise<void> {
        this.eventBus.publish(new ScriptCodeUpdatingEvent(id, code));

        try {
            return await super.updateCode(id, code, signal);
        } finally {
            this.eventBus.publish(new ScriptCodeUpdatedEvent(id, code));
        }
    }

    public async openRenamePrompt(script: Script): Promise<void> {
        const prompt = await this.dialogUtil.prompt({
            message: "New name:",
            defaultValue: script.name,
            placeholder: "Script name"
        });

        const newName = (prompt.value as string | undefined)?.trim();
        if (!newName || newName === script.name) {
            return;
        }

        super.rename(script.id, newName)
            .catch(err => {
                if (err instanceof ApiException) {
                    alert(err.errorResponse?.message || "An error occurred during rename.");
                }
            });
    }
}

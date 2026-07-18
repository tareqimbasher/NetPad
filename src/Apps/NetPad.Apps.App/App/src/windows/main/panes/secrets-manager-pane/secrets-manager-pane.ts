import {INotificationService, IUserSecretService, Pane, PaneIds, UserSecretListingDto} from "@application";
import {resolve} from "aurelia";
import {DialogUtil} from "@application/dialogs/dialog-util";

interface NewSecret {
    key?: string;
    value: string;
}

interface EditSecret {
    readonly key: string;
    value: string;
}

export class SecretsManagerPane extends Pane {
    public secrets: UserSecretListingDto[] = [];

    public secretInEdit?: EditSecret;
    public newSecret?: NewSecret;

    private readonly userSecretService: IUserSecretService = resolve(IUserSecretService);
    private readonly dialogUtil: DialogUtil = resolve(DialogUtil);
    private readonly notificationService: INotificationService = resolve(INotificationService);

    constructor() {
        super(PaneIds.secretsManager, "Secrets", "secrets-manager-icon");
    }

    public attached() {
        this.refresh();
    }

    public async refresh() {
        this.userSecretService.getAll()
            .then(secrets => {
                this.secrets = secrets;
            })
            .catch(error => {
                this.secrets = [];
                this.logger.error("Error loading user secrets", error);
            });
    }

    public async copyKeyToClipboard(secret: UserSecretListingDto) {
        await navigator.clipboard.writeText(secret.key);
        this.notificationService.notify("Copied to clipboard", "Transient");
    }

    public async copyUnprotectedValueToClipboard(secret: UserSecretListingDto) {
        let value = await this.userSecretService.getUnprotectedValue(secret.key);
        value ??= "";
        await navigator.clipboard.writeText(value);
        this.notificationService.notify("Copied to clipboard", "Transient");
    }

    public async startNewSecret() {
        if (this.newSecret) return;
        this.resetEdits();
        this.newSecret = {value: ""};
    }

    public async selectForEdit(secret: UserSecretListingDto) {
        this.resetEdits();
        const value = await this.userSecretService.getUnprotectedValue(secret.key);
        this.secretInEdit = {
            key: secret.key,
            value: value
        };
    }

    public async finishEdit() {
        try {
            if (!this.secretInEdit && !this.newSecret) {
                return;
            }

            const key = (this.newSecret ? this.newSecret.key : this.secretInEdit?.key)?.trim();
            const value = (this.newSecret ? this.newSecret.value : this.secretInEdit?.value) ?? "";

            if (!key) {
                await this.dialogUtil.alert({
                    title: "Save Secret",
                    message: "Key cannot be empty."
                });
                return;
            }

            if (this.newSecret && this.secrets.some(secret => secret.key === key)) {
                await this.dialogUtil.alert({
                    title: "Save Secret",
                    message: `Another secret already has the key: '${key}'`
                });
                return;
            }

            const saved = await this.userSecretService.save(key, value);
            if (this.newSecret) {
                this.secrets.push(saved);
            } else {
                const existing = this.secrets.find(secret => secret.key === key);
                if (existing) {
                    existing.init(saved);
                }
            }

            this.resetEdits();
        } catch (e) {
            this.logger.error("Error saving secret value", e);
            await this.dialogUtil.alert({title: "Save Secret", message: `There was an error saving value.`});
        }
    }

    private resetEdits() {
        this.secretInEdit = undefined;
        this.newSecret = undefined;
    }

    public async deleteSecret(secret: UserSecretListingDto) {
        const result = await this.dialogUtil.ask({
            title: "Delete Secret",
            message: `Are you sure you want to delete <code class="fw-bold">${secret.key}</code>? This is not reversible!`,
            buttons: [{text: "Delete", cssClasses: "btn-danger"}, {text: "Cancel"}],
        });

        if (result.value !== "Delete") {
            return;
        }

        await this.userSecretService.delete(secret.key);
        const ix = this.secrets.findIndex(s => s === secret);
        if (ix >= 0) {
            this.secrets.splice(ix, 1);
        }

        this.notificationService.notify(`Deleted secret '${secret.key}'`, "Notice");
    }
}

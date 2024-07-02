import {watch} from "@aurelia/runtime-html";
import {DatabaseConnection, IDataConnectionService} from "@application";
import {IDataConnectionViewComponent} from "./idata-connection-view-component";

export class AuthComponent implements IDataConnectionViewComponent {
    public authType: "none" | "password" | "userAndPassword";
    private unprotectedPassword?: string;

    constructor(
        private readonly connection: DatabaseConnection,
        private readonly dataConnectionService: IDataConnectionService,
        private readonly passwordOnly = false) {

        if (passwordOnly && !!connection.password) {
            this.authType = "password";
        } else if (!passwordOnly && (!!this.connection.userId || !!this.connection.password))
            this.authType = "userAndPassword";
        else
            this.authType = "none";
    }

    public get validationError(): string | undefined {
        if (this.authType === "userAndPassword" && !this.connection.userId) {
            return "The User is required.";
        }

        if (this.authType !== "none" && !this.connection.password)
            return "The Password is required.";

        return undefined;
    }

    @watch<AuthComponent>(vm => vm.authType)
    private async authTypeChanged() {
        if (this.authType === "none") {
            this.connection.userId = undefined;
            this.connection.password = undefined;
            this.unprotectedPassword = undefined;
        } else if (this.authType === "password") {
            this.connection.userId = undefined;
        }
    }

    private async unprotectedPasswordEntered() {
        const dbConnection = this.connection as DatabaseConnection;
        if (!this.unprotectedPassword) dbConnection.password = this.unprotectedPassword;
        else {
            dbConnection.password = await this.dataConnectionService.protectPassword(this.unprotectedPassword) || undefined;
        }
    }
}

import {IDataConnectionService} from "@application";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";

/**
 * Services commonly used in data connection window and its components.
 */
export interface CommonServices {
    dataConnectionService: IDataConnectionService;
    nativeDialogService: INativeDialogService;
}

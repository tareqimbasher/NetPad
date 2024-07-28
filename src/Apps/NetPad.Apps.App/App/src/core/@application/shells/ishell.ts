import {IAurelia} from "aurelia";

/**
 * Represents the shell that the app is hosted within. This is not a handle to the actual shell
 * but a logic representation of it.
 */
export interface IShell {
    configure(appBuilder: IAurelia): void;
}

import {WindowId} from "@application/windows/window-id";
import {ShellType} from "@application/windows/shell-type";

export class WindowParams {
    public window: WindowId;
    public shell: ShellType;

    constructor(private readonly queryParams: URLSearchParams) {
        const win = queryParams.get("win") as WindowId | undefined;
        if (!win) {
            this.window = WindowId.Main;
        } else if (Object.values(WindowId).includes(win)) {
            this.window = win as WindowId;
        } else {
            throw new Error(`Unrecognized 'win' query parameter: ${win}`);
        }

        const shell = queryParams.get("shell") as ShellType | undefined;
        if (!shell) {
            this.shell = ShellType.Browser;
        } else if (Object.values(ShellType).includes(shell)) {
            this.shell = shell as ShellType;
        } else {
            throw new Error(`Unrecognized 'shell' query parameter: ${shell}`);
        }
    }

    public get(key: string): string | null {
        return this.queryParams.get(key);
    }

    public toString(): string {
        return this.queryParams.toString();
    }
}

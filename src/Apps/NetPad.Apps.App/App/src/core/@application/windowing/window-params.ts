import {WindowId} from "@application/windowing/window-id";
import {ShellType} from "@application/windowing/shell-type";

export class WindowParams {
    public static window: WindowId;
    public static shell: ShellType;
    public static token: string | null;
    private static queryParams: URLSearchParams;

    public static init(queryParams: URLSearchParams) {
        this.queryParams = queryParams;

        const win = queryParams.get("win") as WindowId | undefined;
        if (!win) {
            this.window = WindowId.Main;
        } else if (Object.values(WindowId).includes(win)) {
            this.window = win as WindowId;
        } else {
            throw new Error(`Unrecognized 'win' query parameter: ${win}`);
        }

        this.token = queryParams.get("token");

        const shell = queryParams.get("shell") as ShellType | undefined;
        if (!shell) {
            this.shell = ShellType.Browser;
        } else if (Object.values(ShellType).includes(shell)) {
            this.shell = shell as ShellType;
        } else {
            throw new Error(`Unrecognized 'shell' query parameter: ${shell}`);
        }
    }

    public static get(key: string): string | null {
        return this.queryParams.get(key);
    }

    /**
     * Reloads this window with some of its parameters changed. A null value drops the parameter.
     */
    public static reloadWith(changes: Record<string, string | null>) {
        const params = new URLSearchParams(this.queryParams);

        for (const [key, value] of Object.entries(changes)) {
            if (value === null) params.delete(key);
            else params.set(key, value);
        }

        window.location.search = params.toString();
    }

    public static toString(): string {
        return this.queryParams.toString();
    }
}

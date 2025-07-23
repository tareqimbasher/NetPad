import {DI} from "aurelia";

export const INativeDialogService = DI.createInterface<INativeDialogService>();

/**
 * Provides methods to open native dialogs.
 */
export interface INativeDialogService {
    /**
     * Opens a file (or directory) selector dialog.
     * @param options Options for the file selector dialog.
     */
    showFileSelectorDialog(options: FileSelectorDialogOptions): Promise<string[] | null>;
}

export interface FileSelectorDialogOptions {
    /** The title of the dialog window. */
    title?: string;

    /**
     * The filters of the dialog.
     * @example
     * ```typescript
     * {
     *   filters: [
     *     { name: 'Images', extensions: ['jpg', 'png', 'gif'] },
     *     { name: 'Movies', extensions: ['mkv', 'avi', 'mp4'] },
     *     { name: 'Custom File Type', extensions: ['as'] },
     *     { name: 'All Files', extensions: ['*'] }
     *   ]
     * }
     * ```
     */
    filters?: FileSelectorFilter[];

    /** Initial directory or file path. */
    defaultPath?: string;

    /** Whether the dialog allows multiple selection or not. */
    multiple?: boolean;

    /** Whether the dialog is a directory selection or not. */
    directory?: boolean;
}

export interface FileSelectorFilter {
    /** Filter name. */
    name: string;
    /**
     * Extensions to filter, without a `.` prefix.
     * @example
     * ```typescript
     * extensions: ['svg', 'png']
     * ```
     */
    extensions: string[];
}

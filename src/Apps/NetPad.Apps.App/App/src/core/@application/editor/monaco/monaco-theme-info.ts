import * as monaco from "monaco-editor";
import {ResolvedTheme} from "@application/themes/app-theme";

export class MonacoThemeInfo {
    /**
     * @param id The theme's identifier, as stored in user settings.
     * @param name The theme's display name.
     * @param data The theme's data, when it is already known.
     * @param url The name of the file to load the theme's data from, for themes that ship with the
     * `monaco-themes` library.
     * @param build Builds the theme's data on demand, from the app theme in force at that moment.
     */
    constructor(
        public readonly id: string,
        public readonly name: string,
        public data?: monaco.editor.IStandaloneThemeData,
        public readonly url?: string,
        public readonly build?: (appTheme: ResolvedTheme) => monaco.editor.IStandaloneThemeData) {
    }

    public get loaded() {
        return !this.build && this.data !== undefined && this.data !== null;
    }
}

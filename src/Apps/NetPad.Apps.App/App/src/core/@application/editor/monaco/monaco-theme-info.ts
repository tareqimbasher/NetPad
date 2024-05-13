import * as monaco from "monaco-editor";

export class MonacoThemeInfo {
    constructor(
        public readonly id: string,
        public readonly name: string,
        public data?: monaco.editor.IStandaloneThemeData,
        public readonly url?: string) {
    }

    public get loaded() {
        return this.data !== undefined && this.data !== null;
    }
}

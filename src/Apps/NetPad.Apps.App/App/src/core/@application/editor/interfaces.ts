import {DI} from "aurelia";
import {languages} from "monaco-editor";

export interface ICompletionItemProvider extends languages.CompletionItemProvider {}
export const ICompletionItemProvider = DI.createInterface<languages.CompletionItemProvider>();

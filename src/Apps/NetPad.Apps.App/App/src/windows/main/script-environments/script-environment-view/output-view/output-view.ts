import {bindable} from "aurelia";
import {ScriptEnvironment} from "@domain";

export class OutputView {
    @bindable public environment: ScriptEnvironment;
    @bindable public onCloseRequested: () => void;
    public view: string = "Results";
}

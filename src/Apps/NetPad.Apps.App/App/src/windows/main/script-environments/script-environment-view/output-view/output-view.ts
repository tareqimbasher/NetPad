import {bindable} from "aurelia";
import {ScriptEnvironment} from "@domain";

export class OutputView {
    @bindable public environment: ScriptEnvironment;
    @bindable public close: () => void;
    public view = "Results";
}

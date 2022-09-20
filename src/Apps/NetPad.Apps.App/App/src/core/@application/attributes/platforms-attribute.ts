import {bindable} from "aurelia";
import {Env} from "@application/env";

export class PlatformsCustomAttribute {
    @bindable supportedPlatforms?: string;

    constructor(private readonly element: Element) {
    }

    public bound() {
        if (!this.supportedPlatforms) return;

        const runningInElectron = Env.isRunningInElectron();

        if (runningInElectron && this.supportedPlatforms.indexOf("Electron") < 0)
            this.element.remove();

        if (!runningInElectron && this.supportedPlatforms.indexOf("Web") < 0)
            this.element.remove();
    }
}

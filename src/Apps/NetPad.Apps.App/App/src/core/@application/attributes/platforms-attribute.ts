import {bindable} from "aurelia";
import {System} from "@common";

export class PlatformsCustomAttribute {
    @bindable supportedPlatforms?: "Electron" | "Web";

    constructor(private readonly element: Element) {
    }

    public bound() {
        if (!this.supportedPlatforms) return;

        const runningInElectron = System.isRunningInElectron();

        if (runningInElectron && this.supportedPlatforms.indexOf("Electron") < 0)
            this.element.remove();
        else if (this.supportedPlatforms.indexOf("Web") < 0)
            this.element.remove();
    }
}

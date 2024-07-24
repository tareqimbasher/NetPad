import {bindable, ILogger} from "aurelia";
import {Tooltip} from "bootstrap";
import {ViewModelBase} from "@application";

/**
 * Adds a Bootstrap tooltip to an element. Usage:
 *
 * <div tooltip="Tooltip text"></div> or
 * <div tooltip.bind="person.name"></div>
 * */
export class TooltipCustomAttribute extends ViewModelBase {
    @bindable text?: string;

    constructor(private readonly element: Element, @ILogger logger: ILogger) {
        super(logger);
    }

    public async attached() {
        await this.getOrInitTooltip();
    }

    private async getOrInitTooltip() {
        let tooltip = Tooltip.getInstance(this.element);

        if (!tooltip && this.text) {
            tooltip = new Tooltip(this.element, {
                title: this.text,
                animation: true
            });

            this.addDisposable(tooltip);
        }

        return tooltip;
    }

    private async textChanged(newValue: string) {
        (await this.getOrInitTooltip())?.setContent({ ".tooltip-inner": newValue });
    }
}

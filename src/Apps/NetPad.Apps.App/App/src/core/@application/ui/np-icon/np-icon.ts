import {bindable, customElement, INode, resolve} from "aurelia";
import {IconName, iconSvgMarkup} from "./icons";

/**
 * Renders one of the app's glyphs as inline SVG at `1em`, so `font-size` sizes it and `color` tints it.
 * The name is mirrored to `data-icon`, so one glyph can be styled with `np-icon[data-icon="run"]`.
 */
@customElement({name: "np-icon", template: null})
export class NpIcon {
    @bindable({primary: true}) public name: IconName | string;

    private readonly element = resolve(INode) as HTMLElement;

    public binding() {
        this.render();
    }

    public nameChanged() {
        this.render();
    }

    private render() {
        this.element.dataset["icon"] = this.name ?? "";
        this.element.innerHTML = iconSvgMarkup(this.name);
    }
}

import {bindable, customElement, INode, resolve} from "aurelia";
import {IconName, iconSvgMarkup} from "./icons";

/**
 * Renders one of the app's glyphs as inline SVG.
 *
 * The glyph is drawn at `1em`, so a surface sizes its icons with `font-size` and tints them with
 * `color`, exactly as it would text. The name is also mirrored onto a `data-icon` attribute so a
 * surface can style one specific glyph (`np-icon[data-icon="run"]`).
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

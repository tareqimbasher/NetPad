import {bindable, BindingMode} from "aurelia";
import {IconName} from "../np-icon/icons";

/**
 * One page listed by a {@link NpRailNav}.
 */
export interface RailNavPage {
    route: string;
    text: string;
    icon: IconName;
    /** A number shown at the end of the item. */
    count?: number;
}

/**
 * The list of pages down the side of a window that is split into pages.
 */
export class NpRailNav {
    @bindable public pages: readonly RailNavPage[] = [];
    @bindable({mode: BindingMode.twoWay}) public selected?: RailNavPage;
}

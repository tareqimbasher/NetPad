import {bindable, BindingMode} from "aurelia";

/**
 * One choice offered by a {@link NpSegmentedControl}.
 */
export interface SegmentedOption<T = unknown> {
    /** The value assigned to the control when this choice is picked. */
    value: T;
    label: string;
    /** A number shown after the label. */
    count?: number;
    /** Shows the choice in the warning color, for a choice that calls for care. */
    warn?: boolean;
    title?: string;
}

/**
 * A row of buttons that picks exactly one of a few choices.
 */
export class NpSegmentedControl {
    @bindable public options: readonly SegmentedOption[] = [];
    @bindable({mode: BindingMode.twoWay}) public value: unknown;
    @bindable public disabled?: boolean;
}

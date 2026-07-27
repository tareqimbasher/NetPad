/**
 * One option offered by a {@link NpValueSelect}.
 */
export interface ValueSelectOption {
    /** The value assigned to the select when this option is picked. */
    value: unknown;
    /** The name of the choice. */
    label: string;
    /** A secondary fact about the option, shown right-aligned in mono. */
    detail?: string;
    /** The name of a glyph to lead the row with. */
    icon?: string;
}

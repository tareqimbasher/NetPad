export class YesNoValueConverter {
    /**
     * Converts a true boolean value or "true" string value to a "Yes" string, and
     * converts a false boolean value or "false" string value to a "No" string.
     * String comparison ignores casing.
     * @param value The value to convert.
     */
    public toView(value?: boolean | string): "Yes" | "No" | null {
        const isString = typeof value === "string";

        if (value === true || (isString && value.toLowerCase() === "true"))
            return "Yes";

        if (value === false || (isString && value.toLowerCase() === "false"))
            return "No";

        return null;
    }
}

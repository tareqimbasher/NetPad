export class YesNoValueConverter {
    public toView(value?: boolean | string) {
        if (value === undefined || value === null)
            return "";

        const isString = typeof value === "string";

        if (value === true || (isString && value.toLowerCase() === "true"))
            return "Yes";

        if (value === false || (isString && value.toLowerCase() === "false"))
            return "No";
    }
}

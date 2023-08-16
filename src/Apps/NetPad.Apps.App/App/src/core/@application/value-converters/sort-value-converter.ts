export class SortValueConverter {
    /**
     * Sorts an array by a property.
     * @param array The array to sort
     * @param propertyName The name of the property to sort by.
     * @param direction The sort direction.
     * @param comparison Type of comparison.
     */
    public toView<T>(
        array: T[],
        propertyName: string,
        direction: "asc" | "desc" = "asc",
        comparison: "ordinal" | "ordinalIgnoreCase" | "date" | "numeral" | "number" = "ordinal"): T[] {

        if (!Array.isArray(array))
            return [];

        if (array == null || !array.length)
            return array;

        if (!comparison) comparison = "ordinal";

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const self = this as any;
        const directionFactor = direction === "asc" ? 1 : -1,
            comparer = self[comparison + "Comparison"];

        if (!comparer)
            return array;

        if (!propertyName)
            return array.sort((a, b) => comparer(a, b) * directionFactor);

        return array.sort((a, b) => comparer(a[propertyName as keyof typeof a], b[propertyName as keyof typeof b]) * directionFactor);
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    private ordinalIgnoreCaseComparison(a: any, b: any) {
        if ((a === null || a === undefined) && (b === null || b === undefined)) return 0;
        if (a === null || a === undefined) return -1;
        if (b === null || b === undefined) return 1;
        a = a.toString().toLowerCase();
        b = b.toString().toLowerCase();
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    private ordinalComparison(a: any, b: any) {
        if ((a === null || a === undefined) && (b === null || b === undefined)) return 0;
        if (a === null || a === undefined) return -1;
        if (b === null || b === undefined) return 1;
        a = a.toString();
        b = b.toString();
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }

    private dateComparison(a: number | string | Date, b: number | string | Date) {
        a = new Date(a);
        b = new Date(b);
        if ((a === null || a === undefined) && (b === null || b === undefined)) return 0;
        if (a === null || a === undefined) return -1;
        if (b === null || b === undefined) return 1;
        return a.getTime() > b.getTime() ? 1 : -1;
    }


    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    private numeralComparison(a: any, b: any) {
        if ((a === null || a === undefined || isNaN(a)) && (b === null || b === undefined || isNaN(b)))
            return 0;
        if (a === null || a === undefined || isNaN(a)) return -1;
        if (b === null || b === undefined || isNaN(b)) return 1;
        a = Number(a);
        b = Number(b);
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }

    private numberComparison(a: number | null | undefined, b: number | null | undefined) {
        if ((a === null || a === undefined) && (b === null || b === undefined)) return 0;
        if (a === null || a === undefined) return -1;
        if (b === null || b === undefined) return 1;
        return a - b;
    }
}

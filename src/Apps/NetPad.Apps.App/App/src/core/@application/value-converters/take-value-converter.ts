export class TakeValueConverter {
    /**
     * Slices an array to starting at the beginning and taking the number of items specified.
     * @param array The array to slice.
     * @param take How many items to take.
     */
    public toView<T>(array: T[], take: number): T[] {
        if (!Array.isArray(array) || !take)
            return [];

        if (array.length === 0 || take > array.length)
            return array;

        return array.slice(0, take);
    }
}

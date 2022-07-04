export class TakeValueConverter {
    public toView<T>(array: T[], take: number): T[] {
        if (!array || !array.length || !take) return [];

        if (take > array.length) take = array.length;

        return array.slice(0, take);
    }
}

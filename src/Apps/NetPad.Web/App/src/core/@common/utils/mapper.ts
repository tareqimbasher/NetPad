/**
 * @deprecated This is only used in the deprecated Mapper
 */
interface Constructable<T = {}> {
    // eslint-disable-next-line @typescript-eslint/prefer-function-type
    new(...args: any[]): T;
}

/**
 * @deprecated This is no longer used, but kept in case it becomes useful.
 */
export class Mapper
{
    public static toNew<TModel>(modelType: Constructable, source: any): TModel {
        const model = new modelType() as TModel;
        Object.assign(model, source);
        return model;
    }

    public static toInstance<TModel>(instance: TModel, source: any): TModel {
        Object.assign(instance, source);
        return instance;
    }
}

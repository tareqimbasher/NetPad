import {Constructable} from "aurelia";

/**
 * @deprecated This is no longer used, but kept in case it becomes useful.
 */
export class Mapper
{
    public static toNew<TModel>(modelType: Constructable, source: unknown): TModel {
        const model = new modelType() as TModel;
        Object.assign(model, source);
        return model;
    }

    public static toInstance<TModel>(instance: TModel, source: unknown): TModel {
        Object.assign(instance, source);
        return instance;
    }
}

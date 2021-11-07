import {Constructable} from "aurelia";

// export interface Constructable<T = {}> = {
//     // eslint-disable-next-line @typescript-eslint/prefer-function-type
//     new(...args: any[]): T;
// };

export class Mapper
{
    public static toModel<TModel>(modelType: Constructable, source: any): TModel {
        const model = new modelType() as TModel;
        Object.assign(model, source);
        return model;
    }
}
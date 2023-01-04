import {Aurelia, ILogger} from "aurelia";

export interface IWindowBootstrapperConstructor {
    new(logger: ILogger): IWindowBootstrapper;
}

export interface IWindowBootstrapper {
    registerServices(app: Aurelia): void;

    getEntry(): unknown;
}

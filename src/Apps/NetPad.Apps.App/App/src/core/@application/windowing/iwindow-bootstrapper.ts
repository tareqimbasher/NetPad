import {Aurelia, Constructable, ILogger} from "aurelia";

export interface IWindowBootstrapperConstructor {
    new(logger: ILogger): IWindowBootstrapper;
}

export interface IWindowBootstrapper {
    /**
     * Gets the entry view-model for the window.
     */
    getEntry(): Constructable;

    /**
     * Registers services specific to the window.
     */
    registerServices(app: Aurelia): void;
}

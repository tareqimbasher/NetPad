import {IAurelia} from "aurelia";

export interface IPlatform {
    configure(appBuilder: IAurelia): void;
}

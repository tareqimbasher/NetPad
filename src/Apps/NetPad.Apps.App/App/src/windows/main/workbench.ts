import {IWorkAreaService} from "./work-area/work-area-service";
import {IMainMenuService} from "./titlebar/main-menu/main-menu-service";
import {IStatusbarService} from "./statusbar/statusbar-service";
import {ITextEditorService} from "@application/editor/text-editor-service";

export class Workbench {
    constructor(
        @IWorkAreaService readonly workAreaService: IWorkAreaService,
        @IMainMenuService readonly mainMenuService: IMainMenuService,
        @IStatusbarService readonly statusbarService: IStatusbarService,
        @ITextEditorService readonly textEditorService: ITextEditorService,
    ) {
    }
}

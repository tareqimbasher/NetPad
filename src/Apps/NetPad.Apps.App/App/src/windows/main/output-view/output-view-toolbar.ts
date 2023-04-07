import {bindable} from "aurelia";

export class ToolbarOptions {
    constructor(public readonly tabs: IToolbarTab[], public readonly actions: IToolbarAction[]) {
    }
}

export interface IToolbarTab {
    text: string;
    icon?: string;
    active?: boolean;
    show?: () => boolean;
    clicked?: (event: MouseEvent) => Promise<void>;
    [otherProp: string | number | symbol]: unknown;
}

export interface IToolbarAction {
    icon: string;
    label?: string;
    active?: boolean;
    show?: () => boolean;
    clicked?: (event: MouseEvent) => Promise<void>;
    [otherProp: string | number | symbol]: unknown;
}

export class OutputViewToolbar {
    @bindable public view = "Results";
    @bindable options: ToolbarOptions;

    public async tabClicked(tab: IToolbarTab, event: MouseEvent) {
        tab.active = true;

        for (const otherTab of this.options.tabs.filter(t => t !== tab)) {
            otherTab.active = false;
        }

        if (!tab.clicked) return;
        await tab.clicked(event);
    }

    public async actionClicked(action: IToolbarAction, event: MouseEvent) {
        if (!action.clicked) return;
        await action.clicked(event);
    }
}



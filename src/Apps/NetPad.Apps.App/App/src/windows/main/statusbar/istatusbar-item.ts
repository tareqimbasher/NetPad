export interface IStatusbarItem {
    text: string,
    icon?: string,
    hoverText?: string,
    click?: () => Promise<void>,
}

export interface IStatusbarItemNew {
    readonly position: "left" | "right";
}

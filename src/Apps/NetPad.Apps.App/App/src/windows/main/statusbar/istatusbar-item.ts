export interface IStatusbarItem {
    text: string,
    icon?: string,
    hoverText?: string,
    click?: () => Promise<void>,
}

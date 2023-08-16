import {DndType} from "./dnd-type";

/**
 * Represents a drag-and-dropped object.
 */
export abstract class DragAndDropBase {
    protected constructor(public readonly type: DndType) {
    }

    public transferDataToEvent(event: DragEvent): void {
        event.dataTransfer!.setData("text/html", JSON.stringify(this));
    }

    public static getFromEventData(event: DragEvent): DragAndDropBase | null {
        try {
            const json = event.dataTransfer!.getData("text/html");

            const dnd = JSON.parse(json) as DragAndDropBase;

            return dnd.type === undefined ? null : dnd;

        } catch (ex) {
            // failed to parse value
        }

        return null;
    }
}

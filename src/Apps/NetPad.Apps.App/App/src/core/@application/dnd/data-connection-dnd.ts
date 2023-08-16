import {DndType} from "./dnd-type";
import {DragAndDropBase} from "./drag-and-drop-base";

/**
 * Represents a drag-and-dropped DataConnection.
 */
export class DataConnectionDnd extends DragAndDropBase {
    constructor(public readonly dataConnectionId: string) {
        super(DndType.DataConnection);
    }
}

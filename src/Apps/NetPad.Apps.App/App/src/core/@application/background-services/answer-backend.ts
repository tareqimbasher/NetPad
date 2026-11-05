import {ILogger} from "aurelia";
import {ChannelInfo} from "../events/channel-info";
import {IIpcGateway} from "../events/iipc-gateway";

/**
 * Asks the user something the backend is waiting on and sends the answer, or sends `unanswered` (what Cancel
 * would give) when the dialog can't open because one of its kind is showing, since the backend has no timeout.
 */
export async function answerBackend<T>(
    ipcGateway: IIpcGateway,
    logger: ILogger,
    requestId: string,
    unanswered: T,
    ask: () => Promise<T>
): Promise<void> {
    let answer: T;

    try {
        answer = await ask();
    } catch (err) {
        logger.warn("Couldn't ask the user, so answered as if they cancelled", err);
        answer = unanswered;
    }

    await ipcGateway.send(new ChannelInfo("Respond"), requestId, answer);
}

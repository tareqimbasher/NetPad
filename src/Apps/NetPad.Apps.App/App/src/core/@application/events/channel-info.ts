import {Constructable} from "aurelia";

/**
 * Info identifying a particular channel and the type of messages it transmits.
 */
export class ChannelInfo {
    /**
     * The name of the channel. This is typically used as the main identifier of a channel.
     */
    public readonly name: string;

    /**
     * The type of message this channel transmits. when a message is received on this channel, a new instance
     * of this type will be instantiated and auto-filled using the received message.
     *
     * The benefit over returning the raw received message is that since we instantiate an object of the specified
     * type, the object returned will still retain any members that the serialization process will typically skip
     * (ex: methods, getter/setter props...etc.) Specifying this property allows on to use those members on the
     * messages received from this channel.
     *
     * Since parameters are not passed in the construction of the object, if the type has a parameterized constructor,
     * the param values will all be "undefined".
     */
    public readonly messageType?: Constructable;

    /**
     * Instantiates a new ChannelInfo.
     * @param channelMessageTypeOrName The name of the channel or the type of message the channel
     * transmits. See messageType doc for more details.
     */
    constructor(channelMessageTypeOrName: Constructable | string) {
        if (typeof channelMessageTypeOrName === "string") {
            this.name = channelMessageTypeOrName;
        }
        else {
            this.messageType = channelMessageTypeOrName;
            this.name = this.messageType.name;
        }
    }
}

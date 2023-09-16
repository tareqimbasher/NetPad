// @ts-ignore
/// <reference path="../App/node_modules/electron/electron.d.ts" />

import { Socket } from 'socket.io';

export class Connector {
    constructor(private socket: Socket, protected app: Electron.App) { }

    protected on(key: string, javaScriptCode: Function): void {
        this.socket.on(key, (...args: any[]) => {
            const id: string = args.pop();

            try {
                javaScriptCode(...args, (data) => {
                    if (data) {
                        this.socket.emit(`${key}Complete${id}`, data);
                    }
                });
            } catch (error) {
                this.socket.emit(`${key}Error${id}`, `Host Hook Exception`, error);
            }
        });
    }
}

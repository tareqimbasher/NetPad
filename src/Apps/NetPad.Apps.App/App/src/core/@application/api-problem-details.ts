/* tslint:disable */
/* eslint-disable */

export class ProblemDetails implements IProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    traceId?: string | undefined;

    [key: string]: any;

    constructor(data?: IProblemDetails) {
        if (data) {
            for (const property in data) {
                if (data.hasOwnProperty(property)) {
                    (<any>this)[property] = (<any>data)[property];
                }
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            for (const property in _data) {
                if (_data.hasOwnProperty(property)) {
                    this[property] = _data[property];
                }
            }
            this.type = _data["type"];
            this.title = _data["title"];
            this.status = _data["status"];
            this.detail = _data["detail"];
            this.instance = _data["instance"];
            this.traceId = _data["traceId"];
        }
    }

    static fromJS(data: unknown): ProblemDetails {
        data = typeof data === 'object' ? data : {};
        const result = new ProblemDetails();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        for (const property in this) {
            if (this.hasOwnProperty(property))
                data[property] = this[property];
        }
        data["type"] = this.type;
        data["title"] = this.title;
        data["status"] = this.status;
        data["detail"] = this.detail;
        data["instance"] = this.instance;
        return data;
    }

    clone(): ProblemDetails {
        const json = this.toJSON();
        const result = new ProblemDetails();
        result.init(json);
        return result;
    }
}

export interface IProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    traceId?: string | undefined;

    [key: string]: any;
}

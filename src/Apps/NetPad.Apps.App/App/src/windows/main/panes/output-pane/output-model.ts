import {DisposableCollection} from "@common";
import {ScriptEnvironment, Settings} from "@application";
import {DumpContainer} from "./components/dump-container";
import {SqlViewDumpContainer} from "./components/sql-view/sql-view-dump-container";

export interface IUserInputRequest {
    commandId: string;
    userInput?: string | undefined;
}

/**
 * Represents the output of a single script.
 */
export class OutputModel {
    public inputRequest?: IUserInputRequest | null;
    private disposables = new DisposableCollection();

    public constructor(public environment: ScriptEnvironment, settings: Settings) {
        this.resultsDumpContainer = new DumpContainer(settings);
        this.sqlDumpContainer = new SqlViewDumpContainer(settings);
    }

    public resultsDumpContainer: DumpContainer;
    public sqlDumpContainer: SqlViewDumpContainer;

    public destroy() {
        this.disposables.dispose();
        this.resultsDumpContainer.dispose();
        this.sqlDumpContainer.dispose();
        this.inputRequest = null;
    }

    public toDto() {
        return {
            scriptId: this.environment.script.id,
            inputRequest: this.inputRequest,
            resultsDumpContainer: {
                html: this.resultsDumpContainer.getHtml(),
                lastOutputOrder: this.resultsDumpContainer.lastOutputOrder,
                scrollOnOutput: this.resultsDumpContainer.scrollOnOutput,
                textWrap: this.resultsDumpContainer.textWrap
            },
            sqlDumpContainer: {
                html: this.sqlDumpContainer.getHtml(),
                lastOutputOrder: this.sqlDumpContainer.lastOutputOrder,
                scrollOnOutput: this.sqlDumpContainer.scrollOnOutput,
                textWrap: this.sqlDumpContainer.textWrap
            }
        };
    }
}

/**
 * A serializable representation of the IOutputModel.
 */
export interface IOutputModelDto {
    scriptId: string;
    inputRequest?: IUserInputRequest | null;
    resultsDumpContainer: IDumpContainerDto;
    sqlDumpContainer: IDumpContainerDto;
}

/**
 * A serializable representation of the DumpContainer.
 */
export interface IDumpContainerDto {
    html: string;
    lastOutputOrder: number;
    scrollOnOutput: boolean;
    textWrap: boolean;
}

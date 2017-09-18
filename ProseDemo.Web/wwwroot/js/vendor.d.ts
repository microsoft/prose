interface IBootstrapTable {
    defaults: any;
    columns: { [index: number]: any };
}

interface JQuery {
    bootstrapTable(options?: any): IBootstrapTable;
    bootstrapTable(options: Object | string, ...params) : IBootstrapTable;
}

interface ILearnResponse {
    description: string;
    programXML: string;
    programHumanReadable: string;
    programPython: string;
}

interface ITTextLearnResponse extends ILearnResponse {
    output: any[];
}

interface ISTextLearnResponse extends ILearnResponse {
    output: string[][];
}
interface JQuery {
    bootstrapTable(options: Object | string, ...params);
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
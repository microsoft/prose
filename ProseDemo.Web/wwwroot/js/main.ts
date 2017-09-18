function deterministicallySample(arr: any[], count: number): any[] {
    if (count >= arr.length) return arr;
    const step = Math.floor(arr.length / count);
    return arr.filter((_, i) => i % step === 0);
}

function insertColumn($data, name, index, editable?) {
    let columns: any[] = $data.bootstrapTable("getOptions").columns[0];
    columns.splice(index, 0, { field: name, title: name, editable: editable });
    let data: any[] = $data.bootstrapTable("getData");
    data.forEach(r => r[name] = "");
    $data.bootstrapTable("refreshOptions", { columns: columns, data: data });
    return data;
}

function onSplit() {
    const source = $(this).attr("data-id");
    const columns: any[] = $("#data").bootstrapTable("getOptions").columns[0];
    const sourceIndex = _.findIndex(columns, c => c.field === source);
    $(".progress-ring").removeClass("hidden");
    $.ajax({
        method: "POST",
        url: "/Home/SplitText",
        data: sourceIndex,
        contentType: "application/json; charset=utf-8",
        success: (response: ISTextLearnResponse, status, xhr) => {
            $(".progress-ring").addClass("hidden");
            const newColumns = _.range(response.output[0].length).map(i => {
                return { field: "Column" + i, title: "Column" + i };
            });
            const newData = response.output.map(r => {
                var result = {};
                r.forEach((c, i) => result["Column" + i] = c);
                return result;
            });
            $("#data").bootstrapTable("refreshOptions", { columns: newColumns, data: newData });

            $("#tabDescription").html(`<pre class="input-code">${_.escape(response.description)}</pre>`);
            $("#tabHR").html(`<pre class="input-code">${_.escape(response.programHumanReadable)}</pre>`);
            $("#tabPython").html(`<pre class="input-code">${_.escape(response.programPython)}</pre>`);
            $("#tabXml").html(`<pre class="input-code">${_.escape(response.programXML)}</pre>`);

            $("#navProgram").removeClass("hidden");
            $("#btnProgram").click(() => {
                $("#dialogProgram").modal("show");
            });
        },
        error: (xhr, status, error) => {
            $("#alertContent").html(`${status}: ${error}`);
            $("#alertError").removeClass("hidden");
            $(".progress-ring").addClass("hidden");
        }
    });
}

function onDeriveViaFormula() {
    const $dialogFormula = $("#dialogDerivedFormula");
    const $inputFormula = $("#inputDerivedFormula");
    const $inputDerivedColumnName = $("#dialogDerivedFormula input.derived-column-name");
    $("#btnDerivedFormulaOk").click(() => {
        const formula = $inputFormula.val();
        const dest = $inputDerivedColumnName.val();
        $dialogFormula.modal("hide");

        const $data = $("#data");
        let data = insertColumn($data, dest, 0);
        const f = new Function("row", `return ${formula}`);
        data.forEach(r => r[dest] = f(r));
        $data.bootstrapTable("load", data);
    });
    $dialogFormula.on("shown.bs.modal", () => $inputDerivedColumnName.focus())
        .modal("show");
}

function onSelectDeriveSource(e) {
    const source = $(this).attr("data-id");
    const $navComplete = $("#navComplete").removeClass("hidden").addClass("disabled");

    const $dialogDerivedColumnName = $("#dialogDerivedColumnName");
    const $inputDerivedColumnName = $("#dialogDerivedColumnName input.derived-column-name");
    $("#btnDerivedColumnNameOk").click(() => {
        const dest = $inputDerivedColumnName.val();
        $dialogDerivedColumnName.modal("hide");

        const $data = $("#data");
        let columns: any[] = $data.bootstrapTable("getOptions").columns[0];
        const destIndex = _.findIndex(columns, c => c.field === dest);
        if (destIndex >= 0) return;
        const sourceIndex = _.findIndex(columns, c => c.field === source);
        let data = insertColumn($data, dest, sourceIndex + 1, true);

        let examples = {};
        $data.on("editable-save.bs.table", (e, field: string, row: any[], oldValue, $el) => {
            if (field !== dest || row[field] === oldValue) return;
            const rowIndex = $el.parents("tr[data-index]").data("index");
            examples[rowIndex] = row[field];
            $navComplete.removeClass("disabled");
        });
        $("#btnComplete").click(() => {
            $("#navQuestions").addClass("hidden");
            $(".progress-ring").removeClass("hidden");
            const request = Object.getOwnPropertyNames(examples).map(k => {
                return { row: parseInt(k), output: examples[k] }
            });
            $.ajax({
                method: "POST",
                url: "/Home/TextTransformation",
                data: JSON.stringify({ examples: request, sourceColumn: sourceIndex }),
                contentType: "application/json; charset=utf-8",
                success: (response: ITTextLearnResponse, status, xhr) => {
                    data = ($data.bootstrapTable("getData")) as any;
                    _.zip(data, response.output).forEach(r => r[0][dest] = r[1]);
                    $data.bootstrapTable("load", data);
                    $(".progress-ring").addClass("hidden");

                    $("#tabDescription").html(`<pre class="input-code">${_.escape(response.description)}</pre>`);
                    $("#tabHR").html(`<pre class="input-code">${_.escape(response.programHumanReadable)}</pre>`);
                    $("#tabPython").html(`<pre class="input-code">${_.escape(response.programPython)}</pre>`);
                    $("#tabXml").html(`<pre class="input-code">${_.escape(response.programXML)}</pre>`);

                    if (response.significantInputs && response.significantInputs.length > 0) {
                        const $ulQuestions = $("#navQuestions ul");
                        $ulQuestions.empty().append(response.significantInputs.map(i => {
                            const input = data[i][source];
                            return `<li data-id="${i}"><a href="#">${input}</a></li>`;
                        }));
                        $ulQuestions.children().click(function() {
                            const rowIndex = parseInt($(this).attr("data-id"));
                            const pageSize = $data.bootstrapTable("getOptions").pageSize;
                            const page = 1 + Math.floor(rowIndex / pageSize);
                            $data.bootstrapTable("selectPage", page);
                            const $tr = $(`tr[data-index='${rowIndex}']`);
                            $tr.addClass("danger");
                            $(window).scrollTo($tr, { duration: 200 });
                        });
                        $("#navQuestions").removeClass("hidden");
                    }

                    $("#navProgram").removeClass("hidden");
                    $("#btnProgram").click(() => {
                        $("#dialogProgram").modal("show");
                    });
                },
                error: (xhr, status, error) => {
                    $("#alertContent").html(`${status}: ${error}`);
                    $("#alertError").removeClass("hidden");
                    $(".progress-ring").addClass("hidden");
                }
            });
        });
    });
    $dialogDerivedColumnName.on("shown.bs.modal", () => $inputDerivedColumnName.focus())
        .modal("show");
}

function complete(results: PapaParse.ParseResult, dataLimit: number) {
    const columns = results.meta.fields.map(s => { return { field: s, title: s } });
    const data = deterministicallySample(results.data, dataLimit);
    const $data = $("#data");

    function redrawDropdowns() {
        $(".dropup, .dropdown, .columns, .keep-open").removeClass("btn-group");
        $(".dropup button, .dropdown button, .columns button").removeClass("btn-default").addClass("btn-dropdown");
    }

    $data.on("page-change.bs.table", redrawDropdowns)
        .on("reset-view.bs.table", redrawDropdowns)
        .bootstrapTable({ columns: columns, data: data });
    redrawDropdowns();

    const $ddDerive = $("#ddDerive");
    $ddDerive.nextAll().remove();
    $ddDerive.after(columns.map(s => `<li data-id="${s.field}"><a href="#">From "${s.title}"</a></li>`));
    $ddDerive.nextAll().click(onSelectDeriveSource);
    $ddDerive.prev().click(onDeriveViaFormula);
    $("#navDerive").removeClass("hidden");

    const $ulSplit = $("#navSplit ul");
    $ulSplit.empty().append(columns.map(s => `<li data-id="${s.field}"><a href="#">${s.title}</a></li>`));
    $ulSplit.children().click(onSplit);
    //$("#navSplit").removeClass("hidden");
}

Dropzone.options.dataDropzone = {
    init: function() {
        this.on("success", (file: File, response) => {
            $("#divDataLoad").addClass("hidden");
        });
        this.on("sending", (file: File, xhr, formData: FormData) => {
            $("#navDerive").addClass("hidden");
            $("#navComplete").addClass("hidden");
            const dataLimit = $("#inputDataLimit").val();
            formData.append("dataLimit", dataLimit);
            Papa.parse(file, { worker: true, header: true, complete: r => complete(r, dataLimit) });
        });
    }
};

$(() => {
    ($.fn.bootstrapTable as any).defaults.icons.export = "glyph glyph-download";
    $("#btnUpload").click(() => {
        $("#divDataLoad").toggleClass("hidden");
    });
});
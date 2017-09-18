function deterministicallySample(arr, count) {
    if (count >= arr.length)
        return arr;
    var step = Math.floor(arr.length / count);
    return arr.filter(function (_, i) { return i % step === 0; });
}
function insertColumn($data, name, index, editable) {
    var columns = $data.bootstrapTable("getOptions").columns[0];
    columns.splice(index, 0, { field: name, title: name, editable: editable });
    var data = $data.bootstrapTable("getData");
    data.forEach(function (r) { return r[name] = ""; });
    $data.bootstrapTable("refreshOptions", { columns: columns, data: data });
    return data;
}
function onSplit() {
    var source = $(this).attr("data-id");
    var columns = $("#data").bootstrapTable("getOptions").columns[0];
    var sourceIndex = _.findIndex(columns, function (c) { return c.field === source; });
    $(".progress-ring").removeClass("hidden");
    $.ajax({
        method: "POST",
        url: "/Home/SplitText",
        data: sourceIndex,
        contentType: "application/json; charset=utf-8",
        success: function (response, status, xhr) {
            $(".progress-ring").addClass("hidden");
            var newColumns = _.range(response.output[0].length).map(function (i) {
                return { field: "Column" + i, title: "Column" + i };
            });
            var newData = response.output.map(function (r) {
                var result = {};
                r.forEach(function (c, i) { return result["Column" + i] = c; });
                return result;
            });
            $("#data").bootstrapTable("refreshOptions", { columns: newColumns, data: newData });
            $("#tabDescription").html("<pre class=\"input-code\">" + _.escape(response.description) + "</pre>");
            $("#tabHR").html("<pre class=\"input-code\">" + _.escape(response.programHumanReadable) + "</pre>");
            $("#tabPython").html("<pre class=\"input-code\">" + _.escape(response.programPython) + "</pre>");
            $("#tabXml").html("<pre class=\"input-code\">" + _.escape(response.programXML) + "</pre>");
            $("#navProgram").removeClass("hidden");
            $("#btnProgram").click(function () {
                $("#dialogProgram").modal("show");
            });
        },
        error: function (xhr, status, error) {
            $("#alertContent").html(status + ": " + error);
            $("#alertError").removeClass("hidden");
            $(".progress-ring").addClass("hidden");
        }
    });
}
function onDeriveViaFormula() {
    var $dialogFormula = $("#dialogDerivedFormula");
    var $inputFormula = $("#inputDerivedFormula");
    var $inputDerivedColumnName = $("#dialogDerivedFormula input.derived-column-name");
    $("#btnDerivedFormulaOk").click(function () {
        var formula = $inputFormula.val();
        var dest = $inputDerivedColumnName.val();
        $dialogFormula.modal("hide");
        var $data = $("#data");
        var data = insertColumn($data, dest, 0);
        var f = new Function("row", "return " + formula);
        data.forEach(function (r) { return r[dest] = f(r); });
        $data.bootstrapTable("load", data);
    });
    $dialogFormula.on("shown.bs.modal", function () { return $inputDerivedColumnName.focus(); })
        .modal("show");
}
function onSelectDeriveSource(e) {
    var source = $(this).attr("data-id");
    var $navComplete = $("#navComplete").removeClass("hidden").addClass("disabled");
    var $dialogDerivedColumnName = $("#dialogDerivedColumnName");
    var $inputDerivedColumnName = $("#dialogDerivedColumnName input.derived-column-name");
    $("#btnDerivedColumnNameOk").click(function () {
        var dest = $inputDerivedColumnName.val();
        $dialogDerivedColumnName.modal("hide");
        var $data = $("#data");
        var columns = $data.bootstrapTable("getOptions").columns[0];
        var destIndex = _.findIndex(columns, function (c) { return c.field === dest; });
        if (destIndex >= 0)
            return;
        var sourceIndex = _.findIndex(columns, function (c) { return c.field === source; });
        var data = insertColumn($data, dest, sourceIndex + 1, true);
        var examples = {};
        $data.on("editable-save.bs.table", function (e, field, row, oldValue, $el) {
            if (field !== dest || row[field] === oldValue)
                return;
            var rowIndex = $el.parents("tr[data-index]").data("index");
            examples[rowIndex] = row[field];
            $navComplete.removeClass("disabled");
        });
        $("#btnComplete").click(function () {
            $("#navQuestions").addClass("hidden");
            $(".progress-ring").removeClass("hidden");
            var request = Object.getOwnPropertyNames(examples).map(function (k) {
                return { row: parseInt(k), output: examples[k] };
            });
            $.ajax({
                method: "POST",
                url: "/Home/TextTransformation",
                data: JSON.stringify({ examples: request, sourceColumn: sourceIndex }),
                contentType: "application/json; charset=utf-8",
                success: function (response, status, xhr) {
                    data = ($data.bootstrapTable("getData"));
                    _.zip(data, response.output).forEach(function (r) { return r[0][dest] = r[1]; });
                    $data.bootstrapTable("load", data);
                    $(".progress-ring").addClass("hidden");
                    $("#tabDescription").html("<pre class=\"input-code\">" + _.escape(response.description) + "</pre>");
                    $("#tabHR").html("<pre class=\"input-code\">" + _.escape(response.programHumanReadable) + "</pre>");
                    $("#tabPython").html("<pre class=\"input-code\">" + _.escape(response.programPython) + "</pre>");
                    $("#tabXml").html("<pre class=\"input-code\">" + _.escape(response.programXML) + "</pre>");
                    if (response.significantInputs && response.significantInputs.length > 0) {
                        var $ulQuestions = $("#navQuestions ul");
                        $ulQuestions.empty().append(response.significantInputs.map(function (i) {
                            var input = data[i][source];
                            return "<li data-id=\"" + i + "\"><a href=\"#\">" + input + "</a></li>";
                        }));
                        $ulQuestions.children().click(function () {
                            var rowIndex = parseInt($(this).attr("data-id"));
                            var pageSize = $data.bootstrapTable("getOptions").pageSize;
                            var page = 1 + Math.floor(rowIndex / pageSize);
                            $data.bootstrapTable("selectPage", page);
                            var $tr = $("tr[data-index='" + rowIndex + "']");
                            $tr.addClass("danger");
                            $(window).scrollTo($tr, { duration: 200 });
                        });
                        $("#navQuestions").removeClass("hidden");
                    }
                    $("#navProgram").removeClass("hidden");
                    $("#btnProgram").click(function () {
                        $("#dialogProgram").modal("show");
                    });
                },
                error: function (xhr, status, error) {
                    $("#alertContent").html(status + ": " + error);
                    $("#alertError").removeClass("hidden");
                    $(".progress-ring").addClass("hidden");
                }
            });
        });
    });
    $dialogDerivedColumnName.on("shown.bs.modal", function () { return $inputDerivedColumnName.focus(); })
        .modal("show");
}
function complete(results, dataLimit) {
    var columns = results.meta.fields.map(function (s) { return { field: s, title: s }; });
    var data = deterministicallySample(results.data, dataLimit);
    var $data = $("#data");
    function redrawDropdowns() {
        $(".dropup, .dropdown, .columns, .keep-open").removeClass("btn-group");
        $(".dropup button, .dropdown button, .columns button").removeClass("btn-default").addClass("btn-dropdown");
    }
    $data.on("page-change.bs.table", redrawDropdowns)
        .on("reset-view.bs.table", redrawDropdowns)
        .bootstrapTable({ columns: columns, data: data });
    redrawDropdowns();
    var $ddDerive = $("#ddDerive");
    $ddDerive.nextAll().remove();
    $ddDerive.after(columns.map(function (s) { return "<li data-id=\"" + s.field + "\"><a href=\"#\">From \"" + s.title + "\"</a></li>"; }));
    $ddDerive.nextAll().click(onSelectDeriveSource);
    $ddDerive.prev().click(onDeriveViaFormula);
    $("#navDerive").removeClass("hidden");
    var $ulSplit = $("#navSplit ul");
    $ulSplit.empty().append(columns.map(function (s) { return "<li data-id=\"" + s.field + "\"><a href=\"#\">" + s.title + "</a></li>"; }));
    $ulSplit.children().click(onSplit);
    //$("#navSplit").removeClass("hidden");
}
Dropzone.options.dataDropzone = {
    init: function () {
        this.on("success", function (file, response) {
            $("#divDataLoad").addClass("hidden");
        });
        this.on("sending", function (file, xhr, formData) {
            $("#navDerive").addClass("hidden");
            $("#navComplete").addClass("hidden");
            var dataLimit = $("#inputDataLimit").val();
            formData.append("dataLimit", dataLimit);
            Papa.parse(file, { worker: true, header: true, complete: function (r) { return complete(r, dataLimit); } });
        });
    }
};
$(function () {
    $.fn.bootstrapTable.defaults.icons.export = "glyph glyph-download";
    $("#btnUpload").click(function () {
        $("#divDataLoad").toggleClass("hidden");
    });
});
//# sourceMappingURL=main.js.map
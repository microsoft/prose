function setupPrism() {
    Prism.languages.xml = Prism.languages.markup;
    $("code.language-xml").each(function () {
        Prism.highlightElement($(this)[0]);
    });

    Prism.languages["dsl"] = Prism.languages.extend("clike", {
        'keyword': /\b(reference|@start|@input|feature|language|@values|@feature|@complete|semantics|learners|@id|let|in|using|Tuple|bool|byte|char|string|int|uint|sbyte|long|ulong|decimal|float|double|short)\b/,
        'property': /@\b\w+/
    });
    $("code.language-dsl").each(function () {
        Prism.highlightElement($(this)[0]);
    });
    Prism.highlightAll();
    $('pre[class*="language-dsl"]').each(function() {
        $(this).attr("data-language", "DSL");
    });
}

function setupTables() {
    $(".content").find("table").addClass("pure-table pure-table-horizontal mx-auto");
}

$(() => {
    setupPrism();
    setupTables();
});

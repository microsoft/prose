/// <reference path="../Scripts/typings/jquery/jquery.d.ts" />
/// <reference path="./prism.d.ts" />
function jqEscape(id) {
    return id.replace(/(:|\.|\[|\]|,)/g, "\\$1");
}
function isVisible($node) {
    return $node.is(":visible") &&
        $node.attr("visibility") != "hidden" &&
        $node.attr("opacity") != "0";
}
function setupPrism() {
    Prism.languages.xml = Prism.languages.markup;
    $(".language-xml").each(function () {
        Prism.highlightElement($(this)[0]);
    });
    Prism.languages["dsl"] = Prism.languages.extend("clike", {
        'keyword': /\b(reference|@start|@input|feature|property|language|@values|@feature|@property|@complete|semantics|learners|@witnesses|let|in|using|bool|byte|char|string|int|uint|sbyte|long|ulong|decimal|float|double|short)\b/,
        'property': /@\b\w+/
    });
    $(".language-text").each(function () {
        $(this).addClass("language-dsl");
    });
    $(".language-dsl").each(function () {
        Prism.highlightElement($(this)[0]);
    });
}
function setupAnchors() {
    var $root = $("html, body");
    $("a[href*=#]:not([data-toggle])").click(function () {
        $root.animate({ scrollTop: $(jqEscape($(this).attr("href"))).offset().top }, "fast");
        return false;
    });
    $("main h1, main h2, main h3").each(function () {
        var id = $(this).attr("id");
        $(this).wrapInner("<a class=\"header-link\" href=\"#" + id + "\"></a>");
        var $upArrow = $('<span class="anchor-highlight" style="display: none;"><a href="#top"><i class="fa fa-level-up" /></a></span>');
        $(this).hover(function () { return $upArrow.delay(100).fadeIn(200); }, function () { return $upArrow.fadeOut(200); });
        $(this).append($upArrow);
    });
    $("a[href=#top]").click(function () {
        $root.animate({ scrollTop: 0 }, "fast");
        return false;
    });
}
function fixTOC() {
    var $toc = $("#toc");
    if ($toc.length == 0)
        return;
    $toc.find("ul > li > ul > li > ul > li > ul").hide();
    function displayNode(i, node) {
        var $node = $(node);
        if (!isVisible($node))
            return false;
        var childrenDisplayed = $node.children().map(displayNode).get();
        var display = childrenDisplayed.length == 0 || $.inArray(true, childrenDisplayed) >= 0;
        $node.toggle(display);
        return display;
    }
    $toc.find("nav > ul").each(displayNode);
}
window.onload = function () {
    $("#main_content_wrap").find("table").addClass("table table-hover table-bordered");
    $(".footnote-return").each(function () {
        $(this).prev().append($(this));
    });
    fixTOC();
    setupAnchors();
    setupPrism();
};
//# sourceMappingURL=main.js.map
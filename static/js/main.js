/// <reference path="./typings/jquery/jquery.d.ts" />
/// <reference path="./typings/prism.d.ts" />
define(["require", "exports"], function (require, exports) {
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
        $("code.language-xml").each(function () {
            Prism.highlightElement($(this)[0]);
        });
        Prism.languages["dsl"] = Prism.languages.extend("clike", {
            'keyword': /\b(reference|@start|@input|feature|property|language|@values|@feature|@property|@complete|semantics|learners|@witnesses|let|in|using|bool|byte|char|string|int|uint|sbyte|long|ulong|decimal|float|double|short)\b/,
            'property': /@\b\w+/
        });
        $("code.language-text").each(function () {
            $(this).addClass("language-dsl");
        });
        $("code.language-dsl").each(function () {
            Prism.highlightElement($(this)[0]);
        });
        Prism.highlightAll();
        $('pre[class*="language-dsl"]').each(function () {
            $(this).attr("data-language", "DSL");
        });
    }
    function setupAnchors() {
        var $root = $("html, body");
        $("a[href*=#]:not([data-toggle])").click(function () {
            $root.animate({ scrollTop: $(jqEscape($(this).attr("href"))).offset().top }, "fast");
            return false;
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
    function setupNav() {
        var dropdowns = $(".dropdown");
        dropdowns.on("shown.bs.dropdown", function (e) { return $(e.target).find("a > .fa").removeClass("fa-angle-right").addClass("fa-angle-down"); });
        dropdowns.on("hidden.bs.dropdown", function (e) { return $(e.target).find("a > .fa").removeClass("fa-angle-down").addClass("fa-angle-right"); });
        $("#leftNav").on("shown.bs.offcanvas", function (e) { return $("body").css("padding-right", 0); });
    }
    function main() {
        $("#main_content_wrap").find("table").addClass("table table-hover table-bordered");
        $(".footnote-return").each(function () {
            $(this).prev().append($(this));
        });
        fixTOC();
        setupNav();
        setupAnchors();
        setupPrism();
    }
    exports.main = main;
});
//# sourceMappingURL=main.js.map
///<reference path="../Scripts/typings/jquery/jquery.d.ts"/>
/// <reference path="../Scripts/typings/bootstrap/bootstrap.d.ts" />
$(function () {
    $("a.thumb").click(function (event) {
        event.preventDefault();
        var content = $(".modal-body");
        content.empty();
        content.html($(this).html());
        $(".modal-profile").modal({ show: true });
    });
});
//# sourceMappingURL=lightbox.js.map
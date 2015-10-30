///<reference path="../Scripts/typings/jquery/jquery.d.ts"/>
/// <reference path="../Scripts/typings/bootstrap/bootstrap.d.ts" />

$(() => {
    $("a.thumb").click(function(event) {
        event.preventDefault();
        const content = $(".modal-body");
        content.empty();
        content.html($(this).html());
        $(".modal-profile").modal({ show: true });
    });
});

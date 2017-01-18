$(function () {

    var $layout = $('#layout'),
        $menu = $('#menu'),
        $menuLink = $('#menuLink'),
        $content = $('#main');

    function toggleAll(e) {
        e.preventDefault();
        $layout.toggleClass('active');
        $menu.toggleClass('active');
        $menuLink.toggleClass('active');
    }

    $menuLink.click(function (e) {
        toggleAll(e);
    });

    $content.click(function (e) {
        if ($menu.hasClass('active')) {
            toggleAll(e);
        }
    });

});

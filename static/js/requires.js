require.config({
    paths: {
        app: 'main',

        jquery: "//code.jquery.com/jquery-1.11.3.min",
        jasny: "//cdnjs.cloudflare.com/ajax/libs/jasny-bootstrap/3.1.3/js/jasny-bootstrap.min",
        mathjax: "//cdn.mathjax.org/mathjax/latest/MathJax.js?config=TeX-AMS-MML_HTMLorMML&amp;delayStartupUntil=configured"
    },
    shim: {
        jquery: { exports: "$" },
        bootstrap: { deps: ["jquery"] },
        jasny: { deps: ["jquery"] },
        mathjax: {
            exports: "MathJax",
            init: function () {
                MathJax.Hub.Config({
                    tex2jax: {
                        inlineMath: [['$', '$']],
                        displayMath: [['$$', '$$'], ['\\[', '\\]']],
                        processEscapes: true,
                        processEnvironments: true,
                        skipTags: ['script', 'noscript', 'style', 'textarea', 'pre'],
                        TeX: {
                            equationNumbers: {autoNumber: "AMS"},
                            extensions: ["AMSmath.js", "AMSsymbols.js"]
                        }
                    },
                    TeX: {
                        Macros: {
                            llbracket: "[\\![",
                            rrbracket: "]\\!]"
                        }
                    }
                });
                MathJax.Hub.Queue(function () {
                    // Fix <code> tags after MathJax finishes running. This is a
                    // hack to overcome a shortcoming of Markdown. Discussion at
                    // https://github.com/mojombo/jekyll/issues/199
                    var all = MathJax.Hub.getAllJax(), i;
                    for (i = 0; i < all.length; i += 1) {
                        all[i].SourceElement().parentNode.className += ' has-jax';
                    }
                });
                MathJax.Hub.Startup.onload();
                return MathJax;
            }
        }
    },
});

require(['app', 'jquery', 'bootstrap', 'jasny', 'prism', 'mathjax'],
    function (app) {
        app.main();
    }
);

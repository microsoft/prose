function markLanguage(tag: string, name?: string) {
    $("pre").has(`code[class*="language-${tag}"]`).each(function () {
        $(this).attr("data-language", name || tag.toUpperCase());
    });
}

function setupPrism() {
    function getCodePres(...languages: string[]) {
        function preSelector(element: string, suffix: string, ...languages: string[]) {
            return languages.map(tag => `${element}[class*='language-${tag}']${suffix}`).join(", ");
        }

        if (languages.length == 0) languages = [''];
        return $("pre").has(preSelector("code", "", ...languages)).add(preSelector("div", " > pre", ...languages));
    }

    const termLanguages = ['console', 'cmd', 'term', 'terminal'];
    getCodePres(...termLanguages).addClass("command-line").not("pre[data-prompt]").attr("data-prompt", "$");
    getCodePres().not(".command-line").addClass("line-numbers");

    Prism.languages["xml"] = Prism.languages.markup;
    markLanguage("xml");

    Prism.languages["dsl"] = {
        'comment': {
            pattern: /(^|[^\\:])\/\/.*/,
            lookbehind: true
        },
        'string': {
            pattern: /(["'])(\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
            greedy: true
        },
        'keyword': /#?\b(reference|feature|language|semantics|learners|let|in|using)\b/,
        'type': [
            /(Tuple|HashSet|IEnumerable|I?List|I?Dictionary)<[?\w\[\]]+(,\s*[?\w\[\]]+)*>/,
            /\b(Regex|bool|byte|char|string|int|uint|sbyte|long|ulong|decimal|float|double|short)\b\??(\[])?/,
            /\b\w+(\.\w+)+\b/,
            {
                pattern: /(\[)\b\w+(\.\w+)*\b(?=])/,
                lookbehind: true
            }
        ],
        'annotation': /@\b\w+/,
        'function': /\w+(?=\()/,
        'number': /\b-?(?:0x[\da-f]+|\d*\.?\d+(?:e[+-]?\d+)?)\b/i,
        'punctuation': /:=|\||\\|:|=|=>|\(|\)|,/
    };
    markLanguage("dsl");

    Prism.languages["bnf"] = Prism.languages.extend("dsl", {
        'optional-start': /\{/,
        'optional-end': /}/,
        'placeholder': {
            pattern: /<[\w\d\s]+>/,
            greedy: true,
        },
        'string': {
            pattern: /(["'])(\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1/,
            greedy: true,
            inside: {
                'placeholder': /<[\w\d\s]+>/
            }
        }
    });
    markLanguage("bnf", "DSL");

    Prism.highlightAll();
    $(".toolbar").addClass("flex justify-between");
}

function setupTables() {
    $(".content").find("table").addClass("pure-table pure-table-horizontal mx-auto");
}

function setupHeaderLinks() {
    $("h1[id], h2[id], h3[id], h4[id], h5[id], h6[id]").each(function () {
        $(this).append($(`<a href="#${$(this).attr('id')}" class="header-link">#</a>`));
    });
}

function defer(method: () => void) {
    if (window.jQuery) {
        method();
    }
    else {
        setTimeout(() => defer(method), 50);
    }
}

defer(function () {
    $(() => {
        setupPrism();
        setupTables();
        setupHeaderLinks();
    });
});

if (window.Prism) {
    document.removeEventListener('DOMContentLoaded', Prism.highlightAll as any);
} else {
    window.Prism = {manual: true} as any;
}

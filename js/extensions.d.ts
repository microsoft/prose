interface Window {
    Prism?: Prism;
    jQuery?: JQueryStatic;
}

interface JQueryStatic {
    escapeSelector(selector: string): string;
}

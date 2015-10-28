declare type TokenDescription = RegExp | IPrismToken;
declare type TokenBag = { [token: string]: TokenDescription | TokenDescription[] };

interface IPrismToken {
    pattern: RegExp;
    inside?: TokenBag;
    lookbehind?: boolean;
    rest?: TokenBag;
    alias?: string | string[];
}

interface ILanguageList {
    markup: TokenBag;
    xml: TokenBag;
    extend(name: string, tokens: TokenBag): void;
}


interface Prism {
    languages: ILanguageList;
    highlightElement(element: HTMLElement, async?: boolean, callback?: (n: HTMLElement) => void): void;
}

declare var Prism: Prism;
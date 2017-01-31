declare type TokenDescription = RegExp | IPrismToken;
declare type TokenBag = {[token: string]: TokenDescription | TokenDescription[]};

interface IPrismToken {
    pattern: RegExp;
    inside?: TokenBag;
    lookbehind?: boolean;
    greedy?: boolean;
    rest?: TokenBag;
    alias?: string | string[];
}

interface ILanguageList {
    markup: TokenBag;
    extend(name: string, tokens: TokenBag): void;
}


interface Prism {
    manual?: boolean;
    languages: ILanguageList;
    highlightElement(element: HTMLElement, async?: boolean, callback?: (n: HTMLElement) => void): void;
    highlightAll(async?: boolean, callback?: (n: HTMLElement) => void): void;
}

declare const Prism: Prism;

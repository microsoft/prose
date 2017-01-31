interface JQuery {
    magnificPopup(options?: IMagnificPopupOptions);
}

interface IMagnificPopupOptions {
    type?: 'image' | 'inline' | 'iframe' | 'ajax';
    autoFocusLast?: boolean;
    removalDelay?: number;
    mainClass?: string;
}

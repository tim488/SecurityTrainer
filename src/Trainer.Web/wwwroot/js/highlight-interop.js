// Syntax highlighting with highlight.js (served from wwwroot/lib, works offline).
window.trainerHighlight = {
    // Returns highlighted HTML for a snippet. highlight.js escapes the code itself.
    code(code, language) {
        const lang = language === "razor" ? "xml" : language;
        return hljs.getLanguage(lang)
            ? hljs.highlight(code, { language: lang, ignoreIllegals: true }).value
            : hljs.highlightAuto(code).value;
    },
    // Highlights fenced code blocks rendered from Markdown inside an element.
    within(element) {
        element?.querySelectorAll("pre code:not([data-highlighted])").forEach(block => hljs.highlightElement(block));
    },
};

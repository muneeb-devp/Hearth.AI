export function scrollToBottom(el) {
    if (el) el.scrollTop = el.scrollHeight;
}

export function copyToClipboard(text) {
    return navigator.clipboard.writeText(text);
}

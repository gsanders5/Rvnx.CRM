document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.note-source').forEach(source => {
        const markdown = source.textContent;
        const render = source.nextElementSibling;
        if (typeof marked !== 'undefined' && typeof DOMPurify !== 'undefined') {
            const dirtyHtml = marked.parse(markdown);
            const cleanHtml = DOMPurify.sanitize(dirtyHtml);
            render.innerHTML = cleanHtml;
        } else {
            console.error('Marked or DOMPurify not loaded');
            render.textContent = markdown;
        }
    });
});

// Initialize Material Style components
document.addEventListener('DOMContentLoaded', () => {
    // Add ripple surface to buttons
    const buttons = document.querySelectorAll('.btn:not(.btn-link):not(.btn-close)');
    buttons.forEach(btn => {
        if (!btn.querySelector('.ripple-surface')) {
            const ripple = document.createElement('span');
            ripple.classList.add('ripple-surface');
            btn.appendChild(ripple);
        }
    });

    // Initialize Ripple
    const rippleSurface = Array.prototype.slice.call(document.querySelectorAll('.ripple-surface'));
    rippleSurface.map(s => {
        return new mdc.ripple.MDCRipple(s);
    });

    // Initialize Text fields
    var textFieldList = [].slice.call(document.querySelectorAll('.form-control'));
    textFieldList.map(function (textField) {
        return new materialstyle.TextField(textField);
    });

    // Initialize Select fields
    var selectList = [].slice.call(document.querySelectorAll('.form-select'));
    selectList.map(function (select) {
        return new materialstyle.SelectField(select);
    });
});

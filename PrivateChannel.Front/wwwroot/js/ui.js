window.scrollPosition = (elementId) => {
    var element = document.getElementById(elementId);
    return element.scrollTop;
}

window.scrollIntoView = (elementId) => {
    var element = document.getElementById(elementId);
    element.scrollIntoView(true);
}
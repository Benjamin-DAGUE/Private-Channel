window.blazorCulture = {
    get: function () {
        return window.localStorage['BlazorCulture'];
    },
    set: function (value) {
        window.localStorage['BlazorCulture'] = value;
    }
};
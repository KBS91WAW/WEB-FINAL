// Wraps localStorage in .then()-based promises so Blazor JS interop
// closes the message channel immediately instead of leaving it open.
window.skillSnapStorage = {
    getItem: function (key) {
        return Promise.resolve(localStorage.getItem(key));
    },
    setItem: function (key, value) {
        return Promise.resolve().then(function () {
            localStorage.setItem(key, value);
        });
    },
    removeItem: function (key) {
        return Promise.resolve().then(function () {
            localStorage.removeItem(key);
        });
    }
};

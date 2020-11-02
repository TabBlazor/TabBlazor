window.blazorTabler = {

    scrollToFragment: (elementId) => {
        var element = document.getElementById(elementId);

        if (element) {
            element.scrollIntoView({
                behavior: 'smooth'
            });
        }
    },

    showPrompt: (message, defaultValue) => {
        return prompt(message, defaultValue);
    },

    showAlert: (message) => {
        return alert(message);
    },

      windowOpen: (url, name, features, replace) => {
        window.open(url, name, features, replace);
        return "";
    },

    redirect: (url) => {
        window.open(url);
        return "";
    },

    clickOutsideHandler: {
        addEvent: function (elementId, dotnetHelper) {
            window.addEventListener("click", (e) => {
                var element = document.getElementById(elementId);
                if (e != null && element != null) {
                    if (e.target !== element && !element.contains(e.target)) {
                        dotnetHelper.invokeMethodAsync("InvokeClickOutside");
                    }
                }
            });
        }
    }
}

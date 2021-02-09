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

    disableDraggable: (container, element) => {

        element.addEventListener("mousedown", (e) => {
            e.stopPropagation();
            container['draggable'] = false;
        });

        element.addEventListener("mouseup", (e) => {
            container['draggable'] = true;
        });

        element.addEventListener("mouseleave", (e) => {
            container['draggable'] = true;
        });
    },

    setPropByElement: (element, property, value) => {
        element[property] = value;
        return "";
    },


    clickOutsideHandler: {
        addEvent: function (elementId, dotnetHelper) {
            window.addEventListener("click", (e) => {
                var element = document.getElementById(elementId);
                if (e != null && element != null) {
                    if (e.target.isConnected == true && e.target !== element && (!element.contains(e.target))) {
                        dotnetHelper.invokeMethodAsync("InvokeClickOutside");
                    }
                }
            });
        }
    }
}

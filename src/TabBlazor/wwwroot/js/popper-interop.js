let popperLoadPromise = null;

function ensurePopper(scriptUrl) {
    if (window.Popper) return Promise.resolve(window.Popper);
    if (popperLoadPromise) return popperLoadPromise;

    popperLoadPromise = new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[data-tabblazor-popper]`);
        if (existing) {
            existing.addEventListener('load', () => resolve(window.Popper));
            existing.addEventListener('error', () => reject(new Error('popper script failed')));
            return;
        }
        const s = document.createElement('script');
        s.src = scriptUrl;
        s.async = true;
        s.setAttribute('data-tabblazor-popper', '');
        s.onload = () => {
            if (window.Popper) resolve(window.Popper);
            else reject(new Error('popper loaded but window.Popper missing'));
        };
        s.onerror = () => reject(new Error('failed loading popper from ' + scriptUrl));
        document.head.appendChild(s);
    });
    return popperLoadPromise;
}

export async function create(reference, popper, options, scriptUrl) {
    const lib = await ensurePopper(scriptUrl);

    const popperOpts = {
        placement: options.placement,
        strategy: options.strategy,
        modifiers: [
            { name: 'offset', options: { offset: [0, options.offset || 0] } },
            { name: 'preventOverflow', options: { padding: 8 } },
            { name: 'flip', options: { padding: 8 } }
        ]
    };

    const instance = lib.createPopper(reference, popper, popperOpts);

    return {
        update: () => instance.update(),
        forceUpdate: () => instance.forceUpdate(),
        show: () => {
            popper.setAttribute('data-show', '');
            instance.update();
        },
        hide: () => {
            popper.removeAttribute('data-show');
        },
        setPlacement: (placement) => {
            instance.setOptions((opts) => ({ ...opts, placement }));
        },
        destroy: () => instance.destroy()
    };
}

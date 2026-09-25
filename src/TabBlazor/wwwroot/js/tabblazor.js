window.tabBlazor = {

    setTheme: function (theme) {

        document.querySelector("body").setAttribute("data-bs-theme", theme);

    },

    getUserAgent: function () {
        return navigator.userAgent;
    },

    openContentWindow(contentType, content, urlSuffix, name, features) {
        const blob = new Blob([content], { type: contentType });
        var url = URL.createObjectURL(blob);

        if (urlSuffix) {
            url = url + urlSuffix;
        }

        var newWindow = window.open(url, name, features);

        newWindow.addEventListener('beforeunload', () => {
            URL.revokeObjectURL(url, name, features);
        })

    },



    createObjectURL(contentType, content) {
        const blob = new Blob([content], { type: contentType });
        return URL.createObjectURL(blob);
    },

    revokeObjectURL(objectURL) {
        URL.revokeObjectURL(objectURL);
    },

  
    saveAsBinary: function (filename, contentType, content) {
        const file = new File([content], filename, { type: contentType });
        const exportUrl = URL.createObjectURL(file);
        const a = document.createElement("a");
        document.body.appendChild(a);
        a.href = exportUrl;
        a.download = filename;
        a.target = "_self";
        a.click();
        URL.revokeObjectURL(exportUrl);
    },

    saveAsFile: function (filename, href) {
        var link = document.createElement('a');
        link.download = filename;
        link.href = href;
        document.body.appendChild(link); // Needed for Firefox
        link.click();
        document.body.removeChild(link);
    },

    addResizeObserver: (element, dotNetReference) => {
        const resizeObserver = new ResizeObserver(onResize);
        resizeObserver.observe(element);

        function onResize(entries) {
            const entry = entries[0];
            console.log(entry);
            const result = {
                contentRect: entry.contentRect,
            };

            dotNetReference.invokeMethodAsync("ElementResized", result);
        }
    },

    preventDefaultKey: (element, event, keys) => {
        element.addEventListener(event,
            (e) => {
                if (keys.includes(e.key)) {
                    e.preventDefault();
                }
            });
    },

    focusFirstInTableRow: (tr) => {
        var td = tr.cells[0];
        window.tabBlazor.navigateTable(td, '');
    },

    navigateTable: (td, key) => {
        var tr = td.closest('tr');
        if (!tr) {
            return;
        }

        var moveToRow = tr;
        if (key == 'ArrowUp') {
            moveToRow = tr.parentNode.rows[tr.rowIndex - 2];
        } else if (key == 'ArrowDown') {
            moveToRow = tr.parentNode.rows[tr.rowIndex];
        }

        if (!moveToRow) {
            return;
        }

        var pos = td.cellIndex;
        if (key == 'ArrowLeft') {
            pos = pos - 1;
        } else if (key == 'ArrowRight') {
            pos = pos + 1;
        } else if (key == '') {
            key = 'ArrowRight';
        }

        var moveToCell = moveToRow.cells[pos];
        if (!moveToCell) {
            return;
        }

        var focusElement = Array.from(moveToCell.getElementsByTagName("*")).find(x => x.tabIndex >= 0);

        if (focusElement) {
            focusElement.focus();
            if (focusElement.select) {
                focusElement.select();
            }
        } else {
            //Try next
            window.tabBlazor.navigateTable(moveToCell, key);
        }
    },

    scrollToFragment: (elementId) => {
        var element = document.getElementById(elementId);

        if (element) {
            element.scrollIntoView({
                behavior: 'smooth'
           });
        }
    },

    scrollIntoView: (elementId, options) => {
        var element = document.getElementById(elementId);

        if (element) {
            element.scrollIntoView(options);
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

    copyToClipboard: (text) => {
        navigator.clipboard.writeText(text)
    },

    readFromClipboard: () => {
        return navigator.clipboard.readText();
    },

    disableDraggable: (container, element) => {

        element.addEventListener("mousedown",
            (e) => {
                e.stopPropagation();
                container['draggable'] = false;
            });

        element.addEventListener("mouseup",
            (e) => {
                container['draggable'] = true;
            });

        element.addEventListener("mouseleave",
            (e) => {
                container['draggable'] = true;
            });
    },

    setPropByElement: (element, property, value) => {
        element[property] = value;
        return "";
    },

    confetti: (() => {
        const palette = [
            ['blue', '#066fd1'], ['azure', '#4299e1'], ['indigo', '#4263eb'], ['purple', '#ae3ec9'],
            ['pink', '#d6336c'], ['red', '#d63939'], ['orange', '#f76707'], ['yellow', '#f59f00'],
            ['lime', '#74b816'], ['green', '#2fb344'], ['teal', '#0ca678'], ['cyan', '#17a2b8']
        ];
        const referenceHeight = 800;
        const frameMs = 1000 / 60;
        const maxDevicePixelRatio = 2;
        const defaults = { count: 220, duration: 3500, decay: 3.5, fade: 0.15, speed: 1, colors: null };

        const stage = {
            canvas: null,
            context: null,
            particles: [],
            emitters: [],
            frame: 0,
            lastTick: 0,

            add(emitter) {
                this.emitters.push(emitter);
                this.mount();
                if (!this.frame) {
                    this.lastTick = performance.now();
                    this.frame = requestAnimationFrame((now) => this.tick(now));
                }
            },

            mount() {
                if (this.canvas) return;
                const canvas = document.createElement('canvas');
                canvas.className = 'confetti-canvas';
                canvas.setAttribute('aria-hidden', 'true');
                Object.assign(canvas.style, { position: 'fixed', inset: '0', width: '100%', height: '100%', zIndex: '9999', pointerEvents: 'none', display: 'block' });
                document.body.append(canvas);
                this.canvas = canvas;
                this.context = canvas.getContext('2d');
                this.resize();
                window.addEventListener('resize', this.onResize);
            },

            unmount() {
                cancelAnimationFrame(this.frame);
                this.frame = 0;
                window.removeEventListener('resize', this.onResize);
                this.canvas?.remove();
                this.canvas = null;
                this.context = null;
                this.particles = [];
                this.emitters = [];
            },

            onResize: () => stage.resize(),

            resize() {
                if (!this.canvas || !this.context) return;
                const dpr = Math.min(window.devicePixelRatio || 1, maxDevicePixelRatio);
                this.canvas.width = window.innerWidth * dpr;
                this.canvas.height = window.innerHeight * dpr;
                this.context.setTransform(dpr, 0, 0, dpr, 0, 0);
            },

            spawn(emitter) {
                const speed = (window.innerHeight / referenceHeight) * emitter.config.speed;
                return {
                    emitter,
                    x: Math.random() * window.innerWidth,
                    y: -20 - Math.random() * 30,
                    w: 6 + Math.random() * 3.6,
                    h: 10 + Math.random() * 4.4,
                    color: emitter.colors[Math.floor(Math.random() * emitter.colors.length)] ?? '#000',
                    vx: (Math.random() - 0.5) * 1.2,
                    vy: (1.5 + Math.random() * 2.1) * speed,
                    speed,
                    angle: Math.random() * Math.PI * 2,
                    spin: (Math.random() - 0.5) * 0.25,
                    flip: Math.random() * Math.PI * 2,
                    flipSpeed: 0.12 + Math.random() * 0.15,
                    sway: Math.random() * Math.PI * 2,
                    alpha: 1
                };
            },

            pour(emitter, now) {
                const { count, duration, decay } = emitter.config;
                const elapsed = now - emitter.startedAt;
                if (emitter.stopped || elapsed >= duration) return;
                const t = elapsed / duration;
                const target = Math.round((count * (1 - Math.exp(-decay * t))) / (1 - Math.exp(-decay)));
                while (emitter.emitted < target) {
                    this.particles.push(this.spawn(emitter));
                    emitter.emitted++;
                }
            },

            tick(now) {
                const context = this.context;
                if (!context) return;
                const step = Math.min((now - this.lastTick) / frameMs, 3);
                this.lastTick = now;
                const width = window.innerWidth;
                const height = window.innerHeight;
                context.clearRect(0, 0, width, height);

                for (const emitter of this.emitters) this.pour(emitter, now);

                for (let i = this.particles.length - 1; i >= 0; i--) {
                    const p = this.particles[i];
                    const fadeFrom = height * (1 - p.emitter.config.fade);
                    p.sway += 0.05 * step;
                    p.vy = Math.min(p.vy + 0.025 * p.speed * step, 4.2 * p.speed);
                    p.x += (p.vx + Math.sin(p.sway) * 0.7) * step;
                    p.y += p.vy * step;
                    p.angle += p.spin * step;
                    p.flip += p.flipSpeed * step;
                    if (p.y > fadeFrom) p.alpha = Math.max(0, 1 - (p.y - fadeFrom) / (height - fadeFrom));
                    if (p.alpha === 0) { this.particles.splice(i, 1); continue; }
                    context.save();
                    context.globalAlpha = p.alpha;
                    context.translate(p.x, p.y);
                    context.rotate(p.angle);
                    context.scale(1, Math.cos(p.flip));
                    context.fillStyle = p.color;
                    context.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
                    context.restore();
                }

                this.settle(now);
                if (this.emitters.length === 0) { this.unmount(); return; }
                this.frame = requestAnimationFrame((next) => this.tick(next));
            },

            settle(now) {
                for (let i = this.emitters.length - 1; i >= 0; i--) {
                    const emitter = this.emitters[i];
                    const pouring = !emitter.stopped && now - emitter.startedAt < emitter.config.duration;
                    const airborne = this.particles.some((p) => p.emitter === emitter);
                    if (pouring || airborne) continue;
                    this.emitters.splice(i, 1);
                    emitter.done = true;
                    emitter.dotNetRef?.invokeMethodAsync('OnConfettiEnd');
                }
            }
        };

        const emitters = new Map();
        let nextId = 1;

        const paletteColors = () => {
            const style = getComputedStyle(document.documentElement);
            return palette.map(([name, fallback]) => style.getPropertyValue(`--tblr-${name}`).trim() || fallback);
        };

        const normalizeConfig = (options) => {
            const config = { ...defaults, ...(options || {}) };
            if (typeof config.colors === 'string') {
                config.colors = config.colors.split(',').map((c) => c.trim()).filter(Boolean);
            }
            if (!config.colors || config.colors.length === 0) config.colors = null;
            return config;
        };

        return {
            burst(options, dotNetRef) {
                const config = normalizeConfig(options);
                const id = nextId++;

                if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                    dotNetRef?.invokeMethodAsync('OnConfettiEnd');
                    return id;
                }

                const emitter = {
                    id,
                    config,
                    colors: config.colors ?? paletteColors(),
                    startedAt: performance.now(),
                    emitted: 0,
                    stopped: false,
                    done: false,
                    dotNetRef
                };
                emitters.set(id, emitter);
                stage.add(emitter);
                return id;
            },

            stop(id) {
                const emitter = emitters.get(id);
                if (emitter && !emitter.done) emitter.stopped = true;
                emitters.delete(id);
            }
        };
    })(),

    clickOutsideHandler: {
        removeEvent: (elementId) => {
            if (elementId === undefined || window.clickHandlers === undefined) return;
            if (!window.clickHandlers.has(elementId)) return;

            var handler = window.clickHandlers.get(elementId);
            window.removeEventListener("click", handler);
            window.clickHandlers.delete(elementId);
        },
        addEvent: (elementId, unregisterAfterClick, dotnetHelper) => {
            window.tabBlazor.clickOutsideHandler.removeEvent(elementId);

            if (window.clickHandlers === undefined) {
                window.clickHandlers = new Map();
            }
            var currentTime = (new Date()).getTime();

            var handler = (e) => {

                var nowTime = (new Date()).getTime();
                var diff = Math.abs((nowTime - currentTime) / 1000);

                if (diff < 0.5)
                    return;

                currentTime = nowTime;

                var element = document.getElementById(elementId);
                if (e != null && element != null) {
                    if (e.target.isConnected === true && e.target !== element && (!element.contains(e.target))) {
                        if (unregisterAfterClick) {
                            window.tabBlazor.clickOutsideHandler.removeEvent(elementId);
                        }
                        dotnetHelper.invokeMethodAsync("InvokeClickOutside");
                    }
                }
            };
            window.clickHandlers.set(elementId, handler);
            window.addEventListener("click", handler);
        }
    }
}

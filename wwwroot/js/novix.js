(() => {
    "use strict";

    const container = document.getElementById("planet-container");
    const loading = document.getElementById("planet-loading");
    const loadingMessage = document.getElementById("loading-message");
    const starCanvas = document.getElementById("starfield");

    if (!container) {
        console.error("NOVIX: no se encontró planet-container.");
        return;
    }

    const reducedMotion = window.matchMedia(
        "(prefers-reduced-motion: reduce)"
    ).matches;

    let THREE = null;
    let scene = null;
    let camera = null;
    let renderer = null;
    let planetGroup = null;
    let planet = null;
    let ringGroup = null;
    let animationId = null;
    let resizeObserver = null;

    let dragging = false;
    let previousX = 0;
    let previousY = 0;
    let rotationX = -0.12;
    let rotationY = 0.4;
    let velocityX = 0;
    let velocityY = 0;
    let lastFrame = 0;
    let elapsed = 0;

    const shootingStars = [];

    function setLoadingMessage(message) {
        if (loadingMessage) {
            loadingMessage.textContent = message;
        }
    }

    function hideLoading() {
        if (!loading) return;

        loading.classList.add("hidden");

        window.setTimeout(() => {
            loading.remove();
        }, 600);
    }

    function showFallback(message) {
        console.error("NOVIX 3D:", message);

        if (loading) {
            loading.classList.remove("hidden");
            loading.innerHTML = `
                <div class="fallback-orb" aria-hidden="true"></div>
                <span>${message}</span>
                <span class="fallback-note">
                    EFECTO ESPACIAL ALTERNATIVO
                </span>
            `;
        }

        createFallbackPlanet();
    }

    /* =========================================
       ALTERNATIVA VISUAL SI THREE.JS FALLA
       ========================================= */

    function createFallbackPlanet() {
        if (container.querySelector(".fallback-planet")) {
            return;
        }

        const orb = document.createElement("div");
        orb.className = "fallback-planet";
        orb.setAttribute("aria-label", "Planeta espacial alternativo");

        orb.innerHTML = `
            <div class="fallback-orbit fallback-orbit-one"></div>
            <div class="fallback-orbit fallback-orbit-two"></div>
            <div class="fallback-sphere"></div>
        `;

        container.appendChild(orb);

        if (loading) {
            loading.classList.add("fallback-loading");
        }

        let down = false;
        let startX = 0;
        let angle = 0;

        orb.addEventListener("pointerdown", event => {
            down = true;
            startX = event.clientX;
            orb.setPointerCapture(event.pointerId);
        });

        orb.addEventListener("pointermove", event => {
            if (!down) return;

            angle += (event.clientX - startX) * 0.5;
            startX = event.clientX;

            const sphere = orb.querySelector(".fallback-sphere");

            if (sphere) {
                sphere.style.transform =
                    `rotateY(${angle}deg) rotateX(-12deg)`;
            }
        });

        function stopDragging() {
            down = false;
        }

        orb.addEventListener("pointerup", stopDragging);
        orb.addEventListener("pointercancel", stopDragging);
        orb.addEventListener("lostpointercapture", stopDragging);
    }

    /* =========================================
       TEXTURA PROCEDURAL
       ========================================= */

    function createPlanetTexture() {
        const canvas = document.createElement("canvas");

        canvas.width = 1024;
        canvas.height = 512;

        const ctx = canvas.getContext("2d");

        if (!ctx) {
            throw new Error("No se pudo generar la textura del planeta.");
        }

        const gradient = ctx.createLinearGradient(
            0, 0, 0, canvas.height
        );

        gradient.addColorStop(0, "#08080d");
        gradient.addColorStop(.22, "#282832");
        gradient.addColorStop(.45, "#858591");
        gradient.addColorStop(.57, "#44444f");
        gradient.addColorStop(.80, "#15151c");
        gradient.addColorStop(1, "#050507");

        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        for (let i = 0; i < 350; i++) {
            const x = Math.random() * canvas.width;
            const y = Math.random() * canvas.height;
            const radius = 2 + Math.random() * 30;

            const cloud = ctx.createRadialGradient(
                x, y, 0, x, y, radius
            );

            cloud.addColorStop(
                0,
                `rgba(255,255,255,${Math.random() * .18})`
            );

            cloud.addColorStop(1, "rgba(255,255,255,0)");

            ctx.fillStyle = cloud;
            ctx.beginPath();
            ctx.arc(x, y, radius, 0, Math.PI * 2);
            ctx.fill();
        }

        for (let i = 0; i < 75; i++) {
            const y = Math.random() * canvas.height;

            ctx.strokeStyle =
                `rgba(255,255,255,${Math.random() * .12})`;

            ctx.lineWidth = 1 + Math.random() * 5;
            ctx.beginPath();
            ctx.moveTo(0, y);

            for (let x = 0; x <= canvas.width; x += 60) {
                ctx.lineTo(
                    x,
                    y + Math.sin(x * .01 + i) * 9
                );
            }

            ctx.stroke();
        }

        const texture = new THREE.CanvasTexture(canvas);
        texture.colorSpace = THREE.SRGBColorSpace;

        return texture;
    }

    function createBumpTexture() {
        const canvas = document.createElement("canvas");

        canvas.width = 512;
        canvas.height = 256;

        const ctx = canvas.getContext("2d");

        if (!ctx) {
            throw new Error("No se pudo crear el relieve del planeta.");
        }

        ctx.fillStyle = "#777777";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        for (let i = 0; i < 600; i++) {
            const x = Math.random() * canvas.width;
            const y = Math.random() * canvas.height;
            const radius = 1 + Math.random() * 8;
            const tone = Math.floor(90 + Math.random() * 150);

            ctx.fillStyle = `rgb(${tone},${tone},${tone})`;
            ctx.beginPath();
            ctx.arc(x, y, radius, 0, Math.PI * 2);
            ctx.fill();
        }

        return new THREE.CanvasTexture(canvas);
    }

    function createRingTexture() {
        const canvas = document.createElement("canvas");

        canvas.width = 512;
        canvas.height = 32;

        const ctx = canvas.getContext("2d");

        if (!ctx) {
            throw new Error("No se pudo crear la textura de los anillos.");
        }

        const gradient = ctx.createLinearGradient(
            0, 0, canvas.width, 0
        );

        gradient.addColorStop(0, "rgba(255,255,255,0)");
        gradient.addColorStop(.15, "rgba(230,230,240,.8)");
        gradient.addColorStop(.28, "rgba(100,100,120,.25)");
        gradient.addColorStop(.42, "rgba(255,255,255,.85)");
        gradient.addColorStop(.56, "rgba(150,150,170,.2)");
        gradient.addColorStop(.72, "rgba(255,255,255,.65)");
        gradient.addColorStop(1, "rgba(255,255,255,0)");

        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        const texture = new THREE.CanvasTexture(canvas);
        texture.colorSpace = THREE.SRGBColorSpace;

        return texture;
    }

    /* =========================================
       ILUMINACIÓN
       ========================================= */

    function createLighting() {
        scene.add(
            new THREE.AmbientLight(0xb8b8c8, 1.5)
        );

        const mainLight = new THREE.DirectionalLight(
            0xffffff,
            3
        );

        mainLight.position.set(-5, 4, 7);
        scene.add(mainLight);

        const sideLight = new THREE.PointLight(
            0xd8d8f0,
            30,
            30
        );

        sideLight.position.set(5, 2, -4);
        scene.add(sideLight);

        const backLight = new THREE.PointLight(
            0xaaaac8,
            15,
            30
        );

        backLight.position.set(0, 4, -8);
        scene.add(backLight);
    }

    /* =========================================
       ESTRELLAS 3D
       ========================================= */

    function createStars() {
        const positions = [];
        const colors = [];

        for (let i = 0; i < 1800; i++) {
            const radius = 30 + Math.random() * 90;
            const theta = Math.random() * Math.PI * 2;
            const phi = Math.acos(2 * Math.random() - 1);

            positions.push(
                radius * Math.sin(phi) * Math.cos(theta),
                radius * Math.cos(phi),
                radius * Math.sin(phi) * Math.sin(theta)
            );

            const brightness = .55 + Math.random() * .45;

            colors.push(
                brightness,
                brightness,
                Math.min(1, brightness + .04)
            );
        }

        const geometry = new THREE.BufferGeometry();

        geometry.setAttribute(
            "position",
            new THREE.Float32BufferAttribute(positions, 3)
        );

        geometry.setAttribute(
            "color",
            new THREE.Float32BufferAttribute(colors, 3)
        );

        const material = new THREE.PointsMaterial({
            size: .15,
            vertexColors: true,
            transparent: true,
            opacity: .9,
            sizeAttenuation: true,
            depthWrite: false
        });

        scene.add(new THREE.Points(geometry, material));
    }

    /* =========================================
       PLANETA Y ATMÓSFERA
       ========================================= */

    function createPlanet() {
        planetGroup = new THREE.Group();
        scene.add(planetGroup);

        const geometry = new THREE.SphereGeometry(
            2.05,
            96,
            64
        );

        const material = new THREE.MeshStandardMaterial({
            map: createPlanetTexture(),
            bumpMap: createBumpTexture(),
            bumpScale: .06,
            roughness: .8,
            metalness: .15
        });

        planet = new THREE.Mesh(geometry, material);
        planetGroup.add(planet);

        const atmosphereGeometry = new THREE.SphereGeometry(
            2.17,
            64,
            48
        );

        const atmosphereMaterial = new THREE.ShaderMaterial({
            uniforms: {
                glowColor: {
                    value: new THREE.Color(0xdedeea)
                }
            },

            vertexShader: `
                varying vec3 vNormal;
                varying vec3 vViewPosition;

                void main() {
                    vNormal = normalize(normalMatrix * normal);

                    vec4 positionView = modelViewMatrix *
                        vec4(position, 1.0);

                    vViewPosition = -positionView.xyz;

                    gl_Position = projectionMatrix * positionView;
                }
            `,

            fragmentShader: `
                uniform vec3 glowColor;

                varying vec3 vNormal;
                varying vec3 vViewPosition;

                void main() {
                    float intensity = pow(
                        max(
                            0.0,
                            0.75 - dot(
                                normalize(vNormal),
                                normalize(vViewPosition)
                            )
                        ),
                        3.0
                    );

                    gl_FragColor = vec4(
                        glowColor,
                        intensity * 0.8
                    );
                }
            `,

            transparent: true,
            blending: THREE.AdditiveBlending,
            side: THREE.BackSide,
            depthWrite: false
        });

        planetGroup.add(
            new THREE.Mesh(
                atmosphereGeometry,
                atmosphereMaterial
            )
        );

        const halo = new THREE.Mesh(
            new THREE.SphereGeometry(2.4, 48, 48),
            new THREE.MeshBasicMaterial({
                color: 0xbfc0d2,
                transparent: true,
                opacity: .035,
                side: THREE.BackSide,
                depthWrite: false
            })
        );

        planetGroup.add(halo);

        createPlanetRings();

        planetGroup.rotation.x = -.12;
        planetGroup.rotation.y = .4;
    }

    /* =========================================
       ANILLOS ORBITALES
       ========================================= */

    function createPlanetRings() {
        ringGroup = new THREE.Group();

        const ring = new THREE.Mesh(
            new THREE.RingGeometry(2.65, 3.65, 192),
            new THREE.MeshBasicMaterial({
                map: createRingTexture(),
                color: 0xe3e3ec,
                transparent: true,
                opacity: .8,
                side: THREE.DoubleSide,
                depthWrite: false
            })
        );

        ring.rotation.x = Math.PI / 2.35;
        ring.rotation.y = -.12;

        ringGroup.add(ring);

        const outerRing = new THREE.Mesh(
            new THREE.RingGeometry(3.85, 3.87, 192),
            new THREE.MeshBasicMaterial({
                color: 0xffffff,
                transparent: true,
                opacity: .25,
                side: THREE.DoubleSide,
                depthWrite: false
            })
        );

        outerRing.rotation.x = Math.PI / 2.35;
        outerRing.rotation.y = -.12;

        ringGroup.add(outerRing);

        planetGroup.add(ringGroup);
    }

    /* =========================================
       CONTROL DE ROTACIÓN
       ========================================= */

    function onPointerDown(event) {
        dragging = true;
        previousX = event.clientX;
        previousY = event.clientY;
        velocityX = 0;
        velocityY = 0;

        container.style.cursor = "grabbing";

        try {
            container.setPointerCapture(event.pointerId);
        } catch (_) {
            // La captura del puntero es opcional.
        }
    }

    function onPointerMove(event) {
        if (!dragging) return;

        const dx = event.clientX - previousX;
        const dy = event.clientY - previousY;

        velocityY = dx * .008;
        velocityX = dy * .008;

        rotationY += velocityY;
        rotationX += velocityX;

        rotationX = Math.max(-1.15, Math.min(1.15, rotationX));

        previousX = event.clientX;
        previousY = event.clientY;
    }

    function onPointerUp() {
        dragging = false;
        container.style.cursor = "grab";
    }

    container.addEventListener("pointerdown", onPointerDown);
    container.addEventListener("pointermove", onPointerMove);
    container.addEventListener("pointerup", onPointerUp);
    container.addEventListener("pointercancel", onPointerUp);
    container.addEventListener("lostpointercapture", onPointerUp);

    container.addEventListener("contextmenu", event => {
        event.preventDefault();
    });

    /* =========================================
       ESTRELLAS FUGACES
       ========================================= */

    function createShootingStar() {
        const start = new THREE.Vector3(
            (Math.random() - .5) * 20,
            6 + Math.random() * 5,
            -5 - Math.random() * 8
        );

        const end = start.clone().add(
            new THREE.Vector3(-4, -4, 0)
        );

        const geometry = new THREE.BufferGeometry()
            .setFromPoints([start, end]);

        const material = new THREE.LineBasicMaterial({
            color: 0xffffff,
            transparent: true,
            opacity: .8
        });

        const line = new THREE.Line(geometry, material);

        scene.add(line);

        shootingStars.push({
            line,
            geometry,
            material,
            life: 1,
            speed: .018 + Math.random() * .012
        });
    }

    function updateShootingStars(delta) {
        elapsed += delta;

        if (elapsed > 3 && Math.random() < .015) {
            if (shootingStars.length < 2) {
                createShootingStar();
            }

            elapsed = 0;
        }

        const active = [];

        shootingStars.forEach(star => {
            star.life -= star.speed * delta * 60;
            star.material.opacity = Math.max(0, star.life * .8);

            if (star.life > 0) {
                active.push(star);
            } else {
                scene.remove(star.line);
                star.geometry.dispose();
                star.material.dispose();
            }
        });

        shootingStars.length = 0;
        shootingStars.push(...active);
    }

    /* =========================================
       ESCENA Y REDIMENSIONADO
       ========================================= */

    function resize() {
        if (!renderer || !camera) return;

        const width = Math.max(1, container.clientWidth);
        const height = Math.max(1, container.clientHeight);

        camera.aspect = width / height;
        camera.updateProjectionMatrix();

        renderer.setSize(width, height, false);

        if (reducedMotion) {
            renderer.render(scene, camera);
        }
    }

    function updatePlanet(delta) {
        if (!planetGroup) return;

        if (!dragging && !reducedMotion) {
            rotationY += .12 * delta;
            rotationY += velocityY * delta;
            rotationX += velocityX * delta;

            velocityX *= Math.pow(.94, delta);
            velocityY *= Math.pow(.94, delta);
        }

        rotationX = Math.max(-1.15, Math.min(1.15, rotationX));

        planetGroup.rotation.x +=
            (rotationX - planetGroup.rotation.x) *
            Math.min(1, delta * 5);

        planetGroup.rotation.y +=
            (rotationY - planetGroup.rotation.y) *
            Math.min(1, delta * 5);

        if (planet && !reducedMotion) {
            planet.rotation.y += .025 * delta;
        }
    }

    function renderFrame(time) {
        animationId = null;

        if (document.hidden || !renderer) return;

        const delta = lastFrame
            ? Math.min((time - lastFrame) / 1000, .05)
            : .016;

        lastFrame = time;

        updatePlanet(delta);
        updateShootingStars(delta);

        renderer.render(scene, camera);

        animationId = requestAnimationFrame(renderFrame);
    }

    function startAnimation() {
        if (
            reducedMotion ||
            animationId !== null ||
            document.hidden
        ) {
            return;
        }

        lastFrame = 0;
        animationId = requestAnimationFrame(renderFrame);
    }

    function stopAnimation() {
        if (animationId !== null) {
            cancelAnimationFrame(animationId);
            animationId = null;
        }
    }

    /* =========================================
       INICIALIZACIÓN SEGURA
       ========================================= */

    async function init() {
        try {
            setLoadingMessage("CARGANDO MOTOR 3D");

            if (!window.novixThreeReady) {
                throw new Error("No se encontró el cargador de Three.js.");
            }

            THREE = await window.novixThreeReady;

            if (!THREE || !THREE.WebGLRenderer) {
                throw new Error("Three.js no está disponible.");
            }

            setLoadingMessage("INICIALIZANDO ESCENA");

            scene = new THREE.Scene();
            scene.background = new THREE.Color(0x030305);

            camera = new THREE.PerspectiveCamera(
                38,
                Math.max(1, container.clientWidth) /
                    Math.max(1, container.clientHeight),
                .1,
                300
            );

            camera.position.set(0, 0, 10.5);

            renderer = new THREE.WebGLRenderer({
                antialias: true,
                alpha: false,
                powerPreference: "default"
            });

            renderer.setPixelRatio(
                Math.min(window.devicePixelRatio || 1, 1.5)
            );

            renderer.setSize(
                Math.max(1, container.clientWidth),
                Math.max(1, container.clientHeight)
            );

            renderer.outputColorSpace = THREE.SRGBColorSpace;
            renderer.toneMapping = THREE.ACESFilmicToneMapping;
            renderer.toneMappingExposure = 1.15;

            renderer.domElement.setAttribute(
                "aria-label",
                "Planeta tridimensional. Arrastra para rotarlo."
            );

            container.insertBefore(
                renderer.domElement,
                loading
            );

            setLoadingMessage("CREANDO PLANETA");

            createLighting();
            createStars();
            createPlanet();

            resizeObserver = new ResizeObserver(resize);
            resizeObserver.observe(container);

            window.addEventListener("resize", resize);

            hideLoading();

            if (reducedMotion) {
                renderer.render(scene, camera);
            } else {
                startAnimation();
            }
        } catch (error) {
            showFallback(
                "No se pudo cargar el motor 3D"
            );

            console.error(error);
        }
    }

    document.addEventListener("visibilitychange", () => {
        if (document.hidden) {
            stopAnimation();
        } else if (!reducedMotion && renderer) {
            startAnimation();
        }
    });

    window.addEventListener("beforeunload", () => {
        stopAnimation();

        if (resizeObserver) {
            resizeObserver.disconnect();
        }

        if (renderer) {
            renderer.dispose();
        }
    });

    /* Fondo estelar 2D: funciona aunque Three.js falle. */

    function initStarfield() {
        if (!starCanvas) return;

        const ctx = starCanvas.getContext("2d");

        if (!ctx) return;

        let stars = [];
        let frame = null;
        let sw = 0;
        let sh = 0;

        function resizeStars() {
            const dpr = Math.min(
                window.devicePixelRatio || 1,
                2
            );

            sw = window.innerWidth;
            sh = window.innerHeight;

            starCanvas.width = Math.floor(sw * dpr);
            starCanvas.height = Math.floor(sh * dpr);

            starCanvas.style.width = sw + "px";
            starCanvas.style.height = sh + "px";

            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

            const count = Math.min(
                550,
                Math.floor(sw * sh / 2200)
            );

            stars = Array.from({ length: count }, () => ({
                x: Math.random() * sw,
                y: Math.random() * sh,
                radius: .3 + Math.random() * 1.2,
                alpha: .2 + Math.random() * .65,
                phase: Math.random() * Math.PI * 2,
                speed: .5 + Math.random()
            }));

            if (reducedMotion) drawStars(0);
        }

        function drawStars(time) {
            ctx.clearRect(0, 0, sw, sh);

            stars.forEach(star => {
                const twinkle = reducedMotion
                    ? 1
                    : .6 + .4 * Math.sin(
                        time * .001 * star.speed + star.phase
                    );

                ctx.fillStyle =
                    `rgba(255,255,255,${star.alpha * twinkle})`;

                ctx.beginPath();

                ctx.arc(
                    star.x,
                    star.y,
                    star.radius,
                    0,
                    Math.PI * 2
                );

                ctx.fill();
            });
        }

        function animateStars(time) {
            frame = null;

            if (document.hidden || reducedMotion) return;

            drawStars(time);
            frame = requestAnimationFrame(animateStars);
        }

        function startStars() {
            if (
                frame !== null ||
                reducedMotion ||
                document.hidden
            ) {
                return;
            }

            frame = requestAnimationFrame(animateStars);
        }

        function stopStars() {
            if (frame !== null) {
                cancelAnimationFrame(frame);
                frame = null;
            }
        }

        resizeStars();

        if (!reducedMotion) {
            startStars();
        }

        window.addEventListener("resize", resizeStars);

        document.addEventListener("visibilitychange", () => {
            if (document.hidden) {
                stopStars();
            } else {
                startStars();
            }
        });
    }

    initStarfield();
    init();
})();

/* NOVIX ACADEMY SUPPORT — se mantiene separado del motor Three.js original. */
(() => {
    "use strict";
    const container = document.getElementById("planet-container");
    if (!container) return;

    // Mejora de accesibilidad para la zona 3D sin interferir con sus controles de arrastre.
    if (!container.hasAttribute("tabindex")) container.setAttribute("tabindex", "0");
    if (!container.hasAttribute("role")) container.setAttribute("role", "img");
    if (!container.hasAttribute("aria-label")) {
        container.setAttribute("aria-label", "Planeta 3D de NOVIX. Puedes arrastrar con el ratón para rotarlo.");
    }
})();

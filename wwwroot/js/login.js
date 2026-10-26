(() => {
    "use strict";

    const canvas = document.getElementById("login-stars");
    const planet = document.getElementById("loginPlanet");
    const togglePassword = document.getElementById("togglePassword");
    const password = document.getElementById("password");
    const loginForm = document.getElementById("loginForm");
    const loginMessage = document.getElementById("loginMessage");

    const reducedMotion = window.matchMedia(
        "(prefers-reduced-motion: reduce)"
    ).matches;

    /* ======================================
       ESTRELLAS
       ====================================== */

    if (canvas) {
        const ctx = canvas.getContext("2d");

        if (ctx) {
            let stars = [];
            let width = 0;
            let height = 0;
            let animationId = null;

            function resizeStars() {
                const dpr = Math.min(
                    window.devicePixelRatio || 1,
                    2
                );

                width = window.innerWidth;
                height = window.innerHeight;

                canvas.width = Math.floor(width * dpr);
                canvas.height = Math.floor(height * dpr);

                canvas.style.width = width + "px";
                canvas.style.height = height + "px";

                ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

                const count = Math.min(
                    500,
                    Math.floor(width * height / 2200)
                );

                stars = Array.from(
                    { length: count },
                    () => ({
                        x: Math.random() * width,
                        y: Math.random() * height,
                        radius: .3 + Math.random() * 1.2,
                        alpha: .2 + Math.random() * .65,
                        phase: Math.random() * Math.PI * 2,
                        speed: .4 + Math.random() * 1.4
                    })
                );

                if (reducedMotion) {
                    drawStars(0);
                }
            }

            function drawStars(time) {
                ctx.clearRect(0, 0, width, height);

                stars.forEach(star => {
                    const twinkle = reducedMotion
                        ? 1
                        : .55 + .45 * Math.sin(
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

                    if (star.radius > 1) {
                        ctx.fillStyle =
                            `rgba(255,255,255,${star.alpha * .12})`;

                        ctx.beginPath();

                        ctx.arc(
                            star.x,
                            star.y,
                            star.radius * 5,
                            0,
                            Math.PI * 2
                        );

                        ctx.fill();
                    }
                });
            }

            function animate(time) {
                animationId = null;

                if (document.hidden || reducedMotion) {
                    return;
                }

                drawStars(time);
                animationId = requestAnimationFrame(animate);
            }

            function startStars() {
                if (
                    animationId !== null ||
                    document.hidden ||
                    reducedMotion
                ) {
                    return;
                }

                animationId = requestAnimationFrame(animate);
            }

            function stopStars() {
                if (animationId !== null) {
                    cancelAnimationFrame(animationId);
                    animationId = null;
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
    }

    /* ======================================
       MOSTRAR / OCULTAR CONTRASEÑA
       ====================================== */

    if (togglePassword && password) {
        togglePassword.addEventListener("click", () => {
            const show = password.type === "password";

            password.type = show ? "text" : "password";

            togglePassword.setAttribute(
                "aria-label",
                show ? "Ocultar contraseña" : "Mostrar contraseña"
            );

            togglePassword.setAttribute(
                "aria-pressed",
                String(show)
            );
        });
    }

    /* ======================================
       ROTACIÓN INTERACTIVA DEL PLANETA
       ====================================== */

    if (planet) {
        let dragging = false;
        let previousX = 0;
        let previousY = 0;
        let rotateX = -12;
        let rotateY = 0;

        function updatePlanet() {
            planet.style.transform =
                `rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
        }

        planet.addEventListener("pointerdown", event => {
            dragging = true;
            previousX = event.clientX;
            previousY = event.clientY;

            planet.style.cursor = "grabbing";

            try {
                planet.setPointerCapture(event.pointerId);
            } catch (_) {
                // La captura no está disponible en todos los navegadores.
            }
        });

        planet.addEventListener("pointermove", event => {
            if (!dragging) {
                return;
            }

            const deltaX = event.clientX - previousX;
            const deltaY = event.clientY - previousY;

            rotateY += deltaX * .7;
            rotateX -= deltaY * .5;

            rotateX = Math.max(
                -55,
                Math.min(55, rotateX)
            );

            previousX = event.clientX;
            previousY = event.clientY;

            updatePlanet();
        });

        function stopDragging() {
            dragging = false;
            planet.style.cursor = "grab";
        }

        planet.addEventListener("pointerup", stopDragging);
        planet.addEventListener("pointercancel", stopDragging);
        planet.addEventListener("lostpointercapture", stopDragging);

        updatePlanet();
    }

    /* ======================================
       VALIDACIÓN VISUAL DEL FORMULARIO
       ====================================== */

    function showMessage(message, type) {
        if (!loginMessage) {
            return;
        }

        loginMessage.textContent = message;
        loginMessage.className = "login-message visible " + type;
    }

    if (loginForm) {
        loginForm.addEventListener("submit", event => {
            const email = document.getElementById("email");

            if (
                !email ||
                !email.value.trim() ||
                !password ||
                !password.value
            ) {
                event.preventDefault();

                showMessage(
                    "Ingresa tu correo electrónico y contraseña.",
                    "error"
                );

                return;
            }

            /*
             * No se cancela el envío del formulario.
             * Las credenciales deben validarse en el servidor.
             */
        });
    }
})();
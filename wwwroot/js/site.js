```javascript
document.addEventListener("DOMContentLoaded", function () {
    const enlaces = document.querySelectorAll(
        'a[href]:not([href^="#"]):not([href^="javascript:"]):not([target="_blank"])'
    );

    enlaces.forEach(function (enlace) {
        enlace.addEventListener("click", function (evento) {
            const destino = enlace.getAttribute("href");

            if (!destino || destino === "#") {
                return;
            }

            if (
                evento.ctrlKey ||
                evento.metaKey ||
                evento.shiftKey ||
                evento.altKey ||
                enlace.hasAttribute("download")
            ) {
                return;
            }

            let urlDestino;

            try {
                urlDestino = new URL(destino, window.location.href);
            } catch {
                return;
            }

            if (urlDestino.origin !== window.location.origin) {
                return;
            }

            if (urlDestino.href === window.location.href) {
                return;
            }

            evento.preventDefault();

            document.body.classList.add("novix-transitioning");

            setTimeout(function () {
                window.location.href = urlDestino.href;
            }, 450);
        });
    });
});
```
(() => {
  const body = document.body;
  const toggle = document.getElementById("menu-toggle");
  const menu = document.getElementById("site-menu");
  const backdrop = document.querySelector(".menu-backdrop");
  const menuLinks = menu?.querySelectorAll("a") ?? [];
  const firstMenuLink = menuLinks[0];
  const openLabel = toggle?.dataset.labelOpen ?? "Open menu";
  const closeLabel = toggle?.dataset.labelClose ?? "Close menu";

  if (!toggle || !menu || !backdrop) {
    return;
  }

  const setMenuOpen = (open, returnFocus = false) => {
    body.classList.toggle("menu-open", open);
    toggle.setAttribute("aria-expanded", String(open));
    toggle.setAttribute("aria-label", open ? closeLabel : openLabel);
    menu.setAttribute("aria-hidden", String(!open));
    backdrop.hidden = !open;

    if (open) {
      firstMenuLink?.focus();
    } else if (returnFocus) {
      toggle.focus();
    }
  };

  toggle.addEventListener("click", () => {
    setMenuOpen(toggle.getAttribute("aria-expanded") !== "true");
  });

  backdrop.addEventListener("click", () => setMenuOpen(false, true));
  menuLinks.forEach((link) => {
    link.addEventListener("click", () => setMenuOpen(false));
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && toggle.getAttribute("aria-expanded") === "true") {
      setMenuOpen(false, true);
    }
  });
})();

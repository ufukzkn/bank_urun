(() => {
  const valueSelector = "[data-month-chart-value]";
  const summarySelector = "[data-month-chart-summary]";
  let tooltip = null;
  let activeSource = null;

  function ensureTooltip() {
    if (tooltip) return tooltip;

    tooltip = document.createElement("div");
    tooltip.className = "month-chart-tooltip";
    tooltip.id = "monthChartTooltip";
    tooltip.setAttribute("role", "tooltip");
    tooltip.setAttribute("aria-live", "polite");
    tooltip.hidden = true;
    tooltip.innerHTML = "<span></span><strong></strong>";
    document.body.append(tooltip);
    return tooltip;
  }

  function constrainPosition(pointerX, pointerY) {
    const panel = ensureTooltip();
    const margin = 12;
    const width = panel.offsetWidth;
    const height = panel.offsetHeight;
    const left = pointerX + width + 24 <= window.innerWidth
      ? pointerX + 12
      : Math.max(margin, pointerX - width - 12);
    const top = pointerY - height - 12 >= margin
      ? pointerY - height - 12
      : Math.min(window.innerHeight - height - margin, pointerY + 16);

    panel.style.left = `${Math.round(left)}px`;
    panel.style.top = `${Math.round(top)}px`;
    panel.style.transform = "none";
  }

  function showTooltip(source, pointerX, pointerY) {
    const panel = ensureTooltip();
    const label = source.dataset.tooltipLabel || "";
    const value = source.dataset.tooltipValue || "";
    panel.querySelector("span").textContent = label;
    panel.querySelector("strong").textContent = value;
    panel.hidden = false;
    activeSource?.removeAttribute("aria-describedby");
    activeSource = source;
    activeSource.setAttribute("aria-describedby", panel.id);
    constrainPosition(pointerX, pointerY);
  }

  function showFocusedSummary(source) {
    const bounds = source.getBoundingClientRect();
    showTooltip(source, bounds.left + bounds.width / 2, bounds.top);
  }

  function hideTooltip(source = null) {
    if (source && activeSource !== source) return;
    activeSource?.removeAttribute("aria-describedby");
    activeSource = null;
    if (tooltip) tooltip.hidden = true;
  }

  function handlePointer(event) {
    const source = event.target.closest?.(valueSelector);
    if (!source) {
      if (activeSource?.matches?.(valueSelector)) hideTooltip(activeSource);
      return;
    }
    showTooltip(source, event.clientX, event.clientY);
  }

  document.addEventListener("pointerover", handlePointer);
  document.addEventListener("pointermove", handlePointer);

  document.addEventListener("pointerout", (event) => {
    const source = event.target.closest?.(valueSelector);
    if (!source) return;
    const relatedSource = event.relatedTarget?.closest?.(valueSelector);
    if (relatedSource === source) return;
    hideTooltip(source);
  });

  document.addEventListener("focusin", (event) => {
    const source = event.target.closest?.(summarySelector);
    if (source) showFocusedSummary(source);
  });

  document.addEventListener("focusout", (event) => {
    const source = event.target.closest?.(summarySelector);
    if (!source) return;
    const relatedSummary = event.relatedTarget?.closest?.(summarySelector);
    if (relatedSummary === source) return;
    hideTooltip(source);
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") hideTooltip();
  });

  window.addEventListener("scroll", () => hideTooltip(), true);
  window.addEventListener("resize", () => hideTooltip());
})();

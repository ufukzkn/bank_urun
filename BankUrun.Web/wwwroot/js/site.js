const codeMode = document.querySelector("#codeMode");
const manualCodeWrap = document.querySelector("#manualCodeWrap");
const manualCode = document.querySelector("#manualCode");
const productType = document.querySelector("#productType");
const codeSuggestion = document.querySelector("#codeSuggestion");
const createMainProductWrap = document.querySelector("#createMainProductWrap");
const mainProductCombo = document.querySelector("#mainProductCombo");
const createMainProductSearch = document.querySelector("#createMainProductSearch");
const createMainProductId = document.querySelector("#createMainProductId");
const periodFields = document.querySelector("#periodFields");
const actionConfirmToast = document.querySelector("#actionConfirmToast");
const actionToastBackdrop = document.querySelector("#actionToastBackdrop");
const toastTitle = document.querySelector("#toastTitle");
const toastMessage = document.querySelector("#toastMessage");
const confirmImpact = document.querySelector("[data-confirm-impact]");
const confirmCodeWrap = document.querySelector("[data-confirm-code-wrap]");
const confirmCodeInput = document.querySelector("[data-confirm-code-input]");
const confirmCodeLabel = document.querySelector("[data-confirm-code-label]");

let pendingForm = null;
let pendingSubmitter = null;
let pendingConfirmCode = "";
let pendingImpactAllowed = true;
let pendingImpactRequestId = 0;

function segmentedButtons(control) {
  return Array.from(control?.querySelectorAll(":scope > button[data-segmented-value]") || []);
}

function setSegmentedControlValue(control, value, dispatchChange = false) {
  if (!control) return;
  const buttons = segmentedButtons(control);
  if (!buttons.length) return;
  const activeButton = buttons.find((button) => button.dataset.segmentedValue === String(value))
    || buttons.find((button) => button.classList.contains("is-active"))
    || buttons[0];
  const activeIndex = buttons.indexOf(activeButton);

  control.style.setProperty("--segment-count", buttons.length.toString());
  control.style.setProperty("--segment-index", activeIndex.toString());
  buttons.forEach((button) => {
    const active = button === activeButton;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-pressed", active.toString());
  });

  const inputSelector = control.dataset.segmentedInput;
  const input = inputSelector ? control.querySelector(inputSelector) : null;
  if (input && input.value !== activeButton.dataset.segmentedValue) {
    input.value = activeButton.dataset.segmentedValue || "";
    if (dispatchChange) input.dispatchEvent(new Event("change", { bubbles: true }));
  }
}

function setupSegmentedControls(root = document) {
  root.querySelectorAll("[data-segmented-control]").forEach((control) => {
    if (control.dataset.segmentedReady === "true") return;
    control.dataset.segmentedReady = "true";
    const inputSelector = control.dataset.segmentedInput;
    const input = inputSelector ? control.querySelector(inputSelector) : null;
    const initialValue = input?.value || segmentedButtons(control).find((button) => button.classList.contains("is-active"))?.dataset.segmentedValue;
    setSegmentedControlValue(control, initialValue, false);
    control.addEventListener("click", (event) => {
      const button = event.target.closest("button[data-segmented-value]");
      if (!button || button.parentElement !== control || button.classList.contains("is-active")) return;
      setSegmentedControlValue(control, button.dataset.segmentedValue, true);
    });
  });
}

setupSegmentedControls();

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function parseLocalizedDecimal(value) {
  return Number((value || "0").toString().replace(",", "."));
}

function normalizeDecimalInputs(form) {
  form.querySelectorAll(".decimal-input").forEach((input) => {
    input.value = input.value.trim().replace(".", ",");
  });
}

function toggleManualCode() {
  if (!codeMode || !manualCodeWrap) {
    return;
  }

  const isManual = codeMode.value === "Manual";
  manualCodeWrap.classList.toggle("is-visible", isManual);
  if (!isManual && manualCode && codeSuggestion) {
    manualCode.value = "";
    codeSuggestion.textContent = "";
  }
}

function toggleCreateProductFields() {
  if (!productType || !createMainProductWrap || !createMainProductSearch || !createMainProductId) {
    return;
  }

  const isSubProduct = productType.value === "Sub";
  createMainProductWrap.classList.toggle("is-visible", isSubProduct);
  periodFields?.classList.toggle("d-none", isSubProduct);
  createMainProductSearch.required = isSubProduct;
  periodFields?.querySelectorAll("input, select").forEach((input) => {
    input.disabled = isSubProduct;
    input.required = !isSubProduct;
  });
  if (!isSubProduct) {
    createMainProductSearch.value = "";
    createMainProductId.value = "";
  }
}

async function refreshSuggestion() {
  if (!manualCode || !productType || !codeSuggestion || codeMode?.value !== "Manual") {
    return;
  }

  const code = manualCode.value.trim().toUpperCase();
  manualCode.value = code;
  if (code.length !== 2) {
    codeSuggestion.textContent = "";
    codeSuggestion.className = "form-text";
    return;
  }

  if (productType.value === "Sub" && !createMainProductId?.value) {
    codeSuggestion.className = "form-text text-warning";
    codeSuggestion.textContent = "Alt ürün kodu için önce bağlı ana ürünü seç.";
    return;
  }

  const params = new URLSearchParams({ type: productType.value, code });
  if (productType.value === "Sub" && createMainProductId?.value) {
    params.set("mainProductInstanceId", createMainProductId.value);
  }

  try {
    const response = await fetch(`/code-suggestion?${params.toString()}`);
    const result = await response.json();
    codeSuggestion.className = result.available ? "form-text text-success" : "form-text text-warning";
    codeSuggestion.textContent = result.available
      ? `${result.requested} uygun.`
      : (result.suggestion ? `${result.requested} dolu. Öneri: ${result.suggestion}` : result.message);
  } catch {
    codeSuggestion.className = "form-text text-danger";
    codeSuggestion.textContent = "Kod önerisi alınamadı.";
  }
}

function syncCreateMainProductId() {
  if (!createMainProductSearch || !createMainProductId) {
    return;
  }

  const options = Array.from(document.querySelectorAll("#mainProductOptions .combo-option"));
  const match = options.find((option) => option.dataset.label === createMainProductSearch.value);
  createMainProductId.value = match?.dataset.id || "";
}

function filterMainProductOptions() {
  if (!createMainProductSearch || !mainProductCombo) {
    return;
  }

  const query = createMainProductSearch.value.trim().toUpperCase();
  let visibleCount = 0;
  document.querySelectorAll("#mainProductOptions .combo-option").forEach((option) => {
    const text = option.dataset.label?.toUpperCase() || option.textContent?.toUpperCase() || "";
    const isVisible = !query || text.includes(query);
    option.classList.toggle("d-none", !isVisible);
    visibleCount += isVisible ? 1 : 0;
  });
  mainProductCombo.classList.toggle("has-no-results", visibleCount === 0);
}

function openMainProductCombo() {
  mainProductCombo?.classList.add("is-open");
  filterMainProductOptions();
}

function closeMainProductCombo() {
  mainProductCombo?.classList.remove("is-open");
}

function getToast() {
  return actionConfirmToast && window.bootstrap?.Toast
    ? window.bootstrap.Toast.getOrCreateInstance(actionConfirmToast, { autohide: false })
    : null;
}

function syncConfirmAvailability() {
  const codeMatches = !pendingConfirmCode
    || (confirmCodeInput?.value.trim().toLocaleUpperCase("tr-TR") === pendingConfirmCode.toLocaleUpperCase("tr-TR"));
  document.querySelectorAll("[data-toast-confirm]").forEach((button) => {
    button.disabled = !pendingImpactAllowed || !codeMatches;
  });
}

function renderConfirmImpact(result) {
  if (!confirmImpact) return;
  const counts = Array.isArray(result?.counts) ? result.counts : [];
  const warnings = Array.isArray(result?.warnings) ? result.warnings : [];
  const blockers = Array.isArray(result?.blockers) ? result.blockers : [];
  const rows = counts.filter((item) => Number(item.count) > 0)
    .map((item) => `<li><span>${escapeHtml(item.label)}</span><strong>${Number(item.count).toLocaleString("tr-TR")}</strong></li>`).join("");
  const notices = [...warnings.map((item) => `<p class="impact-warning">${escapeHtml(item)}</p>`),
    ...blockers.map((item) => `<p class="impact-blocker">${escapeHtml(item)}</p>`)].join("");
  confirmImpact.innerHTML = `<strong>${escapeHtml(result?.subject || "Etki özeti")}</strong><p>${escapeHtml(result?.summary || "")}</p>${rows ? `<ul>${rows}</ul>` : ""}${notices}`;
  confirmImpact.classList.remove("d-none");
  pendingImpactAllowed = result?.allowed !== false;
  syncConfirmAvailability();
}

async function loadConfirmImpact(url, requestId) {
  if (!url || !confirmImpact) return;
  pendingImpactAllowed = false;
  confirmImpact.innerHTML = '<span class="impact-loading">Etki hesaplanıyor…</span>';
  confirmImpact.classList.remove("d-none");
  syncConfirmAvailability();
  try {
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    const result = await response.json();
    if (requestId !== pendingImpactRequestId) return;
    if (!response.ok) throw new Error(result?.error || "Etki özeti alınamadı.");
    renderConfirmImpact(result);
  } catch (error) {
    if (requestId !== pendingImpactRequestId) return;
    pendingImpactAllowed = false;
    confirmImpact.innerHTML = `<p class="impact-blocker">${escapeHtml(error?.message || "Etki özeti alınamadı.")}</p>`;
    syncConfirmAvailability();
  }
}

function showActionToast(form, submitter, message) {
  const impactRequestId = ++pendingImpactRequestId;
  pendingForm = form;
  pendingSubmitter = submitter;
  pendingImpactAllowed = true;
  pendingConfirmCode = submitter?.dataset.confirmCode || form.dataset.confirmCode || "";
  if (toastTitle) {
    toastTitle.textContent = "İşlemi onayla";
  }
  if (toastMessage) {
    toastMessage.textContent = message || "Bu işlemi yapmak istediğinize emin misiniz?";
  }
  confirmImpact?.classList.add("d-none");
  if (confirmImpact) confirmImpact.innerHTML = "";
  confirmCodeWrap?.classList.toggle("d-none", !pendingConfirmCode);
  if (confirmCodeLabel) confirmCodeLabel.textContent = pendingConfirmCode;
  if (confirmCodeInput) confirmCodeInput.value = "";
  syncConfirmAvailability();
  actionToastBackdrop?.classList.add("is-visible");
  getToast()?.show();
  const impactUrl = submitter?.dataset.impactUrl || form.dataset.impactUrl;
  if (impactUrl) void loadConfirmImpact(impactUrl, impactRequestId);
}

function submitPendingForm() {
  if (!pendingForm) {
    return;
  }

  if (pendingConfirmCode) {
    let confirmationInput = pendingForm.querySelector('input[name="ConfirmationCode"]');
    if (!confirmationInput) {
      confirmationInput = document.createElement("input");
      confirmationInput.type = "hidden";
      confirmationInput.name = "ConfirmationCode";
      pendingForm.appendChild(confirmationInput);
    }
    confirmationInput.value = confirmCodeInput?.value.trim() || "";
  }

  pendingForm.dataset.toastConfirmed = "true";
  getToast()?.hide();
  pendingForm.requestSubmit(pendingSubmitter);
}

document.addEventListener("submit", (event) => {
  const form = event.target;
  if (!(form instanceof HTMLFormElement)) return;
  normalizeDecimalInputs(form);
  const missingCombo = Array.from(form.querySelectorAll("[data-combo-required]"))
    .find((input) => !input.value);
  if (missingCombo) {
    event.preventDefault();
    alert(missingCombo.dataset.comboRequired || "Seçim yapmalısınız.");
    return;
  }

  if (form.dataset.toastConfirmed === "true") {
    delete form.dataset.toastConfirmed;
    return;
  }

  const submitter = event.submitter;
  const message = submitter?.dataset.confirm || form.dataset.confirm;
  if (message) {
    event.preventDefault();
    if (form.matches("[data-group-product-removal]") && form.dataset.impactTemplate && submitter) {
      const values = new FormData(form);
      const params = new URLSearchParams({
        groupId: values.get("GroupId") || "",
        mainProductId: values.get("MainProductId") || "",
        effectiveFromYear: values.get("EffectiveFromYear") || "",
        effectiveFromTerm: values.get("EffectiveFromTerm") || ""
      });
      submitter.dataset.impactUrl = `${form.dataset.impactTemplate}?${params}`;
    }
    if (form.action.includes("/Organization/SaveBranchMainProductExclusion")
      && !form.querySelector('input[name="Id"]')?.value && submitter) {
      const values = new FormData(form);
      const impactUrl = new URL(form.action);
      impactUrl.pathname = impactUrl.pathname.replace(
        "/SaveBranchMainProductExclusion",
        "/BranchMainProductExclusionImpact");
      ["BranchId", "MainProductId", "EffectiveFromYear", "EffectiveFromTerm"]
        .forEach((field) => impactUrl.searchParams.set(field, values.get(field) || ""));
      submitter.dataset.impactUrl = impactUrl.toString();
    }
    showActionToast(form, submitter, message);
  }
});

document.querySelectorAll("[data-toast-cancel]").forEach((button) => button.addEventListener("click", () => {
  pendingForm = null;
  pendingSubmitter = null;
  getToast()?.hide();
}));
confirmCodeInput?.addEventListener("input", syncConfirmAvailability);

actionToastBackdrop?.addEventListener("click", () => {
  pendingForm = null;
  pendingSubmitter = null;
  getToast()?.hide();
});

document.querySelector("[data-toast-confirm]")?.addEventListener("click", () => submitPendingForm());
actionConfirmToast?.addEventListener("hidden.bs.toast", () => {
  pendingForm = null;
  pendingSubmitter = null;
  actionToastBackdrop?.classList.remove("is-visible");
});

createMainProductSearch?.addEventListener("focus", openMainProductCombo);
createMainProductSearch?.addEventListener("click", openMainProductCombo);
createMainProductSearch?.addEventListener("input", () => {
  if (createMainProductId) {
    createMainProductId.value = "";
  }
  openMainProductCombo();
  refreshSuggestion();
});
createMainProductSearch?.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    closeMainProductCombo();
  }
});
document.querySelectorAll("#mainProductOptions .combo-option").forEach((option) => option.addEventListener("click", () => {
  createMainProductSearch.value = option.dataset.label || "";
  createMainProductId.value = option.dataset.id || "";
  closeMainProductCombo();
  refreshSuggestion();
}));

function setupGenericCombos(root = document) {
root.querySelectorAll(".generic-combo:not([data-combo-ready])").forEach((combo) => {
  combo.dataset.comboReady = "true";
  const input = combo.querySelector("[data-combo-input]");
  const value = combo.querySelector("[data-combo-value]");
  const options = Array.from(combo.querySelectorAll("[data-combo-option]"));
  const filterOptions = () => {
    const query = input?.value.trim().toUpperCase() || "";
    let visibleCount = 0;
    options.forEach((option) => {
      const text = (option.dataset.label || option.textContent || "").toUpperCase();
      const isVisible = option.dataset.contextHidden !== "true" && (!query || text.includes(query));
      option.classList.toggle("d-none", !isVisible);
      visibleCount += isVisible ? 1 : 0;
    });
    combo.classList.toggle("has-no-results", visibleCount === 0);
  };
  input?.addEventListener("focus", () => { combo.classList.add("is-open"); filterOptions(); });
  input?.addEventListener("click", () => { combo.classList.add("is-open"); filterOptions(); });
  input?.addEventListener("input", () => {
    if (value?.value) {
      value.value = "";
      value.dispatchEvent(new Event("change", { bubbles: true }));
    }
    combo.classList.add("is-open");
    filterOptions();
  });
  input?.addEventListener("keydown", (event) => { if (event.key === "Escape") combo.classList.remove("is-open"); });
  options.forEach((option) => option.addEventListener("click", () => {
    if (input) input.value = option.dataset.label || "";
    if (value) {
      value.value = option.dataset.id || "";
      value.dispatchEvent(new Event("change", { bubbles: true }));
    }
    combo.classList.remove("is-open");
  }));
});
}

setupGenericCombos();

document.addEventListener("click", (event) => {
  if (mainProductCombo && !mainProductCombo.contains(event.target)) {
    closeMainProductCombo();
  }
  document.querySelectorAll(".generic-combo.is-open").forEach((combo) => {
    if (!combo.contains(event.target)) combo.classList.remove("is-open");
  });
});

createMainProductSearch?.closest("form")?.addEventListener("submit", (event) => {
  syncCreateMainProductId();
  if (productType?.value === "Sub" && !createMainProductId?.value) {
    event.preventDefault();
    alert("Alt ürün oluşturmak için listeden bağlı ana ürün seçmelisiniz.");
  }
});

function detailRowFor(row) {
  const detailId = row.dataset.detailId;
  return detailId ? document.querySelector(`[data-detail-for="${detailId}"]`) : null;
}

function closeDetail(row) {
  detailRowFor(row)?.querySelectorAll(".collapse.show").forEach((element) => {
    window.bootstrap?.Collapse.getOrCreateInstance(element, { toggle: false }).hide();
  });
}

function setupRemoteList(root, options) {
  const tableBody = root.querySelector("[data-remote-list-body]");
  const summary = root.querySelector("[data-list-summary]");
  const indicator = root.querySelector("[data-list-page-indicator]");
  const jump = root.querySelector("[data-list-page-jump]");
  const pageSize = root.querySelector("[data-list-page-size]");
  const filters = Array.from(root.querySelectorAll("[data-list-filter]"));
  const sortButtons = Array.from(root.querySelectorAll("[data-list-sort]"));
  const pageButtons = Array.from(root.querySelectorAll("[data-list-page]"));
  const state = {
    page: Number(root.dataset.listPage || 1),
    totalPages: Number(root.dataset.listTotalPages || 1),
    totalCount: Number(root.dataset.listTotalCount || 0),
    sort: { ...options.defaultSort }
  };
  let requestController = null;
  let debounceTimer = null;

  const filterValue = (name) => {
    const input = filters.find((item) => item.dataset.listFilter === name);
    return input?.type === "checkbox" ? input.checked : (input?.value.trim() || "");
  };
  const updateSortState = () => {
    sortButtons.forEach((button) => {
      const isActive = button.dataset.listSort === state.sort.key;
      button.classList.toggle("is-active", isActive);
      button.classList.toggle("desc", isActive && state.sort.direction === "desc");
      button.setAttribute("aria-sort", isActive ? (state.sort.direction === "asc" ? "ascending" : "descending") : "none");
    });
  };
  const updatePager = () => {
    const size = Number(pageSize?.value || 10);
    const first = state.totalCount === 0 ? 0 : (state.page - 1) * size + 1;
    const last = Math.min(state.page * size, state.totalCount);
    if (summary) {
      summary.textContent = state.totalCount === 0
        ? `0 ${options.label}`
        : `${state.totalCount} ${options.label} içinden ${first}-${last} gösteriliyor`;
    }
    if (indicator) indicator.textContent = `${state.page} / ${state.totalPages}`;
    if (jump) {
      jump.max = state.totalPages.toString();
      jump.value = state.page.toString();
    }
    pageButtons.forEach((button) => {
      const action = button.dataset.listPage;
      button.disabled = (action === "first" || action === "previous") ? state.page <= 1
        : (action === "next" || action === "last") ? state.page >= state.totalPages
          : false;
    });
  };
  const load = async (resetPage = true) => {
    if (resetPage) state.page = 1;
    requestController?.abort();
    requestController = new AbortController();
    root.classList.add("is-loading");
    try {
      const result = await options.remote({ state, filterValue, signal: requestController.signal });
      if (tableBody) tableBody.innerHTML = result.html;
      setupGenericCombos(tableBody || root);
      options.afterLoad?.(tableBody || root);
      state.page = result.page;
      state.totalPages = result.totalPages;
      state.totalCount = result.totalCount;
      updatePager();
    } catch (error) {
      if (error.name !== "AbortError" && tableBody) {
        tableBody.innerHTML = `<tr class="empty-row"><td colspan="${options.colspan || 9}" class="empty-cell">Liste yüklenemedi. Lütfen yeniden deneyin.</td></tr>`;
      }
    } finally {
      root.classList.remove("is-loading");
    }
  };
  const changePage = (page) => {
    state.page = Math.min(Math.max(1, page), state.totalPages);
    return load(false);
  };

  filters.forEach((input) => {
    const eventName = input.tagName === "SELECT" || input.type === "checkbox" ? "change" : "input";
    input.addEventListener(eventName, () => {
      window.clearTimeout(debounceTimer);
      debounceTimer = window.setTimeout(() => load(true), eventName === "input" ? 250 : 0);
    });
  });
  pageSize?.addEventListener("change", () => load(true));
  sortButtons.forEach((button) => button.addEventListener("click", () => {
    const key = button.dataset.listSort;
    if (!key) return;
    state.sort = state.sort.key === key
      ? { key, direction: state.sort.direction === "asc" ? "desc" : "asc" }
      : { key, direction: options.descendingKeys.includes(key) ? "desc" : "asc" };
    updateSortState();
    load(true);
  }));
  pageButtons.forEach((button) => button.addEventListener("click", () => {
    const action = button.dataset.listPage;
    if (action === "first") changePage(1);
    if (action === "previous") changePage(state.page - 1);
    if (action === "next") changePage(state.page + 1);
    if (action === "last") changePage(state.totalPages);
    if (action === "jump") changePage(Number(jump?.value || 1));
  }));
  jump?.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      changePage(Number(jump.value || 1));
    }
  });
  updateSortState();
  updatePager();
  return { apply: load };
}

function setupList(root, options) {
  if (!root) {
    return null;
  }

  if (options.remote) {
    return setupRemoteList(root, options);
  }

  const rows = Array.from(root.querySelectorAll(".list-row"));
  const tableBody = root.querySelector("tbody");
  const emptyRows = Array.from(root.querySelectorAll(".empty-row"));
  const emptyState = root.querySelector("[data-list-empty]");
  const summary = root.querySelector("[data-list-summary]");
  const indicator = root.querySelector("[data-list-page-indicator]");
  const jump = root.querySelector("[data-list-page-jump]");
  const pageSize = root.querySelector("[data-list-page-size]");
  const filters = Array.from(root.querySelectorAll("[data-list-filter]"));
  const sortButtons = Array.from(root.querySelectorAll("[data-list-sort]"));
  const pageButtons = Array.from(root.querySelectorAll("[data-list-page]"));
  const state = { page: 1, totalPages: 1, sort: { ...options.defaultSort } };

  const filterValue = (name) => {
    const input = filters.find((item) => item.dataset.listFilter === name);
    return input?.type === "checkbox" ? input.checked : (input?.value.trim() || "");
  };
  const sortValue = (row, key) => options.numericKeys.includes(key)
    ? parseLocalizedDecimal(row.dataset[key])
    : (row.dataset[key] || "");
  const compare = (a, b) => {
    const aValue = sortValue(a, state.sort.key);
    const bValue = sortValue(b, state.sort.key);
    const multiplier = state.sort.direction === "asc" ? 1 : -1;
    return typeof aValue === "number" && typeof bValue === "number"
      ? (aValue - bValue) * multiplier
      : aValue.localeCompare(bValue, "tr", { numeric: true, sensitivity: "base" }) * multiplier;
  };
  const updateSortState = () => {
    sortButtons.forEach((button) => {
      const isActive = button.dataset.listSort === state.sort.key;
      button.classList.toggle("is-active", isActive);
      button.classList.toggle("desc", isActive && state.sort.direction === "desc");
      button.setAttribute("aria-sort", isActive ? (state.sort.direction === "asc" ? "ascending" : "descending") : "none");
    });
  };
  const reorder = (orderedRows) => {
    if (!tableBody) return;
    const remainingRows = rows.filter((row) => !orderedRows.includes(row));
    [...orderedRows, ...remainingRows].forEach((row) => {
      tableBody.append(row);
      const detail = detailRowFor(row);
      if (detail) tableBody.append(detail);
    });
    emptyRows.forEach((row) => tableBody.append(row));
  };
  const apply = (resetPage = true) => {
    if (resetPage) state.page = 1;
    const matchingRows = rows.filter((row) => options.matches(row, filterValue)).sort(compare);
    reorder(matchingRows);
    const size = Number(pageSize?.value || 10);
    state.totalPages = Math.max(1, Math.ceil(matchingRows.length / size));
    state.page = Math.min(Math.max(1, state.page), state.totalPages);
    const start = (state.page - 1) * size;
    const pageRows = matchingRows.slice(start, start + size);

    rows.forEach((row) => {
      const isVisible = pageRows.includes(row);
      row.classList.toggle("d-none", !isVisible);
      detailRowFor(row)?.classList.toggle("d-none", !isVisible);
      if (!isVisible) closeDetail(row);
    });

    const first = matchingRows.length === 0 ? 0 : start + 1;
    const last = Math.min(start + size, matchingRows.length);
    if (summary) {
      summary.textContent = matchingRows.length === 0
        ? `0 ${options.label}`
        : `${matchingRows.length} ${options.label} içinden ${first}-${last} gösteriliyor`;
    }
    if (indicator) indicator.textContent = `${state.page} / ${state.totalPages}`;
    if (jump) {
      jump.max = state.totalPages.toString();
      jump.value = state.page.toString();
    }
    pageButtons.forEach((button) => {
      const action = button.dataset.listPage;
      button.disabled = (action === "first" || action === "previous") ? state.page <= 1
        : (action === "next" || action === "last") ? state.page >= state.totalPages
          : false;
    });
    emptyState?.classList.toggle("d-none", matchingRows.length !== 0 || rows.length === 0);
    options.afterApply?.(matchingRows);
  };
  const changePage = (page) => {
    state.page = Math.min(Math.max(1, page), state.totalPages);
    apply(false);
  };

  filters.forEach((input) => {
    input.addEventListener(input.tagName === "SELECT" || input.type === "checkbox" ? "change" : "input", () => apply(true));
  });
  pageSize?.addEventListener("change", () => apply(true));
  sortButtons.forEach((button) => button.addEventListener("click", () => {
    const key = button.dataset.listSort;
    if (!key) return;
    state.sort = state.sort.key === key
      ? { key, direction: state.sort.direction === "asc" ? "desc" : "asc" }
      : { key, direction: options.descendingKeys.includes(key) ? "desc" : "asc" };
    updateSortState();
    rows.forEach(closeDetail);
    apply(true);
  }));
  pageButtons.forEach((button) => button.addEventListener("click", () => {
    const action = button.dataset.listPage;
    if (action === "first") changePage(1);
    if (action === "previous") changePage(state.page - 1);
    if (action === "next") changePage(state.page + 1);
    if (action === "last") changePage(state.totalPages);
    if (action === "jump") changePage(Number(jump?.value || 1));
  }));
  jump?.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      changePage(Number(jump.value || 1));
    }
  });
  updateSortState();
  apply(true);
  return { apply };
}

setupList(document.querySelector('[data-list="products"]'), {
  defaultSort: { key: "year", direction: "desc" },
  descendingKeys: ["year", "term"],
  numericKeys: ["year", "term"],
  label: "ürün",
  matches: (row, value) => {
    const search = value("search").toUpperCase();
    const hasSub = row.dataset.hasSub === "true";
    return (!search || (row.dataset.search || "").includes(search))
      && (!value("year") || row.dataset.year === value("year"))
      && (!value("term") || row.dataset.term === value("term"))
      && (value("includeInactive") || row.dataset.active === "true")
      && (value("showMainProducts") || hasSub)
      && (value("showSubProducts") || !hasSub);
  },
});

setupList(document.querySelector('[data-list="productGamuts"]'), {
  defaultSort: { key: "group", direction: "asc" },
  descendingKeys: ["portfolioCount", "active"],
  numericKeys: ["portfolioCount", "active"],
  label: "ürün gamı",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

const productManagement = document.querySelector("[data-product-management]");
if (productManagement) {
  const picker = productManagement.querySelector(":scope > [data-segmented-control]");
  const buttons = Array.from(productManagement.querySelectorAll("[data-product-mode]"));
  const panels = Array.from(productManagement.querySelectorAll("[data-product-mode-panel]"));
  const setMode = (mode) => {
    setSegmentedControlValue(picker, mode);
    panels.forEach((panel) => panel.classList.toggle("d-none", panel.dataset.productModePanel !== mode));
    window.sessionStorage.setItem("bankurun.product-mode", mode);
  };
  buttons.forEach((button) => button.addEventListener("click", () => setMode(button.dataset.productMode || "products")));
  if (window.sessionStorage.getItem("bankurun.product-mode") === "gamuts") setMode("gamuts");
}

setupList(document.querySelector('[data-list="groups"]'), {
  defaultSort: { key: "groupNo", direction: "asc" },
  descendingKeys: ["branchCount", "gamutCount", "portfolioCount", "active"],
  numericKeys: ["branchCount", "gamutCount", "portfolioCount", "active"],
  label: "grup",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

setupList(document.querySelector('[data-list="branches"]'), {
  defaultSort: { key: "branchCode", direction: "asc" },
  descendingKeys: ["portfolioCount", "exclusionCount"],
  numericKeys: ["portfolioCount", "exclusionCount"],
  label: "şube",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

setupList(document.querySelector('[data-list="portfolios"]'), {
  defaultSort: { key: "portfolio", direction: "asc" },
  descendingKeys: ["active"],
  numericKeys: ["active"],
  label: "portföy",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

setupList(document.querySelector('[data-list="portfolioTypes"]'), {
  defaultSort: { key: "code", direction: "asc" },
  descendingKeys: ["portfolioCount"],
  numericKeys: ["portfolioCount"],
  label: "portföy tipi",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

setupList(document.querySelector('[data-list="productExclusions"]'), {
  defaultSort: { key: "period", direction: "desc" },
  descendingKeys: ["period"],
  numericKeys: ["period"],
  label: "şube ürün istisnası",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

const parameterManagement = document.querySelector("[data-parameter-management]");
if (parameterManagement) {
  const parameterRoot = parameterManagement.querySelector('[data-list="parameters"]');
  const targetRoot = parameterManagement.querySelector('[data-list="mainProductTargets"]');
  const modePicker = parameterManagement.querySelector("[data-segmented-control]");
  const modeButtons = Array.from(parameterManagement.querySelectorAll("[data-parameter-mode]"));
  const modePanels = Array.from(parameterManagement.querySelectorAll("[data-parameter-mode-panel]"));
  let activeParameterMode = "main";
  const setParameterMode = (mode) => {
    activeParameterMode = mode;
    setSegmentedControlValue(modePicker, mode);
    modePanels.forEach((panel) => panel.classList.toggle("d-none", panel.dataset.parameterModePanel !== mode));
    window.sessionStorage.setItem("bankurun.parameter-mode", mode);
  };
  modeButtons.forEach((button) => button.addEventListener("click", () => setParameterMode(button.dataset.parameterMode || "main")));

  const parameterList = setupList(parameterRoot, {
    defaultSort: { key: "year", direction: "desc" },
    descendingKeys: ["year", "term", "criterion", "active"],
    numericKeys: [],
    label: "parametre",
    colspan: 8,
    remote: async ({ state, filterValue, signal }) => {
      const params = new URLSearchParams({
        GroupId: filterValue("groupId"),
        Year: filterValue("year"),
        Term: filterValue("term"),
        Search: filterValue("search"),
        CalculationType: filterValue("calculationType"),
        SortKey: state.sort.key,
        SortDirection: state.sort.direction,
        Page: state.page.toString(),
        PageSize: parameterRoot.querySelector("[data-list-page-size]")?.value || "10"
      });
      const response = await fetch(`${parameterRoot.dataset.rowsUrl}?${params}`, { signal });
      if (!response.ok) throw new Error("Parametre listesi yüklenemedi.");
      return {
        html: await response.text(),
        totalCount: Number(response.headers.get("X-Total-Count") || 0),
        totalPages: Number(response.headers.get("X-Total-Pages") || 1),
        page: Number(response.headers.get("X-Page") || 1)
      };
    }
  });
  const targetList = setupList(targetRoot, {
    defaultSort: { key: "year", direction: "desc" },
    descendingKeys: ["year", "term", "target"],
    numericKeys: ["year", "term", "target"],
    label: "ana ürün hedefi",
    colspan: 9,
    remote: async ({ state, filterValue, signal }) => {
      const params = new URLSearchParams({
        GroupId: filterValue("groupId"),
        ProductGamutId: filterValue("productGamutId"),
        PortfolioId: filterValue("portfolioId"),
        MainProductId: filterValue("mainProductId"),
        Year: filterValue("year"),
        Term: filterValue("term"),
        Search: filterValue("search"),
        SortKey: state.sort.key,
        SortDirection: state.sort.direction,
        Page: state.page.toString(),
        PageSize: targetRoot?.querySelector("[data-list-page-size]")?.value || "10"
      });
      const response = await fetch(`${parameterManagement.dataset.mainTargetRowsUrl}?${params}`, { signal });
      if (!response.ok) throw new Error("Ana ürün hedefleri yüklenemedi.");
      return {
        html: await response.text(),
        totalCount: Number(response.headers.get("X-Total-Count") || 0),
        totalPages: Number(response.headers.get("X-Total-Pages") || 1),
        page: Number(response.headers.get("X-Page") || 1)
      };
    }
  });
  const loadMainTargetEditor = async (button) => {
    const detailId = button.closest("[data-detail-id]")?.dataset.detailId;
    const detailRow = detailId ? targetRoot?.querySelector(`[data-detail-for="${CSS.escape(detailId)}"]`) : null;
    const target = detailRow?.querySelector("[data-main-target-editor]");
    if (!target) return;
    if (target.dataset.loaded === "true") return;
    target.classList.add("is-loading");
    try {
      const params = new URLSearchParams({
        parameterId: button.dataset.parameterId,
        portfolioId: button.dataset.portfolioId
      });
      const response = await fetch(`${parameterManagement.dataset.mainTargetEditorUrl}?${params}`);
      if (!response.ok) throw new Error("Hedefler yüklenemedi.");
      target.innerHTML = await response.text();
      target.dataset.loaded = "true";
    } catch {
      target.innerHTML = '<div class="inline-empty-state">Ana ürün hedefleri yüklenemedi.</div>';
      target.dataset.loaded = "false";
    } finally {
      target.classList.remove("is-loading");
    }
  };

  const syncTargetDependencies = () => {
    if (!targetRoot) return;
    const group = targetRoot.querySelector("[data-target-group]")?.value || "";
    const gamut = targetRoot.querySelector("[data-target-gamut]")?.value || "";
    targetRoot.querySelectorAll("[data-target-gamut] option[data-group-id]").forEach((option) => {
      option.hidden = Boolean(group) && option.dataset.groupId !== group;
      option.disabled = option.hidden;
    });
    const gamutSelect = targetRoot.querySelector("[data-target-gamut]");
    if (gamutSelect?.selectedOptions[0]?.disabled) gamutSelect.value = "";
    targetRoot.querySelectorAll("[data-target-portfolio] option[data-group-id]").forEach((option) => {
      option.hidden = (Boolean(group) && option.dataset.groupId !== group)
        || (Boolean(gamut) && option.dataset.gamutId !== gamut);
      option.disabled = option.hidden;
    });
    const portfolioSelect = targetRoot.querySelector("[data-target-portfolio]");
    if (portfolioSelect?.selectedOptions[0]?.disabled) portfolioSelect.value = "";
  };
  parameterManagement.addEventListener("change", (event) => {
    if (event.target.matches?.("[data-target-group], [data-target-gamut]")) syncTargetDependencies();
  });
  parameterManagement.addEventListener("click", async (event) => {
    const detailButton = event.target.closest?.("[data-main-target-detail]");
    if (detailButton) {
      await loadMainTargetEditor(detailButton);
      return;
    }
    const button = event.target.closest?.("[data-show-main-targets]");
    if (!button || !targetRoot) return;
    const setFilter = (name, value) => {
      const input = targetRoot.querySelector(`[data-list-filter="${name}"]`);
      if (input) input.value = value || "";
    };
    setFilter("groupId", button.dataset.groupId);
    setFilter("year", button.dataset.year);
    setFilter("term", button.dataset.term);
    setFilter("mainProductId", button.dataset.mainProductId);
    syncTargetDependencies();
    setParameterMode("target");
    targetList?.apply(true);
  });
  syncTargetDependencies();
  const storedMode = window.sessionStorage.getItem("bankurun.parameter-mode");
  if (storedMode === "target") setParameterMode("target");
}

const dashboardRoot = document.querySelector("[data-dashboard]");
if (dashboardRoot) {
  const groupValue = dashboardRoot.querySelector('[data-dashboard-filter="groupId"]');
  const branchValue = dashboardRoot.querySelector('[data-dashboard-filter="branchId"]');
  const branchInput = branchValue?.closest(".generic-combo")?.querySelector("[data-combo-input]");
  const branchOptions = Array.from(branchValue?.closest(".generic-combo")?.querySelectorAll("[data-combo-option]") || []);
  const yearValue = dashboardRoot.querySelector('[data-dashboard-filter="year"]');
  const termValue = dashboardRoot.querySelector('[data-dashboard-filter="term"]');
  const productValue = dashboardRoot.querySelector('[data-dashboard-filter="mainProductId"]');
  const productInput = productValue?.closest(".generic-combo")?.querySelector("[data-combo-input]");
  const productOptions = Array.from(productValue?.closest(".generic-combo")?.querySelectorAll("[data-combo-option]") || []);
  const snapshot = dashboardRoot.querySelector("[data-dashboard-snapshot]");
  const modeStage = dashboardRoot.querySelector("[data-performance-mode-stage]");
  const modePicker = dashboardRoot.querySelector("[data-performance-mode-picker]");
  const modeButtons = Array.from(dashboardRoot.querySelectorAll("[data-performance-mode]"));
  const branchField = dashboardRoot.querySelector('[data-filter-field="branch"]');
  const productField = dashboardRoot.querySelector('[data-filter-field="product"]');
  const gamutField = dashboardRoot.querySelector('[data-filter-field="gamut"]');
  const portfolioTypeField = dashboardRoot.querySelector('[data-filter-field="portfolioType"]');
  const gamutValue = dashboardRoot.querySelector('[data-dashboard-filter="productGamutId"]');
  const portfolioTypeValue = dashboardRoot.querySelector('[data-dashboard-filter="portfolioTypeId"]');
  const clearButton = dashboardRoot.querySelector("[data-dashboard-clear]");
  const storageKey = "bankurun.performance-context";
  const defaultYear = yearValue?.value || "";
  const defaultTerm = termValue?.value || "";
  let requestController = null;
  let currentMode = dashboardRoot.dataset.defaultMode || "BranchProduct";
  let hasRestoredContext = false;

  const setComboValue = (value, input, option, emptyLabel = "") => {
    if (value) value.value = option?.dataset.id || "";
    if (input) input.value = option?.dataset.label || emptyLabel;
  };
  const syncTermContext = () => {
    const options = Array.from(termValue?.querySelectorAll("option") || []);
    options.forEach((option) => {
      const hidden = Boolean(option.value) && Boolean(yearValue?.value)
        && !(option.dataset.years || "").split(",").includes(yearValue.value);
      option.hidden = hidden;
      option.disabled = hidden;
    });
    if (termValue?.selectedOptions[0]?.disabled) {
      const available = options.find((option) => !option.disabled);
      if (available) termValue.value = available.value;
    }
  };
  const syncBranchContext = () => {
    const supportsBranch = currentMode === "BranchProduct" || currentMode === "Portfolio";
    const hasGroup = Boolean(groupValue?.value) && supportsBranch;
    if (branchInput) {
      branchInput.disabled = !hasGroup;
      branchInput.placeholder = hasGroup ? "Kod veya şube adı ara" : "Önce grup seçin";
    }
    branchOptions.forEach((option) => {
      const hidden = !hasGroup || option.dataset.groupId !== groupValue?.value;
      option.dataset.contextHidden = hidden.toString();
      option.classList.toggle("d-none", hidden);
    });
    const current = branchOptions.find((option) => option.dataset.id === branchValue?.value && option.dataset.contextHidden !== "true");
    if (!current) setComboValue(branchValue, branchInput, null, "");
  };
  const syncProductContext = () => {
    productOptions.forEach((option) => {
      option.dataset.contextHidden = "false";
      option.classList.remove("d-none");
    });
    const current = productOptions.find((option) => option.dataset.id === productValue?.value && option.dataset.contextHidden !== "true");
    if (!current) setComboValue(productValue, productInput, productOptions.find((option) => !option.dataset.id), "Tüm ana ürünler");
  };
  const syncPortfolioContext = () => {
    Array.from(gamutValue?.options || []).forEach((option) => {
      if (!option.value) return;
      const hidden = Boolean(groupValue?.value) && option.dataset.groupId !== groupValue.value;
      option.hidden = hidden;
      option.disabled = hidden;
    });
    if (gamutValue?.selectedOptions[0]?.disabled) gamutValue.value = "";
  };
  const syncModeUi = () => {
    setSegmentedControlValue(modePicker, currentMode);
    branchField?.classList.toggle("d-none", currentMode !== "BranchProduct" && currentMode !== "Portfolio");
    productField?.classList.toggle("d-none", currentMode !== "BranchProduct" && currentMode !== "MainProduct");
    gamutField?.classList.toggle("d-none", currentMode !== "Portfolio");
    portfolioTypeField?.classList.toggle("d-none", currentMode !== "Portfolio");
    syncBranchContext();
    syncPortfolioContext();
  };

  const setupPerformanceList = (name, defaultKey, label, defaultDirection = "asc") => setupList(snapshot?.querySelector(`[data-list="${name}"]`), {
    defaultSort: { key: defaultKey, direction: defaultDirection },
    descendingKeys: ["year", "term", "criterion", "target", "actual", "ratio", "hgo", "total", "rank", "subCount", "branchCount", "officialRank", "branchRank"],
    numericKeys: ["year", "term", "criterion", "target", "actual", "ratio", "hgo", "total", "rank", "subCount", "branchCount", "officialRank", "branchRank"],
    label,
    matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
  });
  const initializeSnapshot = () => {
    setupPerformanceList("performanceBranches", "total", "şube", "desc");
    setupPerformanceList("performanceBranchProducts", "total", "sonuç", "desc");
    setupPerformanceList("performanceMainProducts", "total", "ana ürün", "desc");
    setupPerformanceList("performancePortfolios", "total", "portföy", "desc");
  };

  try {
    const stored = JSON.parse(window.sessionStorage.getItem(storageKey) || "null");
    if (stored?.groupId && groupValue?.querySelector(`option[value="${stored.groupId}"]`)) groupValue.value = stored.groupId;
    if (stored && Object.hasOwn(stored, "year") && yearValue?.querySelector(`option[value="${stored.year}"]`)) yearValue.value = stored.year;
    syncTermContext();
    if (stored && Object.hasOwn(stored, "term") && termValue?.querySelector(`option[value="${stored.term}"]:not(:disabled)`)) termValue.value = stored.term;
    if (["Branch", "BranchProduct", "MainProduct", "Portfolio"].includes(stored?.mode)) currentMode = stored.mode;
    syncBranchContext();
    syncProductContext();
    const storedBranch = branchOptions.find((option) => option.dataset.id === String(stored?.branchId || "") && option.dataset.contextHidden !== "true");
    if (storedBranch) setComboValue(branchValue, branchInput, storedBranch);
    const storedProduct = productOptions.find((option) => option.dataset.id === String(stored?.mainProductId || "") && option.dataset.contextHidden !== "true");
    if (storedProduct) setComboValue(productValue, productInput, storedProduct, "Tüm ana ürünler");
    if (stored?.productGamutId && gamutValue?.querySelector(`option[value="${stored.productGamutId}"]:not(:disabled)`)) gamutValue.value = stored.productGamutId;
    if (stored?.portfolioTypeId && portfolioTypeValue?.querySelector(`option[value="${stored.portfolioTypeId}"]`)) portfolioTypeValue.value = stored.portfolioTypeId;
    hasRestoredContext = Boolean(stored) && (
      (groupValue?.value || "") !== ""
      || (branchValue?.value || "") !== ""
      || (yearValue?.value || "") !== defaultYear
      || (termValue?.value || "") !== defaultTerm
      || (productValue?.value || "") !== ""
      || (gamutValue?.value || "") !== ""
      || (portfolioTypeValue?.value || "") !== ""
      || currentMode !== (dashboardRoot.dataset.defaultMode || "BranchProduct")
    );
  } catch {
    window.sessionStorage.removeItem(storageKey);
    syncTermContext();
    syncBranchContext();
    syncProductContext();
  }

  const refreshDashboard = async () => {
    requestController?.abort();
    requestController = new AbortController();
    dashboardRoot.classList.add("is-loading");
    window.sessionStorage.setItem(storageKey, JSON.stringify({
      groupId: groupValue?.value || "",
      branchId: branchValue?.value || "",
      year: yearValue?.value || "",
      term: termValue?.value || "",
      mainProductId: productValue?.value || "",
      productGamutId: gamutValue?.value || "",
      portfolioTypeId: portfolioTypeValue?.value || "",
      mode: currentMode
    }));
    const params = new URLSearchParams({
      mode: currentMode,
      groupId: groupValue?.value || "",
      branchId: currentMode === "BranchProduct" || currentMode === "Portfolio" ? (branchValue?.value || "") : "",
      year: yearValue?.value || "",
      term: termValue?.value || "",
      mainProductId: currentMode === "BranchProduct" || currentMode === "MainProduct" ? (productValue?.value || "") : "",
      productGamutId: currentMode === "Portfolio" ? (gamutValue?.value || "") : "",
      portfolioTypeId: currentMode === "Portfolio" ? (portfolioTypeValue?.value || "") : ""
    });
    try {
      const response = await fetch(`${dashboardRoot.dataset.snapshotUrl}?${params}`, { signal: requestController.signal });
      if (!response.ok) throw new Error("Dashboard yüklenemedi.");
      if (snapshot) {
        const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (!reduceMotion) {
          if (modeStage) modeStage.style.height = `${snapshot.offsetHeight}px`;
          snapshot.classList.add("is-leaving");
          await new Promise((resolve) => window.setTimeout(resolve, 90));
        }
        snapshot.innerHTML = await response.text();
        initializeSnapshot();
        snapshot.classList.remove("is-leaving");
        snapshot.classList.add("is-entering");
        if (modeStage) modeStage.style.height = `${snapshot.offsetHeight}px`;
        window.requestAnimationFrame(() => snapshot.classList.remove("is-entering"));
        window.setTimeout(() => { if (modeStage) modeStage.style.height = "auto"; }, 220);
      }
    } catch (error) {
      if (error.name !== "AbortError" && snapshot) {
        snapshot.innerHTML = '<div class="surface-panel dashboard-empty-state">Dashboard verisi yüklenemedi. Lütfen yeniden deneyin.</div>';
      }
    } finally {
      dashboardRoot.classList.remove("is-loading");
    }
  };

  modeButtons.forEach((button) => button.addEventListener("click", () => {
    if (button.dataset.performanceMode === currentMode) return;
    currentMode = button.dataset.performanceMode || "BranchProduct";
    syncModeUi();
    refreshDashboard();
  }));

  groupValue?.addEventListener("change", () => {
    setComboValue(branchValue, branchInput, null, "");
    syncBranchContext();
    syncPortfolioContext();
    refreshDashboard();
  });
  branchValue?.addEventListener("change", refreshDashboard);
  yearValue?.addEventListener("change", () => { syncTermContext(); syncProductContext(); refreshDashboard(); });
  termValue?.addEventListener("change", () => { syncProductContext(); refreshDashboard(); });
  productValue?.addEventListener("change", refreshDashboard);
  gamutValue?.addEventListener("change", refreshDashboard);
  portfolioTypeValue?.addEventListener("change", refreshDashboard);
  clearButton?.addEventListener("click", () => {
    if (yearValue) yearValue.value = defaultYear;
    if (termValue) termValue.value = defaultTerm;
    if (groupValue) groupValue.value = "";
    setComboValue(branchValue, branchInput, null, "");
    setComboValue(productValue, productInput, productOptions.find((option) => !option.dataset.id), "Tüm ana ürünler");
    if (gamutValue) gamutValue.value = "";
    if (portfolioTypeValue) portfolioTypeValue.value = "";
    syncTermContext();
    syncBranchContext();
    syncProductContext();
    currentMode = "BranchProduct";
    syncModeUi();
    refreshDashboard();
  });
  snapshot?.addEventListener("click", async (event) => {
    const sectionButton = event.target.closest("[data-lazy-performance-section]");
    if (sectionButton) {
      const section = sectionButton.dataset.lazyPerformanceSection || "";
      const detail = sectionButton.closest(".performance-product-detail");
      const target = detail?.querySelector(`[data-lazy-performance-target="${CSS.escape(section)}"]`);
      if (!target || target.dataset.loading === "true") return;

      if (target.dataset.loaded === "true") {
        target.hidden = !target.hidden;
        sectionButton.setAttribute("aria-expanded", String(!target.hidden));
        return;
      }

      const url = sectionButton.dataset.lazyPerformanceUrl;
      if (!url) return;
      target.hidden = false;
      target.dataset.loading = "true";
      target.innerHTML = '<div class="inline-loading-state">Detay yükleniyor…</div>';
      sectionButton.setAttribute("aria-expanded", "true");
      try {
        const response = await fetch(url);
        if (!response.ok) throw new Error("Performans detayı yüklenemedi.");
        target.innerHTML = await response.text();
        target.dataset.loaded = "true";
      } catch {
        target.innerHTML = '<div class="inline-empty-state">Detay yüklenemedi. Lütfen yeniden deneyin.</div>';
      } finally {
        target.dataset.loading = "false";
      }
      return;
    }

    const inspectButton = event.target.closest("[data-inspect-branch]");
    if (inspectButton) {
      if (groupValue) groupValue.value = inspectButton.dataset.groupId || "";
      syncBranchContext();
      const option = branchOptions.find((item) => item.dataset.id === inspectButton.dataset.branchId);
      setComboValue(branchValue, branchInput, option, inspectButton.dataset.branchLabel || "");
      currentMode = "BranchProduct";
      syncModeUi();
      refreshDashboard();
      return;
    }
    const detailButton = event.target.closest("[data-monthly-detail]");
    if (!detailButton) return;
    const detailRow = detailRowFor(detailButton.closest(".list-row"));
    const target = detailRow?.querySelector("[data-monthly-detail-target]");
    if (!target || target.dataset.loaded === "true" || target.dataset.loading === "true") return;
    target.dataset.loading = "true";
    try {
      const params = new URLSearchParams({
        mainProductInstanceId: detailButton.dataset.productId || ""
      });
      let detailUrl = dashboardRoot.dataset.mainDetailUrl;
      if (detailButton.dataset.monthlyDetail === "portfolio") {
        params.delete("mainProductInstanceId");
        params.set("portfolioId", detailButton.dataset.portfolioId || "");
        params.set("year", detailButton.dataset.year || "");
        params.set("term", detailButton.dataset.term || "");
        detailUrl = dashboardRoot.dataset.portfolioDetailUrl;
      } else if (detailButton.dataset.monthlyDetail === "branch") {
        params.set("branchId", detailButton.dataset.branchId || "");
        detailUrl = dashboardRoot.dataset.branchDetailUrl;
      } else if (groupValue?.value) {
        params.set("groupId", groupValue.value);
      }
      const response = await fetch(`${detailUrl}?${params}`);
      if (!response.ok) throw new Error("Aylık detay yüklenemedi.");
      target.innerHTML = await response.text();
      target.dataset.loaded = "true";
    } catch {
      target.innerHTML = '<div class="inline-empty-state">Aylık detay yüklenemedi. Lütfen yeniden deneyin.</div>';
    } finally {
      target.dataset.loading = "false";
    }
  });
  syncTermContext();
  syncModeUi();
  initializeSnapshot();
  if (hasRestoredContext) refreshDashboard();
}

codeMode?.addEventListener("change", toggleManualCode);
manualCode?.addEventListener("input", refreshSuggestion);
productType?.addEventListener("change", () => {
  refreshSuggestion();
  toggleCreateProductFields();
});

toggleManualCode();
toggleCreateProductFields();

function setupOrganizationCreate() {
  const root = document.querySelector("[data-organization-create]");
  if (!root) return;

  root.querySelectorAll("[data-organization-create-type]").forEach((button) => button.addEventListener("click", () => {
    const type = button.dataset.organizationCreateType;
    root.querySelectorAll("[data-organization-create-form]").forEach((form) => {
      const active = form.dataset.organizationCreateForm === type;
      form.classList.toggle("d-none", !active);
      form.querySelectorAll("input, select").forEach((input) => input.disabled = !active);
    });
  }));

  const portfolioForm = root.querySelector("[data-portfolio-form]");
  const branch = portfolioForm?.querySelector("[data-portfolio-branch]");
  const gamut = portfolioForm?.querySelector("[data-portfolio-gamut]");
  const codeMode = portfolioForm?.querySelector("[data-portfolio-code-mode]");
  const code = portfolioForm?.querySelector("[data-portfolio-code]");
  const syncPortfolioCode = () => {
    if (!portfolioForm || !branch || !gamut || !code) return;
    const isAutomatic = codeMode?.value !== "false";
    const branchCode = branch.selectedOptions[0]?.dataset.branchCode || "";
    const gamutCode = gamut.selectedOptions[0]?.dataset.gamutCode || "";
    code.readOnly = isAutomatic;
    code.required = !isAutomatic;
    code.placeholder = isAutomatic && branchCode && gamutCode
      ? `P${branchCode}-${gamutCode}## otomatik üretilecek`
      : isAutomatic ? "Şube ve ürün gamı seçin" : "P0120-BI01";
    if (isAutomatic) code.value = "";
  };
  const syncPortfolioGamut = () => {
    if (!branch || !gamut) return;
    const groupId = branch.selectedOptions[0]?.dataset.groupId || "";
    Array.from(gamut.options).forEach((option) => {
      if (!option.value) return;
      const hidden = !groupId || option.dataset.groupId !== groupId;
      option.hidden = hidden;
      option.disabled = hidden;
    });
    gamut.disabled = !groupId || portfolioForm?.classList.contains("d-none");
    if (gamut.selectedOptions[0]?.disabled || (!gamut.value && groupId)) {
      gamut.value = Array.from(gamut.options).find((option) => option.value && !option.disabled)?.value || "";
    }
    syncPortfolioCode();
  };
  branch?.addEventListener("change", syncPortfolioGamut);
  gamut?.addEventListener("change", syncPortfolioCode);
  codeMode?.addEventListener("change", syncPortfolioCode);
  root.querySelector('[data-organization-create-type="portfolio"]')?.addEventListener("click", () => {
    window.setTimeout(syncPortfolioGamut, 0);
  });
  syncPortfolioGamut();
}

function setupAutomaticNumbers() {
  document.querySelectorAll("[data-number-mode]").forEach((root) => {
    const modeInput = root.querySelector("[data-number-mode-input]");
    const numberInput = root.querySelector("[data-number-input]");
    const syncNumberMode = () => {
      const isAutomatic = modeInput?.value === "true";
      if (modeInput) modeInput.value = isAutomatic.toString();
      if (numberInput) {
        numberInput.readOnly = isAutomatic;
        numberInput.required = !isAutomatic;
        numberInput.value = isAutomatic ? (numberInput.dataset.autoValue || "") : "";
        if (!isAutomatic) numberInput.focus();
      }
    };
    modeInput?.addEventListener("change", syncNumberMode);
    syncNumberMode();
  });
}

setupOrganizationCreate();
setupAutomaticNumbers();

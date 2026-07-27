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

function impactCountTone(label, result) {
  const normalized = String(label || "").toLocaleLowerCase("tr-TR");
  if (normalized.includes("korunacak") || normalized.includes("korunan")) return "is-preserved";
  if (result?.allowed === false) return "is-warning";
  const destructiveOperation = /(sil|kaldır|çıkar|pasifleştir)/i.test(result?.operation || "");
  const destructiveLabel = /(silinecek|hedef|gerçekleşme|atama|istisna|bağlantı|parametre|dönem kaydı)/i.test(normalized);
  return destructiveOperation || destructiveLabel ? "is-removed" : "is-neutral";
}

function renderConfirmImpact(result) {
  if (!confirmImpact) return;
  const counts = Array.isArray(result?.counts) ? result.counts : [];
  const warnings = Array.isArray(result?.warnings) ? result.warnings : [];
  const blockers = Array.isArray(result?.blockers) ? result.blockers : [];
  const visibleCounts = counts.filter((item) => Number(item.count) > 0);
  const rows = visibleCounts
    .map((item) => `<li><span>${escapeHtml(item.label)}</span><strong>${Number(item.count).toLocaleString("tr-TR")}</strong></li>`).join("");
  const impactNodes = visibleCounts.map((item) => {
    const tone = impactCountTone(item.label, result);
    return `<div class="impact-map-node ${tone}"><span>${escapeHtml(item.label)}</span><strong>${Number(item.count).toLocaleString("tr-TR")}</strong></div>`;
  }).join("");
  const relationshipMap = impactNodes
    ? `<div class="impact-relationship-map">
        <div class="impact-map-caption"><span>Mevcut bağlam</span><span>İşlem etkisi</span></div>
        <div class="impact-map-stage">
          <div class="impact-map-subject"><strong>${escapeHtml(result?.subject || "Seçili kayıt")}</strong><small>${escapeHtml(result?.operation || "Yönetim işlemi")}</small></div>
          <span class="impact-map-arrow" aria-hidden="true">→</span>
          <div class="impact-map-outcomes">${impactNodes}</div>
        </div>
        <div class="impact-map-legend" aria-label="Etki renkleri"><span class="is-removed">Silinen veya değişen</span><span class="is-preserved">Korunan</span><span class="is-neutral">Bağlı kayıt</span></div>
      </div>`
    : "";
  const notices = [...warnings.map((item) => `<p class="impact-warning">${escapeHtml(item)}</p>`),
    ...blockers.map((item) => `<p class="impact-blocker">${escapeHtml(item)}</p>`)].join("");
  confirmImpact.innerHTML = `<strong>${escapeHtml(result?.subject || "Etki özeti")}</strong><p>${escapeHtml(result?.summary || "")}</p>${relationshipMap}${rows ? `<ul class="impact-count-list">${rows}</ul>` : ""}${notices}`;
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
    if (form.matches("[data-product-scope-removal]") && form.dataset.impactTemplate && submitter) {
      const values = new FormData(form);
      const params = new URLSearchParams();
      ["Scope", "GroupId", "ProductGamutId", "BranchId", "MainProductId", "EffectiveFromYear", "EffectiveFromTerm"]
        .forEach((field) => {
          const value = values.get(field);
          if (value) params.set(field, value);
        });
      submitter.dataset.impactUrl = `${form.dataset.impactTemplate}?${params}`;
    } else if (form.matches("[data-group-product-removal]") && form.dataset.impactTemplate && submitter) {
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
    const currentController = new AbortController();
    requestController = currentController;
    root.classList.add("is-loading");
    try {
      const result = await options.remote({ state, filterValue, signal: currentController.signal });
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
      if (requestController === currentController) {
        root.classList.remove("is-loading");
      }
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
  const productFlowCache = new Map();
  const setMode = (mode) => {
    setSegmentedControlValue(picker, mode);
    panels.forEach((panel) => panel.classList.toggle("d-none", panel.dataset.productModePanel !== mode));
    window.sessionStorage.setItem("bankurun.product-mode", mode);
  };
  buttons.forEach((button) => button.addEventListener("click", () => setMode(button.dataset.productMode || "products")));
  if (window.sessionStorage.getItem("bankurun.product-mode") === "gamuts") setMode("gamuts");

  const removalForm = productManagement.querySelector("[data-product-scope-removal]");
  const removalScope = removalForm?.querySelector('input[name="Scope"]');
  const scopeHelp = removalForm?.querySelector("[data-removal-scope-help]");
  const helpText = {
    Group: "Grup seçimi, ürünün gruptaki bütün gam atamalarını seçilen dönemden itibaren kapatır.",
    ProductGamut: "Ürün gamı seçimi, yalnız seçilen gamdaki ürün atamasını kapatır.",
    Branch: "Şube seçimi, ürünü yalnız seçilen şubenin performans kapsamı dışında bırakır."
  };
  const syncRemovalScope = () => {
    const scope = removalScope?.value || "Group";
    removalForm?.querySelectorAll("[data-removal-scope-field]").forEach((field) => {
      const active = field.dataset.removalScopeField === scope;
      field.classList.toggle("d-none", !active);
      field.querySelectorAll("select, input").forEach((input) => { input.disabled = !active; });
    });
    if (scopeHelp) scopeHelp.textContent = helpText[scope] || "";
  };
  removalScope?.addEventListener("change", syncRemovalScope);
  syncRemovalScope();

  productManagement.addEventListener("click", async (event) => {
    const button = event.target.closest?.("[data-product-flow-load]");
    if (!button) return;
    const shell = button.closest("[data-product-flow-shell]");
    const target = shell?.querySelector("[data-product-flow-target]");
    const url = button.dataset.productFlowUrl;
    if (!target || !url) return;

    if (target.dataset.loaded === "true") {
      const willOpen = target.hidden;
      target.hidden = !willOpen;
      button.setAttribute("aria-expanded", willOpen.toString());
      button.textContent = willOpen
        ? "Besleyen ürünleri gizle"
        : "Besleyen ürünleri göster";
      return;
    }

    button.disabled = true;
    target.hidden = false;
    target.setAttribute("aria-busy", "true");
    target.innerHTML = '<div class="inline-empty-state">Besleyen ürünler yükleniyor…</div>';
    try {
      let html = productFlowCache.get(url);
      if (!html) {
        const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!response.ok) throw new Error("Product flow could not be loaded.");
        html = await response.text();
        productFlowCache.set(url, html);
      }
      target.innerHTML = html;
      target.dataset.loaded = "true";
      button.setAttribute("aria-expanded", "true");
      button.textContent = "Besleyen ürünleri gizle";
    } catch {
      target.innerHTML = '<div class="inline-empty-state">Besleyen ürünler yüklenemedi. Lütfen yeniden deneyin.</div>';
      target.hidden = false;
    } finally {
      target.removeAttribute("aria-busy");
      button.disabled = false;
    }
  });
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
  setupList(parameterRoot, {
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
  parameterManagement.addEventListener("click", (event) => {
    const button = event.target.closest?.("[data-open-targets]");
    if (!button) return;
    window.sessionStorage.setItem("bankurun.target-context", JSON.stringify({
      groupId: button.dataset.groupId || "",
      mainProductId: button.dataset.mainProductId || "",
      year: button.dataset.year || "",
      term: button.dataset.term || ""
    }));
    window.location.assign(button.dataset.targetsUrl);
  });
}

const targetManagement = document.querySelector("[data-target-management]");
if (targetManagement) {
  const targetRoot = targetManagement.querySelector('[data-list="targets"]');
  const pageEntryMode = targetManagement.querySelector("[data-target-entry-mode]");
  const importForm = targetManagement.querySelector("[data-target-import-form]");
  const importPreview = targetManagement.querySelector("[data-target-import-preview]");
  const selectionToggle = targetManagement.querySelector("[data-target-selection-toggle]");
  const selectionToolbar = targetManagement.querySelector("[data-target-selection-toolbar]");
  const filteredTargetExport = targetManagement.querySelector("[data-target-export]");
  const targetSelectionLimit = Number(targetManagement.dataset.targetSelectionLimit || 500);
  const selectedTargetContexts = new Set();
  let targetSelectionMode = false;

  const moneyFormatter = new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
  const targetInputValue = (input) => {
    const value = Number.parseFloat((input?.value || "0").replace(",", "."));
    return Number.isFinite(value) && value >= 0 ? value : 0;
  };
  const roundTargetValue = (value) =>
    Math.round((value + Number.EPSILON) * 100) / 100;
  const allocateTargetBlock = (value, count, isAverage) => {
    const roundedValue = roundTargetValue(value);
    if (isAverage) return Array(count).fill(roundedValue);
    const perMonth = roundTargetValue(roundedValue / count);
    return [
      ...Array(Math.max(0, count - 1)).fill(perMonth),
      roundTargetValue(roundedValue - (perMonth * Math.max(0, count - 1)))
    ];
  };
  const updateTargetAllocationPreview = (form) => {
    if (!form) return;
    const preview = form.querySelector("[data-target-allocation-preview]");
    if (!preview) return;
    const mode = form.querySelector("[data-target-editor-mode-input]")?.value || "SixMonth";
    const isAverage = form.dataset.targetCalculationType === "Average";
    let monthlyValues = [];
    if (mode === "SixMonth") {
      monthlyValues = allocateTargetBlock(
        targetInputValue(form.querySelector('[name="SixMonthTarget"]')),
        6,
        isAverage
      );
    } else if (mode === "ThreeMonth") {
      monthlyValues = [
        ...allocateTargetBlock(
          targetInputValue(form.querySelector('[name="FirstThreeMonthTarget"]')),
          3,
          isAverage
        ),
        ...allocateTargetBlock(
          targetInputValue(form.querySelector('[name="SecondThreeMonthTarget"]')),
          3,
          isAverage
        )
      ];
    } else {
      monthlyValues = Array.from(form.querySelectorAll('[name$=".TargetValue"]'))
        .map(targetInputValue);
    }
    while (monthlyValues.length < 6) monthlyValues.push(0);
    monthlyValues = monthlyValues.slice(0, 6);

    const maximum = Math.max(...monthlyValues, 0);
    const periodTarget = roundTargetValue(isAverage
      ? monthlyValues.reduce((sum, value) => sum + value, 0) / monthlyValues.length
      : monthlyValues.reduce((sum, value) => sum + value, 0));
    const valueElements = preview.querySelectorAll("[data-target-allocation-value]");
    const barElements = preview.querySelectorAll("[data-target-allocation-bar]");
    monthlyValues.forEach((value, index) => {
      const formatted = moneyFormatter.format(value);
      if (valueElements[index]) {
        valueElements[index].textContent = formatted;
        valueElements[index].title = formatted;
      }
      if (barElements[index]) {
        const height = maximum > 0 ? Math.max(4, (value / maximum) * 100) : 0;
        barElements[index].style.setProperty("--target-bar-height", `${height}%`);
      }
    });
    const total = preview.querySelector("[data-target-allocation-total]");
    if (total) total.textContent = moneyFormatter.format(periodTarget);
    const explanation = preview.querySelector("[data-target-allocation-explanation]");
    if (explanation) {
      explanation.textContent = mode === "Monthly"
        ? `Aylık değerler aynen kaydedilir; dönem hedefi ${isAverage ? "altı ayın ortalamasıdır" : "altı ayın toplamıdır"}.`
        : isAverage
          ? `${mode === "ThreeMonth" ? "Her üç aylık bloktaki" : "Girilen"} ortalama hedef ilgili aylara aynen uygulanır.`
          : `${mode === "ThreeMonth" ? "Her üç aylık blok" : "Dönem tutarı"} aylara eşit bölünür; kuruş farkı bloğun son ayına eklenir.`;
    }
    preview.querySelector("[data-target-allocation-chart]")?.setAttribute(
      "aria-label",
      `Altı aylık hedef dağılımı. Dönem hedefi ${moneyFormatter.format(periodTarget)}.`
    );
  };

  const currentPageTargetCheckboxes = () =>
    Array.from(targetRoot?.querySelectorAll("[data-target-select-row]") || []);
  const updateTargetSelectionUi = () => {
    targetRoot?.classList.toggle("is-target-selection-mode", targetSelectionMode);
    if (selectionToolbar) selectionToolbar.hidden = !targetSelectionMode;
    if (filteredTargetExport) filteredTargetExport.hidden = targetSelectionMode;
    if (selectionToggle) {
      selectionToggle.classList.toggle("is-active", targetSelectionMode);
      selectionToggle.setAttribute("aria-pressed", targetSelectionMode.toString());
      selectionToggle.textContent = targetSelectionMode
        ? "Seçimli aktarımı kapat"
        : "Satır seçerek aktar";
    }

    const pageCheckboxes = currentPageTargetCheckboxes();
    pageCheckboxes.forEach((checkbox) => {
      checkbox.checked = selectedTargetContexts.has(checkbox.value);
    });
    const selectedOnPage = pageCheckboxes.filter((checkbox) => checkbox.checked).length;
    const selectPage = targetRoot?.querySelector("[data-target-select-page]");
    if (selectPage) {
      selectPage.checked = pageCheckboxes.length > 0 && selectedOnPage === pageCheckboxes.length;
      selectPage.indeterminate = selectedOnPage > 0 && selectedOnPage < pageCheckboxes.length;
      selectPage.disabled = pageCheckboxes.length === 0;
    }

    const count = targetManagement.querySelector("[data-target-selection-count]");
    if (count) count.textContent = `${selectedTargetContexts.size} satır seçildi`;
    const feedback = targetManagement.querySelector("[data-target-selection-feedback]");
    if (feedback) {
      const limitReached = selectedTargetContexts.size >= targetSelectionLimit;
      feedback.textContent = limitReached
        ? `${targetSelectionLimit} satırlık seçim sınırına ulaştınız. Yeni bir satır seçmek için mevcut seçimlerden birini kaldırın.`
        : `Filtre veya sayfa değiştirseniz de seçimleriniz korunur; en fazla ${targetSelectionLimit} satır aktarılır.`;
      feedback.classList.toggle("is-limit", limitReached);
    }
    targetManagement.querySelectorAll("[data-target-export-selected], [data-target-selection-clear]")
      .forEach((button) => { button.disabled = selectedTargetContexts.size === 0; });
  };
  const submitSelectedTargetExport = () => {
    if (!selectedTargetContexts.size || !targetManagement.dataset.exportSelectedUrl) return;
    const form = document.createElement("form");
    form.method = "post";
    form.action = targetManagement.dataset.exportSelectedUrl;
    form.hidden = true;
    const addValue = (name, value) => {
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = name;
      input.value = value;
      form.append(input);
    };
    const token = targetManagement.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) addValue("__RequestVerificationToken", token);
    addValue("EntryMode", pageEntryMode?.value || "SixMonth");
    selectedTargetContexts.forEach((contextKey) => addValue("ContextKeys", contextKey));
    document.body.append(form);
    form.submit();
    window.setTimeout(() => form.remove(), 0);
  };

  const targetList = setupList(targetRoot, {
    defaultSort: { key: "year", direction: "desc" },
    descendingKeys: ["year", "term", "target", "status"],
    numericKeys: ["year", "term", "target", "status"],
    label: "hedef bağlamı",
    colspan: 12,
    afterLoad: () => updateTargetSelectionUi(),
    remote: async ({ state, filterValue, signal }) => {
      const params = new URLSearchParams({
        GroupId: filterValue("groupId"),
        BranchId: filterValue("branchId"),
        ProductGamutId: filterValue("productGamutId"),
        PortfolioId: filterValue("portfolioId"),
        MainProductId: filterValue("mainProductId"),
        Year: filterValue("year"),
        Term: filterValue("term"),
        CompletionStatus: filterValue("completionStatus"),
        Search: filterValue("search"),
        SortKey: state.sort.key,
        SortDirection: state.sort.direction,
        Page: state.page.toString(),
        PageSize: targetRoot?.querySelector("[data-list-page-size]")?.value || "10"
      });
      const response = await fetch(`${targetManagement.dataset.rowsUrl}?${params}`, { signal });
      if (!response.ok) throw new Error("Hedef listesi yüklenemedi.");
      return {
        html: await response.text(),
        totalCount: Number(response.headers.get("X-Total-Count") || 0),
        totalPages: Number(response.headers.get("X-Total-Pages") || 1),
        page: Number(response.headers.get("X-Page") || 1)
      };
    }
  });

  const filterValue = (name) => targetRoot?.querySelector(`[data-list-filter="${name}"]`)?.value || "";
  const currentQuery = () => new URLSearchParams({
    GroupId: filterValue("groupId"),
    BranchId: filterValue("branchId"),
    ProductGamutId: filterValue("productGamutId"),
    PortfolioId: filterValue("portfolioId"),
    MainProductId: filterValue("mainProductId"),
    Year: filterValue("year"),
    Term: filterValue("term"),
    CompletionStatus: filterValue("completionStatus"),
    Search: filterValue("search"),
    EntryMode: pageEntryMode?.value || "SixMonth"
  });

  const syncTargetDependencies = () => {
    const group = filterValue("groupId");
    const branch = filterValue("branchId");
    const gamut = filterValue("productGamutId");
    const syncSelect = (selector, isHidden) => {
      const select = targetRoot?.querySelector(selector);
      select?.querySelectorAll("option[value]").forEach((option) => {
        if (!option.value) return;
        option.hidden = isHidden(option);
        option.disabled = option.hidden;
      });
      if (select?.selectedOptions[0]?.disabled) select.value = "";
      return select?.value || "";
    };
    syncSelect("[data-target-branch]", (option) => Boolean(group) && option.dataset.groupId !== group);
    const activeBranch = filterValue("branchId");
    syncSelect("[data-target-gamut]", (option) => Boolean(group) && option.dataset.groupId !== group);
    const activeGamut = filterValue("productGamutId");
    syncSelect("[data-target-portfolio]", (option) =>
      (Boolean(group) && option.dataset.groupId !== group)
      || (Boolean(activeBranch) && option.dataset.branchId !== activeBranch)
      || (Boolean(activeGamut) && option.dataset.gamutId !== activeGamut));
  };

  const loadTargetEditor = async (button) => {
    const detailId = button.closest("[data-detail-id]")?.dataset.detailId;
    const detailRow = detailId
      ? targetRoot?.querySelector(`[data-detail-for="${CSS.escape(detailId)}"]`)
      : null;
    const editor = detailRow?.querySelector("[data-target-editor]");
    if (!editor || editor.dataset.loaded === "true") return;
    editor.classList.add("is-loading");
    try {
      const params = new URLSearchParams({
        parameterId: button.dataset.parameterId,
        portfolioId: button.dataset.portfolioId
      });
      const response = await fetch(`${targetManagement.dataset.editorUrl}?${params}`);
      if (!response.ok) throw new Error("Hedef editörü yüklenemedi.");
      editor.innerHTML = await response.text();
      editor.dataset.loaded = "true";
      setupSegmentedControls(editor);
      setSegmentedControlValue(
        editor.querySelector("[data-target-editor-mode-input]")?.closest("[data-segmented-control]"),
        pageEntryMode?.value || "SixMonth",
        true
      );
      updateTargetAllocationPreview(editor.querySelector("[data-period-target-form]"));
    } catch {
      editor.innerHTML = '<div class="inline-empty-state">Hedef editörü yüklenemedi. Lütfen yeniden deneyin.</div>';
      editor.dataset.loaded = "false";
    } finally {
      editor.classList.remove("is-loading");
    }
  };

  targetManagement.addEventListener("change", (event) => {
    if (event.target.matches?.("[data-target-group], [data-target-branch], [data-target-gamut]")) {
      syncTargetDependencies();
    }
    if (event.target.matches?.("[data-target-editor-mode-input]")) {
      const form = event.target.closest("[data-period-target-form]");
      form?.querySelectorAll("[data-target-entry-panel]").forEach((panel) => {
        panel.hidden = panel.dataset.targetEntryPanel !== event.target.value;
      });
      updateTargetAllocationPreview(form);
    }
    if (event.target.matches?.("[data-target-entry-mode]")) {
      window.sessionStorage.setItem("bankurun.target-entry-mode", event.target.value);
      targetRoot?.querySelectorAll("[data-target-editor-mode-input]").forEach((input) => {
        setSegmentedControlValue(
          input.closest("[data-segmented-control]"),
          event.target.value,
          true
        );
      });
    }
    if (event.target.matches?.("[data-target-select-row]")) {
      if (event.target.checked
        && !selectedTargetContexts.has(event.target.value)
        && selectedTargetContexts.size >= targetSelectionLimit) {
        event.target.checked = false;
      } else if (event.target.checked) selectedTargetContexts.add(event.target.value);
      else selectedTargetContexts.delete(event.target.value);
      updateTargetSelectionUi();
    }
    if (event.target.matches?.("[data-target-select-page]")) {
      currentPageTargetCheckboxes().forEach((checkbox) => {
        if (event.target.checked) {
          if (selectedTargetContexts.has(checkbox.value)
            || selectedTargetContexts.size < targetSelectionLimit) {
            selectedTargetContexts.add(checkbox.value);
          }
        } else {
          selectedTargetContexts.delete(checkbox.value);
        }
      });
      updateTargetSelectionUi();
    }
  });

  targetManagement.addEventListener("input", (event) => {
    const form = event.target.closest?.("[data-period-target-form]");
    if (form && event.target.matches('input[type="number"]')) {
      updateTargetAllocationPreview(form);
    }
  });

  targetManagement.addEventListener("click", async (event) => {
    const detailButton = event.target.closest?.("[data-target-detail]");
    if (detailButton) {
      await loadTargetEditor(detailButton);
      return;
    }
    if (event.target.closest?.("[data-target-export]")) {
      window.location.assign(`${targetManagement.dataset.exportUrl}?${currentQuery()}`);
      return;
    }
    if (event.target.closest?.("[data-target-selection-toggle]")) {
      targetSelectionMode = !targetSelectionMode;
      updateTargetSelectionUi();
      return;
    }
    if (event.target.closest?.("[data-target-selection-clear]")) {
      selectedTargetContexts.clear();
      updateTargetSelectionUi();
      return;
    }
    if (event.target.closest?.("[data-target-export-selected]")) {
      submitSelectedTargetExport();
      return;
    }
    if (event.target.closest?.("[data-target-missing-template]")) {
      window.location.assign(`${targetManagement.dataset.missingTemplateUrl}?${currentQuery()}`);
      return;
    }
    if (event.target.closest?.("[data-target-template]")) {
      const params = new URLSearchParams({ EntryMode: pageEntryMode?.value || "SixMonth" });
      window.location.assign(`${targetManagement.dataset.templateUrl}?${params}`);
    }
  });

  importForm?.addEventListener("submit", async (event) => {
    event.preventDefault();
    importPreview.innerHTML = '<div class="inline-loading-state">Excel doğrulanıyor…</div>';
    const submit = importForm.querySelector('button[type="submit"]');
    if (submit) submit.disabled = true;
    try {
      const response = await fetch(importForm.action, {
        method: "POST",
        body: new FormData(importForm)
      });
      if (!response.ok) throw new Error("Excel önizlemesi alınamadı.");
      importPreview.innerHTML = await response.text();
    } catch {
      importPreview.innerHTML = '<div class="alert alert-danger mb-0">Excel önizlemesi alınamadı. Lütfen yeniden deneyin.</div>';
    } finally {
      if (submit) submit.disabled = false;
    }
  });

  const storedEntryMode = window.sessionStorage.getItem("bankurun.target-entry-mode");
  if (storedEntryMode && pageEntryMode) {
    pageEntryMode.value = storedEntryMode;
    setSegmentedControlValue(pageEntryMode.closest("[data-segmented-control]"), storedEntryMode);
  }
  const storedContext = window.sessionStorage.getItem("bankurun.target-context");
  if (storedContext) {
    try {
      const context = JSON.parse(storedContext);
      Object.entries(context).forEach(([name, value]) => {
        const filter = targetRoot?.querySelector(`[data-list-filter="${name}"]`);
        if (filter) filter.value = value || "";
      });
      window.sessionStorage.removeItem("bankurun.target-context");
      syncTargetDependencies();
      targetList?.apply(true);
    } catch {
      window.sessionStorage.removeItem("bankurun.target-context");
    }
  }
  syncTargetDependencies();
  updateTargetSelectionUi();
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
  const refreshButton = dashboardRoot.querySelector("[data-dashboard-refresh]");
  const storageKey = "bankurun.performance-context";
  const defaultYear = yearValue?.value || "";
  const defaultTerm = termValue?.value || "";
  const panelCache = new Map();
  const detailCache = new Map();
  const detailRequestControllers = new Set();
  const cacheLifetime = 60_000;
  const maxPanelCacheEntries = 8;
  const maxDetailCacheEntries = 32;
  let dashboardRequestController = null;
  let dashboardRequestGeneration = 0;
  let detailCacheGeneration = 0;
  let dashboardRefreshTimer = null;
  let activePanelKey = "";
  let currentMode = dashboardRoot.dataset.defaultMode || "BranchProduct";

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
    if (supportsBranch && !current) setComboValue(branchValue, branchInput, null, "");
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

  const putCache = (cache, key, value, limit, expiresAt = Date.now() + cacheLifetime) => {
    cache.delete(key);
    if (expiresAt <= Date.now()) return;
    cache.set(key, { value, expiresAt });
    while (cache.size > limit) {
      cache.delete(cache.keys().next().value);
    }
  };
  const takeCache = (cache, key) => {
    const entry = cache.get(key);
    if (!entry) return null;
    cache.delete(key);
    if (entry.expiresAt <= Date.now()) return null;
    cache.set(key, entry);
    return entry.value;
  };
  const responseCacheExpiresAt = (response) => {
    const remaining = Number(response.headers.get("X-Performance-Cache-Max-Age-Ms"));
    const boundedRemaining = Number.isFinite(remaining)
      ? Math.min(cacheLifetime, Math.max(0, remaining))
      : cacheLifetime;
    return Date.now() + boundedRemaining;
  };
  const clearCaches = () => {
    panelCache.clear();
    detailCache.clear();
    detailCacheGeneration += 1;
    detailRequestControllers.forEach((controller) => controller.abort());
    detailRequestControllers.clear();
  };
  const scopeParams = (mode = currentMode) => new URLSearchParams({
    Mode: mode,
    GroupId: groupValue?.value || "",
    BranchId: mode === "BranchProduct" || mode === "Portfolio" ? (branchValue?.value || "") : "",
    Year: yearValue?.value || "",
    Term: termValue?.value || "",
    MainProductId: mode === "BranchProduct" || mode === "MainProduct" ? (productValue?.value || "") : "",
    ProductGamutId: mode === "Portfolio" ? (gamutValue?.value || "") : "",
    PortfolioTypeId: mode === "Portfolio" ? (portfolioTypeValue?.value || "") : ""
  });
  const panelKey = () => Array.from(scopeParams().entries())
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([key, value]) => `${key}=${value}`)
    .join("&");
  const persistContext = () => {
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
  };
  const performanceListConfig = (mode) => ({
    Branch: { name: "performanceBranches", label: "şube", colspan: 10, pageSize: 25 },
    BranchProduct: { name: "performanceBranchProducts", label: "sonuç", colspan: 13, pageSize: 25 },
    MainProduct: { name: "performanceMainProducts", label: "ana ürün", colspan: 13, pageSize: 10 },
    Portfolio: { name: "performancePortfolios", label: "portföy", colspan: 14, pageSize: 25 }
  })[mode];
  const initializeSnapshot = () => {
    const panel = snapshot?.querySelector("[data-dashboard-mode]");
    const mode = panel?.dataset.dashboardMode || currentMode;
    const config = performanceListConfig(mode);
    const listRoot = config ? panel?.querySelector(`[data-list="${config.name}"]`) : null;
    if (!listRoot || listRoot.dataset.remoteListReady === "true") return;
    listRoot.dataset.remoteListReady = "true";
    let rowsRequestGeneration = 0;
    const closeVisiblePerformanceDetails = () => {
      listRoot.querySelectorAll(".list-row").forEach(closeDetail);
    };
    listRoot.addEventListener("input", (event) => {
      if (event.target.matches("[data-list-filter]")) closeVisiblePerformanceDetails();
    });
    listRoot.addEventListener("change", (event) => {
      if (event.target.matches("[data-list-filter], [data-list-page-size]")) closeVisiblePerformanceDetails();
    });
    listRoot.addEventListener("click", (event) => {
      if (event.target.closest("[data-list-sort], [data-list-page]")) closeVisiblePerformanceDetails();
    });
    setupList(listRoot, {
      defaultSort: { key: "total", direction: "desc" },
      descendingKeys: ["year", "term", "criterion", "target", "actual", "ratio", "hgo", "total", "rank", "subCount", "branchCount", "officialRank", "branchRank"],
      numericKeys: ["year", "term", "criterion", "target", "actual", "ratio", "hgo", "total", "rank", "subCount", "branchCount", "officialRank", "branchRank"],
      label: config.label,
      colspan: config.colspan,
      remote: async ({ state, filterValue, signal }) => {
        const requestGeneration = ++rowsRequestGeneration;
        const params = scopeParams(mode);
        params.set("Search", filterValue("search"));
        params.set("SortKey", state.sort.key);
        params.set("SortDirection", state.sort.direction);
        params.set("Page", state.page.toString());
        params.set("PageSize", listRoot.querySelector("[data-list-page-size]")?.value || config.pageSize.toString());
        params.set("ForceRefresh", "false");
        listRoot.setAttribute("aria-busy", "true");
        try {
          const response = await fetch(`${dashboardRoot.dataset.rowsUrl}?${params}`, { signal });
          if (!response.ok) throw new Error("Performans satırları yüklenemedi.");
          const html = await response.text();
          if (requestGeneration !== rowsRequestGeneration) {
            throw new DOMException("Geçersiz performans cevabı.", "AbortError");
          }
          panel.dataset.performanceCacheExpiresAt = String(responseCacheExpiresAt(response));
          const totalCount = Number(response.headers.get("X-Total-Count") || 0);
          const countChip = panel.querySelector("[data-performance-result-count]");
          if (countChip) countChip.textContent = `${totalCount} sonuç`;
          return {
            html,
            totalCount,
            totalPages: Number(response.headers.get("X-Total-Pages") || 1),
            page: Number(response.headers.get("X-Page") || 1)
          };
        } finally {
          if (requestGeneration === rowsRequestGeneration) {
            listRoot.setAttribute("aria-busy", "false");
          }
        }
      }
    });
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
    syncPortfolioContext();
    if (stored?.productGamutId && gamutValue?.querySelector(`option[value="${stored.productGamutId}"]:not(:disabled)`)) gamutValue.value = stored.productGamutId;
    if (stored?.portfolioTypeId && portfolioTypeValue?.querySelector(`option[value="${stored.portfolioTypeId}"]`)) portfolioTypeValue.value = stored.portfolioTypeId;
  } catch {
    window.sessionStorage.removeItem(storageKey);
    syncTermContext();
    syncBranchContext();
    syncProductContext();
  }

  const transitionSnapshot = async (
    content,
    targetKey,
    generation,
    isCached,
    cacheExpiresAt = Date.now() + cacheLifetime) => {
    if (!snapshot) return;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const outgoing = snapshot.firstElementChild;
    if (!reduceMotion && outgoing) {
      if (modeStage) modeStage.style.height = `${snapshot.offsetHeight}px`;
      snapshot.classList.add("is-leaving");
      await new Promise((resolve) => window.setTimeout(resolve, 90));
    }
    snapshot.classList.remove("is-leaving");
    if (generation !== dashboardRequestGeneration) {
      if (modeStage) modeStage.style.height = "auto";
      throw new DOMException("Geçersiz performans cevabı.", "AbortError");
    }
    if (outgoing && activePanelKey && activePanelKey !== targetKey) {
      const expiresAt = Number(outgoing.dataset.performanceCacheExpiresAt || 0);
      putCache(panelCache, activePanelKey, outgoing, maxPanelCacheEntries, expiresAt);
    }
    if (isCached) {
      snapshot.replaceChildren(content);
    } else {
      snapshot.innerHTML = content;
      const loadedPanel = snapshot.firstElementChild;
      if (loadedPanel) {
        loadedPanel.dataset.performanceCacheExpiresAt = String(cacheExpiresAt);
      }
      initializeSnapshot();
    }
    activePanelKey = targetKey;
    if (!reduceMotion) {
      snapshot.classList.add("is-entering");
      if (modeStage) modeStage.style.height = `${snapshot.offsetHeight}px`;
      window.requestAnimationFrame(() => snapshot.classList.remove("is-entering"));
      window.setTimeout(() => {
        if (modeStage && generation === dashboardRequestGeneration) modeStage.style.height = "auto";
      }, 220);
    } else if (modeStage) {
      modeStage.style.height = "auto";
    }
  };

  const refreshDashboard = async ({ forceRefresh = false, useCache = true } = {}) => {
    window.clearTimeout(dashboardRefreshTimer);
    dashboardRefreshTimer = null;
    const generation = ++dashboardRequestGeneration;
    dashboardRequestController?.abort();
    dashboardRequestController = new AbortController();
    const targetKey = panelKey();
    persistContext();

    if (useCache && !forceRefresh && targetKey !== activePanelKey) {
      const cachedPanel = takeCache(panelCache, targetKey);
      if (cachedPanel) {
        dashboardRoot.setAttribute("aria-busy", "true");
        try {
          await transitionSnapshot(cachedPanel, targetKey, generation, true);
        } catch (error) {
          if (error.name !== "AbortError") throw error;
        } finally {
          if (generation === dashboardRequestGeneration) {
            dashboardRoot.classList.remove("is-loading");
            dashboardRoot.setAttribute("aria-busy", "false");
            refreshButton?.removeAttribute("disabled");
          }
        }
        return;
      }
    }

    dashboardRoot.classList.add("is-loading");
    dashboardRoot.setAttribute("aria-busy", "true");
    refreshButton?.setAttribute("disabled", "");
    const config = performanceListConfig(currentMode);
    const params = scopeParams();
    params.set("Search", "");
    params.set("SortKey", "total");
    params.set("SortDirection", "desc");
    params.set("Page", "1");
    params.set("PageSize", config.pageSize.toString());
    params.set("ForceRefresh", String(forceRefresh));
    try {
      const response = await fetch(`${dashboardRoot.dataset.snapshotUrl}?${params}`, {
        signal: dashboardRequestController.signal,
        headers: forceRefresh ? { "X-Performance-Force-Refresh": "1" } : {}
      });
      if (!response.ok) throw new Error("Dashboard yüklenemedi.");
      const html = await response.text();
      if (generation !== dashboardRequestGeneration) {
        throw new DOMException("Geçersiz performans cevabı.", "AbortError");
      }
      await transitionSnapshot(
        html,
        targetKey,
        generation,
        false,
        responseCacheExpiresAt(response));
    } catch (error) {
      if (error.name !== "AbortError" && snapshot && generation === dashboardRequestGeneration) {
        snapshot.classList.remove("is-leaving", "is-entering");
        if (modeStage) modeStage.style.height = "auto";
        snapshot.innerHTML = '<div class="surface-panel dashboard-empty-state">Dashboard verisi yüklenemedi. Lütfen yeniden deneyin.</div>';
        activePanelKey = targetKey;
      }
    } finally {
      if (generation === dashboardRequestGeneration) {
        dashboardRoot.classList.remove("is-loading");
        dashboardRoot.setAttribute("aria-busy", "false");
        refreshButton?.removeAttribute("disabled");
      }
    }
  };
  const scheduleDashboardRefresh = () => {
    window.clearTimeout(dashboardRefreshTimer);
    dashboardRefreshTimer = window.setTimeout(() => refreshDashboard(), 250);
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
    scheduleDashboardRefresh();
  });
  branchValue?.addEventListener("change", scheduleDashboardRefresh);
  yearValue?.addEventListener("change", () => { syncTermContext(); syncProductContext(); scheduleDashboardRefresh(); });
  termValue?.addEventListener("change", () => { syncProductContext(); scheduleDashboardRefresh(); });
  productValue?.addEventListener("change", scheduleDashboardRefresh);
  gamutValue?.addEventListener("change", scheduleDashboardRefresh);
  portfolioTypeValue?.addEventListener("change", scheduleDashboardRefresh);
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
  refreshButton?.addEventListener("click", () => {
    clearCaches();
    activePanelKey = "";
    refreshDashboard({ forceRefresh: true, useCache: false });
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
      const cachedHtml = takeCache(detailCache, url);
      if (cachedHtml !== null) {
        target.innerHTML = cachedHtml;
        target.dataset.loaded = "true";
        sectionButton.setAttribute("aria-expanded", "true");
        return;
      }
      target.dataset.loading = "true";
      target.innerHTML = '<div class="inline-loading-state">Detay yükleniyor…</div>';
      sectionButton.setAttribute("aria-expanded", "true");
      const requestGeneration = detailCacheGeneration;
      const detailController = new AbortController();
      detailRequestControllers.add(detailController);
      try {
        const response = await fetch(url, { signal: detailController.signal });
        if (!response.ok) throw new Error("Performans detayı yüklenemedi.");
        const html = await response.text();
        if (requestGeneration !== detailCacheGeneration) {
          throw new DOMException("Geçersiz performans detayı.", "AbortError");
        }
        putCache(detailCache, url, html, maxDetailCacheEntries);
        target.innerHTML = html;
        target.dataset.loaded = "true";
      } catch (error) {
        if (error.name !== "AbortError") {
          target.innerHTML = '<div class="inline-empty-state">Detay yüklenemedi. Lütfen yeniden deneyin.</div>';
        }
      } finally {
        detailRequestControllers.delete(detailController);
        target.dataset.loading = "false";
      }
      return;
    }

    const inspectButton = event.target.closest("[data-inspect-branch]");
    if (inspectButton) {
      if (groupValue) groupValue.value = inspectButton.dataset.groupId || "";
      if (yearValue && yearValue.querySelector(`option[value="${inspectButton.dataset.year}"]`)) {
        yearValue.value = inspectButton.dataset.year || "";
      }
      syncTermContext();
      if (termValue && termValue.querySelector(`option[value="${inspectButton.dataset.term}"]:not(:disabled)`)) {
        termValue.value = inspectButton.dataset.term || "";
      }
      syncBranchContext();
      const option = branchOptions.find((item) => item.dataset.id === inspectButton.dataset.branchId);
      setComboValue(branchValue, branchInput, option, inspectButton.dataset.branchLabel || "");
      currentMode = "BranchProduct";
      syncModeUi();
      refreshDashboard();
      return;
    }
  });
  syncTermContext();
  syncModeUi();
  refreshDashboard();
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

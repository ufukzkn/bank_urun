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
const standardToastActions = document.querySelector("#standardToastActions");
const deleteToastActions = document.querySelector("#deleteToastActions");

let pendingForm = null;
let pendingSubmitter = null;

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

function showActionToast(form, submitter, message, isDelete) {
  pendingForm = form;
  pendingSubmitter = submitter;
  if (toastTitle) {
    toastTitle.textContent = isDelete ? "Silme işlemi" : "İşlemi onayla";
  }
  if (toastMessage) {
    toastMessage.textContent = message || "Bu işlemi yapmak istediğinize emin misiniz?";
  }
  standardToastActions?.classList.toggle("d-none", isDelete);
  deleteToastActions?.classList.toggle("d-none", !isDelete);
  actionToastBackdrop?.classList.add("is-visible");
  getToast()?.show();
}

function submitPendingForm(deleteScope) {
  if (!pendingForm) {
    return;
  }

  if (deleteScope) {
    const scopeInput = pendingForm.querySelector(".delete-scope");
    if (scopeInput) {
      scopeInput.value = deleteScope;
    }
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
    showActionToast(form, submitter, message, submitter?.dataset.actionType === "delete");
  }
});

document.querySelectorAll("[data-toast-cancel]").forEach((button) => button.addEventListener("click", () => {
  pendingForm = null;
  pendingSubmitter = null;
  getToast()?.hide();
}));

actionToastBackdrop?.addEventListener("click", () => {
  pendingForm = null;
  pendingSubmitter = null;
  getToast()?.hide();
});

document.querySelector("[data-toast-confirm]")?.addEventListener("click", () => submitPendingForm());
document.querySelectorAll("[data-delete-scope]").forEach((button) => button.addEventListener("click", () => submitPendingForm(button.dataset.deleteScope)));
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

document.querySelectorAll(".generic-combo").forEach((combo) => {
  const input = combo.querySelector("[data-combo-input]");
  const value = combo.querySelector("[data-combo-value]");
  const options = Array.from(combo.querySelectorAll("[data-combo-option]"));
  const filterOptions = () => {
    const query = input?.value.trim().toUpperCase() || "";
    let visibleCount = 0;
    options.forEach((option) => {
      const text = (option.dataset.label || option.textContent || "").toUpperCase();
      const isVisible = !query || text.includes(query);
      option.classList.toggle("d-none", !isVisible);
      visibleCount += isVisible ? 1 : 0;
    });
    combo.classList.toggle("has-no-results", visibleCount === 0);
  };
  input?.addEventListener("focus", () => { combo.classList.add("is-open"); filterOptions(); });
  input?.addEventListener("click", () => { combo.classList.add("is-open"); filterOptions(); });
  input?.addEventListener("input", () => { if (value) value.value = ""; combo.classList.add("is-open"); filterOptions(); });
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

setupList(document.querySelector('[data-list="groups"]'), {
  defaultSort: { key: "groupNo", direction: "asc" },
  descendingKeys: ["branchCount", "active"],
  numericKeys: ["branchCount", "active"],
  label: "grup",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

setupList(document.querySelector('[data-list="branches"]'), {
  defaultSort: { key: "branchCode", direction: "asc" },
  descendingKeys: [],
  numericKeys: [],
  label: "şube",
  matches: (row, value) => !value("search") || (row.dataset.search || "").includes(value("search").toUpperCase()),
});

const parameterRoot = document.querySelector('[data-list="parameters"]');
if (parameterRoot) {
  const branchValue = parameterRoot.querySelector('[data-list-filter="branchId"]');
  const branchInput = branchValue?.closest(".generic-combo")?.querySelector("[data-combo-input]");
  const yearValue = parameterRoot.querySelector('[data-list-filter="year"]');
  const termValue = parameterRoot.querySelector('[data-list-filter="term"]');
  const storageKey = "bankurun.parameter-context";

  try {
    const stored = JSON.parse(window.sessionStorage.getItem(storageKey) || "null");
    const storedBranch = parameterRoot.querySelector(`[data-combo-option][data-id="${stored?.branchId || ""}"]`);
    if (storedBranch && branchValue && branchInput) {
      branchValue.value = storedBranch.dataset.id || "";
      branchInput.value = storedBranch.dataset.label || "";
    }
    if (stored?.year && yearValue?.querySelector(`option[value="${stored.year}"]`)) yearValue.value = stored.year;
    if (stored?.term && termValue?.querySelector(`option[value="${stored.term}"]`)) termValue.value = stored.term;
  } catch {
    window.sessionStorage.removeItem(storageKey);
  }

  const saveContext = () => window.sessionStorage.setItem(storageKey, JSON.stringify({
    branchId: branchValue?.value || "",
    year: yearValue?.value || "",
    term: termValue?.value || ""
  }));
  [branchValue, yearValue, termValue].forEach((input) => input?.addEventListener("change", saveContext));

  const parameterList = setupList(parameterRoot, {
    defaultSort: { key: "product", direction: "asc" },
    descendingKeys: ["criterion", "target", "actual", "ratio", "hgo", "total"],
    numericKeys: [],
    label: "ana ürün",
    colspan: 11,
    remote: async ({ state, filterValue, signal }) => {
      const params = new URLSearchParams({
        BranchId: filterValue("branchId"),
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
  parameterList?.apply(true);
}

const dashboardRoot = document.querySelector("[data-dashboard]");
if (dashboardRoot) {
  const branchValue = dashboardRoot.querySelector('[data-dashboard-filter="branchId"]');
  const branchInput = branchValue?.closest(".generic-combo")?.querySelector("[data-combo-input]");
  const yearValue = dashboardRoot.querySelector('[data-dashboard-filter="year"]');
  const termValue = dashboardRoot.querySelector('[data-dashboard-filter="term"]');
  const snapshot = dashboardRoot.querySelector("[data-dashboard-snapshot]");
  const storageKey = "bankurun.dashboard-context";
  let requestController = null;

  try {
    const stored = JSON.parse(window.sessionStorage.getItem(storageKey) || "null");
    const storedBranch = dashboardRoot.querySelector(`[data-combo-option][data-id="${stored?.branchId || ""}"]`);
    if (storedBranch && branchValue && branchInput) {
      branchValue.value = storedBranch.dataset.id || "";
      branchInput.value = storedBranch.dataset.label || "";
    }
    if (stored?.year && yearValue?.querySelector(`option[value="${stored.year}"]`)) yearValue.value = stored.year;
    if (stored?.term && termValue?.querySelector(`option[value="${stored.term}"]`)) termValue.value = stored.term;
  } catch {
    window.sessionStorage.removeItem(storageKey);
  }

  const refreshDashboard = async () => {
    requestController?.abort();
    requestController = new AbortController();
    dashboardRoot.classList.add("is-loading");
    window.sessionStorage.setItem(storageKey, JSON.stringify({
      branchId: branchValue?.value || "",
      year: yearValue?.value || "",
      term: termValue?.value || ""
    }));
    const params = new URLSearchParams({
      branchId: branchValue?.value || "",
      year: yearValue?.value || "",
      term: termValue?.value || ""
    });
    try {
      const response = await fetch(`${dashboardRoot.dataset.snapshotUrl}?${params}`, { signal: requestController.signal });
      if (!response.ok) throw new Error("Dashboard yüklenemedi.");
      if (snapshot) snapshot.innerHTML = await response.text();
    } catch (error) {
      if (error.name !== "AbortError" && snapshot) {
        snapshot.innerHTML = '<div class="surface-panel dashboard-empty-state">Dashboard verisi yüklenemedi. Lütfen yeniden deneyin.</div>';
      }
    } finally {
      dashboardRoot.classList.remove("is-loading");
    }
  };

  [branchValue, yearValue, termValue].forEach((input) => input?.addEventListener("change", refreshDashboard));
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
    root.querySelectorAll("[data-organization-create-type]").forEach((item) => {
      const active = item === button;
      item.classList.toggle("is-active", active);
      item.setAttribute("aria-pressed", active.toString());
    });
    root.querySelectorAll("[data-organization-create-form]").forEach((form) => {
      const active = form.dataset.organizationCreateForm === type;
      form.classList.toggle("d-none", !active);
      form.querySelectorAll("input, select").forEach((input) => input.disabled = !active);
    });
  }));
}

function setupProductCreate() {
  const root = document.querySelector("[data-product-create]");
  if (!root || !productType) return;
  root.querySelectorAll("[data-product-create-type]").forEach((button) => button.addEventListener("click", () => {
    productType.value = button.dataset.productCreateType || "Main";
    root.querySelectorAll("[data-product-create-type]").forEach((item) => {
      const active = item === button;
      item.classList.toggle("is-active", active);
      item.setAttribute("aria-pressed", active.toString());
    });
    refreshSuggestion();
    toggleCreateProductFields();
  }));
}

function setupAutomaticNumbers() {
  document.querySelectorAll("[data-number-mode]").forEach((root) => {
    const modeInput = root.querySelector("[data-number-mode-input]");
    const numberInput = root.querySelector("[data-number-input]");
    root.querySelectorAll("[data-number-mode-value]").forEach((button) => button.addEventListener("click", () => {
      const isAutomatic = button.dataset.numberModeValue === "true";
      root.querySelectorAll("[data-number-mode-value]").forEach((item) => {
        const active = item === button;
        item.classList.toggle("is-active", active);
        item.setAttribute("aria-pressed", active.toString());
      });
      if (modeInput) modeInput.value = isAutomatic.toString();
      if (numberInput) {
        numberInput.readOnly = isAutomatic;
        numberInput.required = !isAutomatic;
        numberInput.value = isAutomatic ? (numberInput.dataset.autoValue || "") : "";
        if (!isAutomatic) numberInput.focus();
      }
    }));
  });
}

setupOrganizationCreate();
setupProductCreate();
setupAutomaticNumbers();

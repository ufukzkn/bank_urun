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
  periodFields?.querySelectorAll("input").forEach((input) => {
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

document.querySelectorAll("form").forEach((form) => {
  form.addEventListener("submit", (event) => {
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
    if (value) value.value = option.dataset.id || "";
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

function setupList(root, options) {
  if (!root) {
    return null;
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

function updateBranchChart(visibleRows) {
  const totals = new Map();
  visibleRows.forEach((row) => {
    const key = row.dataset.branchId || "";
    const total = totals.get(key) || { score: 0, target: 0 };
    total.score += parseLocalizedDecimal(row.dataset.score);
    total.target += parseLocalizedDecimal(row.dataset.target);
    totals.set(key, total);
  });

  const scoreSum = Array.from(totals.values()).reduce((sum, item) => sum + item.score, 0);
  const targetSum = Array.from(totals.values()).reduce((sum, item) => sum + item.target, 0);
  const success = targetSum === 0 ? 0 : scoreSum / targetSum * 100;
  document.querySelector("#totalScore")?.replaceChildren(document.createTextNode(scoreSum.toLocaleString("tr-TR", { maximumFractionDigits: 4 })));
  document.querySelector("#totalTarget")?.replaceChildren(document.createTextNode(targetSum.toLocaleString("tr-TR", { maximumFractionDigits: 4 })));
  document.querySelector("#totalSuccess")?.replaceChildren(document.createTextNode(`% ${success.toLocaleString("tr-TR", { maximumFractionDigits: 2 })}`));

  const chart = document.querySelector("#branchChart");
  const emptyChart = document.querySelector("#noBranchChartRows");
  const chartRows = Array.from(chart?.querySelectorAll(".branch-chart-row") || []);
  chartRows.forEach((row) => {
    const total = totals.get(row.dataset.branchId || "");
    row.classList.toggle("d-none", !total);
    if (!total) return;
    const rate = total.target === 0 ? 0 : total.score / total.target * 100;
    const level = rate >= 90 ? "good" : rate >= 70 ? "watch" : "low";
    row.classList.remove("good", "watch", "low");
    row.classList.add(level);
    row.dataset.success = rate.toString();
    const fill = row.querySelector(".branch-chart-fill");
    if (fill) fill.style.width = `${Math.min(100, rate)}%`;
    const value = row.querySelector(".branch-chart-value");
    if (value) value.textContent = `% ${rate.toLocaleString("tr-TR", { maximumFractionDigits: 2 })}`;
  });
  chartRows.sort((a, b) => parseLocalizedDecimal(b.dataset.success) - parseLocalizedDecimal(a.dataset.success)).forEach((row) => chart?.append(row));
  if (emptyChart) {
    chart?.append(emptyChart);
    emptyChart.classList.toggle("d-none", totals.size !== 0 || chartRows.length === 0);
  }
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

setupList(document.querySelector('[data-list="scores"]'), {
  defaultSort: { key: "year", direction: "asc" },
  descendingKeys: [],
  numericKeys: ["year", "term", "score", "displayed", "target", "hgo", "development", "size", "success"],
  label: "puan satırı",
  matches: (row, value) => {
    const search = value("search").toUpperCase();
    return (!search || (row.dataset.search || "").includes(search))
      && (!value("groupId") || row.dataset.groupId === value("groupId"))
      && (!value("year") || row.dataset.year === value("year"))
      && (!value("term") || row.dataset.term === value("term"));
  },
  afterApply: updateBranchChart,
});

codeMode?.addEventListener("change", toggleManualCode);
manualCode?.addEventListener("input", refreshSuggestion);
productType?.addEventListener("change", () => {
  refreshSuggestion();
  toggleCreateProductFields();
});

toggleManualCode();
toggleCreateProductFields();

const performanceYear = document.querySelector("#performanceYear");
const performanceTerm = document.querySelector("#performanceTerm");
const performanceGroup = document.querySelector("#performanceGroup");
const performanceBranch = document.querySelector("#performanceBranch");
const performanceResultRows = Array.from(document.querySelectorAll(".performance-result-row"));
let performanceParameterList = null;
let performanceResultList = null;

function performanceContextMatches(row) {
  return (!performanceYear?.value || row.dataset.year === performanceYear.value)
    && (!performanceTerm?.value || row.dataset.term === performanceTerm.value)
    && (!performanceGroup?.value || row.dataset.groupId === performanceGroup.value)
    && (!performanceBranch?.value || row.dataset.branchId === performanceBranch.value);
}

function performanceLevel(success) {
  return success >= 90 ? "good" : success >= 70 ? "watch" : "low";
}

function clearChildren(element) {
  while (element?.firstChild) {
    element.removeChild(element.firstChild);
  }
}

function createPerformanceChartRow(item, chartType) {
  const row = document.createElement("div");
  const level = performanceLevel(item.success);
  row.className = `performance-chart-row ${level}`;

  const label = document.createElement("div");
  label.className = "performance-chart-label";
  const code = document.createElement("strong");
  code.textContent = item.code;
  const name = document.createElement("span");
  name.textContent = item.name;
  const caption = document.createElement("small");
  caption.textContent = chartType === "branch" ? item.group : item.caption;
  label.append(code, name, caption);

  const track = document.createElement("div");
  track.className = "performance-chart-track";
  const fill = document.createElement("div");
  fill.className = "performance-chart-fill";
  fill.style.width = `${Math.min(100, Math.max(0, item.success))}%`;
  track.append(fill);

  const value = document.createElement("div");
  value.className = "performance-chart-value";
  value.textContent = `% ${item.success.toLocaleString("tr-TR", { maximumFractionDigits: 1 })}`;
  row.append(label, track, value);
  return row;
}

function updatePerformanceWorkspace() {
  if (!performanceResultRows.length) {
    return;
  }

  const selectedBranch = performanceBranch?.selectedOptions[0];
  if (selectedBranch?.dataset.groupId && performanceGroup && !performanceGroup.value) {
    performanceGroup.value = selectedBranch.dataset.groupId;
  }

  const activeGroup = performanceGroup?.value || "";
  performanceBranch?.querySelectorAll("option[data-group-id]").forEach((option) => {
    const isAllowed = !activeGroup || option.dataset.groupId === activeGroup;
    option.disabled = !isAllowed;
    option.hidden = !isAllowed;
  });
  if (performanceBranch?.value && performanceBranch.selectedOptions[0]?.disabled) {
    performanceBranch.value = "";
  }

  performanceParameterList?.apply(true);
  performanceResultList?.apply(true);
  const matching = performanceResultRows.filter(performanceContextMatches);
  const potential = matching.reduce((sum, row) => sum + parseLocalizedDecimal(row.dataset.allocated), 0);
  const earned = matching.reduce((sum, row) => sum + parseLocalizedDecimal(row.dataset.earned), 0);
  const success = potential === 0 ? 0 : earned / potential * 100;
  const missing = matching.filter((row) => row.dataset.missing === "true").length;

  const setText = (selector, value) => {
    const element = document.querySelector(selector);
    if (element) element.textContent = value;
  };
  setText("#performancePotential", potential.toLocaleString("tr-TR", { maximumFractionDigits: 2 }));
  setText("#performanceEarned", earned.toLocaleString("tr-TR", { maximumFractionDigits: 2 }));
  setText("#performanceSuccess", `% ${success.toLocaleString("tr-TR", { maximumFractionDigits: 1 })}`);
  setText("#performanceMissing", missing.toString());

  const branches = new Map();
  const products = new Map();
  matching.forEach((row) => {
    const branchKey = row.dataset.branchId || "";
    const productKey = row.dataset.product || "";
    const branch = branches.get(branchKey) || { code: row.dataset.branch?.split(" ")[0] || "", name: row.dataset.branch?.replace(/^\S+\s*/, "") || "", group: row.dataset.group || "", earned: 0, potential: 0, missing: 0 };
    branch.earned += parseLocalizedDecimal(row.dataset.earned);
    branch.potential += parseLocalizedDecimal(row.dataset.allocated);
    branch.missing += row.dataset.missing === "true" ? 1 : 0;
    branches.set(branchKey, branch);
    const product = products.get(productKey) || { code: row.dataset.product?.split(" ")[0] || "", name: row.dataset.product?.replace(/^\S+\s*/, "") || "", caption: row.dataset.segment || "", earned: 0, potential: 0 };
    product.earned += parseLocalizedDecimal(row.dataset.earned);
    product.potential += parseLocalizedDecimal(row.dataset.allocated);
    products.set(productKey, product);
  });

  const branchItems = Array.from(branches.values()).map((item) => ({ ...item, success: item.potential === 0 ? 0 : item.earned / item.potential * 100 }))
    .sort((a, b) => b.success - a.success || a.code.localeCompare(b.code, "tr"));
  const productItems = Array.from(products.values()).map((item) => ({ ...item, success: item.potential === 0 ? 0 : item.earned / item.potential * 100 }))
    .sort((a, b) => b.success - a.success || a.code.localeCompare(b.code, "tr"));

  const branchChart = document.querySelector("#performanceBranchChart");
  const branchEmpty = document.querySelector("#performanceBranchChartEmpty");
  clearChildren(branchChart);
  branchItems.forEach((item) => branchChart?.append(createPerformanceChartRow(item, "branch")));
  branchEmpty?.classList.toggle("d-none", branchItems.length !== 0);

  const productChart = document.querySelector("#performanceProductChart");
  const productEmpty = document.querySelector("#performanceProductChartEmpty");
  clearChildren(productChart);
  productItems.forEach((item) => productChart?.append(createPerformanceChartRow(item, "product")));
  productEmpty?.classList.toggle("d-none", productItems.length !== 0);

  const priorities = document.querySelector("#performancePriorities");
  clearChildren(priorities);
  const priorityItems = branchItems
    .filter((item) => item.missing > 0 || item.success < 70)
    .sort((a, b) => (b.missing - a.missing) || (a.success - b.success))
    .slice(0, 5);
  priorityItems.forEach((item) => {
    const row = document.createElement("div");
    row.className = `priority-item ${item.success < 70 ? "low" : ""}`;
    const copy = document.createElement("div");
    const strong = document.createElement("strong");
    strong.textContent = `${item.code} ${item.name}`;
    const caption = document.createElement("span");
    caption.textContent = item.missing > 0 ? `${item.missing} eksik gerçekleşme` : "Başarı oranı izleme eşiğinin altında";
    copy.append(strong, caption);
    const score = document.createElement("b");
    score.textContent = `% ${item.success.toLocaleString("tr-TR", { maximumFractionDigits: 1 })}`;
    row.append(copy, score);
    priorities?.append(row);
  });
  if (!priorityItems.length && priorities) {
    const empty = document.createElement("div");
    empty.className = "empty-inline";
    empty.textContent = "Öncelikli eksik veya düşük başarı kaydı yok";
    priorities.append(empty);
  }
}

function setupPerformanceWorkspace() {
  const parameterRoot = document.querySelector('[data-list="performanceParameters"]');
  const resultRoot = document.querySelector('[data-list="performanceResults"]');
  if (!parameterRoot && !resultRoot) {
    return;
  }

  performanceParameterList = setupList(parameterRoot, {
    defaultSort: { key: "year", direction: "desc" },
    descendingKeys: ["year", "term", "totalScore"],
    numericKeys: ["year", "term", "totalScore", "active"],
    label: "parametre",
    matches: (row, value) => {
      const search = value("search").toUpperCase();
      return performanceContextMatches(row)
        && (!search || (row.dataset.search || "").includes(search));
    }
  });

  performanceResultList = setupList(resultRoot, {
    defaultSort: { key: "year", direction: "desc" },
    descendingKeys: ["year", "term", "earned", "success"],
    numericKeys: ["year", "term", "earned", "allocated", "success"],
    label: "şube sonucu",
    matches: (row, value) => {
      const search = value("search").toUpperCase();
      const missing = value("missing");
      return performanceContextMatches(row)
        && (!search || (row.dataset.search || "").includes(search))
        && (!missing || row.dataset.missing === missing);
    }
  });

  [performanceYear, performanceTerm, performanceGroup, performanceBranch].forEach((input) => input?.addEventListener("change", updatePerformanceWorkspace));
  document.querySelectorAll("[data-performance-tab]").forEach((button) => button.addEventListener("click", () => {
    const target = button.dataset.performanceTab;
    document.querySelectorAll("[data-performance-tab]").forEach((tab) => tab.classList.toggle("is-active", tab === button));
    document.querySelectorAll("[data-performance-view]").forEach((view) => view.classList.toggle("d-none", view.dataset.performanceView !== target));
  }));

  document.querySelectorAll("[data-metric-form]").forEach((form) => {
    const preview = form.closest(".performance-result-detail")?.querySelector("[data-metric-preview]");
    const updatePreview = () => {
      const inputs = Array.from(form.querySelectorAll(".metric-input"));
      const weighted = inputs.reduce((sum, input) => sum + parseLocalizedDecimal(input.value) * parseLocalizedDecimal(input.dataset.weight) / 100, 0);
      const allocated = parseLocalizedDecimal(form.dataset.allocatedScore);
      const earned = Math.min(allocated, allocated * weighted / 100);
      if (preview) preview.textContent = `${earned.toLocaleString("tr-TR", { maximumFractionDigits: 2 })} / ${allocated.toLocaleString("tr-TR", { maximumFractionDigits: 2 })} puan`;
    };
    form.querySelectorAll(".metric-input").forEach((input) => input.addEventListener("input", updatePreview));
  });

  updatePerformanceWorkspace();
}

setupPerformanceWorkspace();

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

const filterSearch = document.querySelector("#filterSearch");
const filterYear = document.querySelector("#filterYear");
const filterTerm = document.querySelector("#filterTerm");
const includeInactive = document.querySelector("#includeInactive");
const showMainProducts = document.querySelector("#showMainProducts");
const showSubProducts = document.querySelector("#showSubProducts");
const pageSize = document.querySelector("#pageSize");
const paginationSummary = document.querySelector("#paginationSummary");
const pageIndicator = document.querySelector("#pageIndicator");
const firstPage = document.querySelector("#firstPage");
const prevPage = document.querySelector("#prevPage");
const nextPage = document.querySelector("#nextPage");
const lastPage = document.querySelector("#lastPage");
const pageJump = document.querySelector("#pageJump");
const goPage = document.querySelector("#goPage");
const noClientRows = document.querySelector("#noClientRows");
const productRows = Array.from(document.querySelectorAll(".product-row"));
const productTableBody = document.querySelector("#productTableBody");
const sortButtons = Array.from(document.querySelectorAll(".sort-button"));
const actionConfirmToast = document.querySelector("#actionConfirmToast");
const actionToastBackdrop = document.querySelector("#actionToastBackdrop");
const toastTitle = document.querySelector("#toastTitle");
const toastMessage = document.querySelector("#toastMessage");
const standardToastActions = document.querySelector("#standardToastActions");
const deleteToastActions = document.querySelector("#deleteToastActions");

let currentPage = 1;
let filteredRows = [];
let currentTotalPages = 1;
let currentSort = { key: "year", direction: "desc" };
let pendingForm = null;
let pendingSubmitter = null;

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

  const params = new URLSearchParams({
    type: productType.value,
    code,
  });

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
  const options = Array.from(document.querySelectorAll("#mainProductOptions .combo-option"));
  let visibleCount = 0;

  options.forEach((option) => {
    const text = option.dataset.label?.toUpperCase() || option.textContent?.toUpperCase() || "";
    const isVisible = !query || text.includes(query);
    option.classList.toggle("d-none", !isVisible);
    if (isVisible) {
      visibleCount += 1;
    }
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

function selectMainProductOption(option) {
  if (!option || !createMainProductSearch || !createMainProductId) {
    return;
  }

  createMainProductSearch.value = option.dataset.label || "";
  createMainProductId.value = option.dataset.id || "";
  closeMainProductCombo();
  refreshSuggestion();
}

function getActionsRow(row) {
  return row.nextElementSibling?.classList.contains("actions-row") ? row.nextElementSibling : null;
}

function rowMatchesFilters(row) {
  const search = filterSearch?.value.trim().toUpperCase() || "";
  const year = filterYear?.value.trim() || "";
  const term = filterTerm?.value.trim() || "";
  const hasSub = row.dataset.hasSub === "true";
  const active = row.dataset.active === "true";

  if (search && !row.dataset.search.includes(search)) {
    return false;
  }

  if (year && row.dataset.year !== year) {
    return false;
  }

  if (term && row.dataset.term !== term) {
    return false;
  }

  if (!includeInactive?.checked && !active) {
    return false;
  }

  if (!showMainProducts?.checked && !hasSub) {
    return false;
  }

  if (!showSubProducts?.checked && hasSub) {
    return false;
  }

  return true;
}

function hideRowPair(row) {
  row.classList.add("d-none");
  getActionsRow(row)?.classList.add("d-none");
}

function showRowPair(row) {
  row.classList.remove("d-none");
  getActionsRow(row)?.classList.remove("d-none");
}

function getSortValue(row, key) {
  switch (key) {
    case "year":
      return Number(row.dataset.year || 0);
    case "term":
      return Number(row.dataset.term || 0);
    case "main":
      return row.dataset.mainCode || "";
    case "sub":
      return row.dataset.subCode || "";
    case "subName":
      return row.dataset.subName || "";
    default:
      return "";
  }
}

function compareRows(a, b) {
  const aValue = getSortValue(a, currentSort.key);
  const bValue = getSortValue(b, currentSort.key);
  const direction = currentSort.direction === "asc" ? 1 : -1;

  if (typeof aValue === "number" && typeof bValue === "number") {
    return (aValue - bValue) * direction;
  }

  return aValue.localeCompare(bValue, "tr", { numeric: true, sensitivity: "base" }) * direction;
}

function reorderTableRows(orderedRows) {
  if (!productTableBody) {
    return;
  }

  const emptyRows = Array.from(productTableBody.querySelectorAll(".empty-row"));
  const remainingRows = productRows.filter((row) => !orderedRows.includes(row));
  [...orderedRows, ...remainingRows].forEach((row) => {
    const actionsRow = getActionsRow(row);
    productTableBody.append(row);
    if (actionsRow) {
      productTableBody.append(actionsRow);
    }
  });
  emptyRows.forEach((row) => productTableBody.append(row));
}

function updateSortButtons() {
  sortButtons.forEach((button) => {
    const isActive = button.dataset.sort === currentSort.key;
    button.classList.toggle("is-active", isActive);
    button.classList.toggle("desc", isActive && currentSort.direction === "desc");
    button.setAttribute("aria-sort", isActive ? (currentSort.direction === "asc" ? "ascending" : "descending") : "none");
  });
}

function applyClientFilters(resetPage = true) {
  if (resetPage) {
    currentPage = 1;
  }

  filteredRows = productRows.filter(rowMatchesFilters).sort(compareRows);
  reorderTableRows(filteredRows);
  const size = Number(pageSize?.value || 10);
  currentTotalPages = Math.max(1, Math.ceil(filteredRows.length / size));
  currentPage = Math.min(Math.max(1, currentPage), currentTotalPages);

  productRows.forEach(hideRowPair);

  const start = (currentPage - 1) * size;
  const pageRows = filteredRows.slice(start, start + size);
  pageRows.forEach(showRowPair);

  const first = filteredRows.length === 0 ? 0 : start + 1;
  const last = Math.min(start + size, filteredRows.length);

  if (paginationSummary) {
    paginationSummary.textContent = filteredRows.length === 0
      ? "0 ürün"
      : `${filteredRows.length} üründen ${first}-${last} arası gösteriliyor`;
  }

  if (pageIndicator) {
    pageIndicator.textContent = `${currentPage} / ${currentTotalPages}`;
  }

  if (pageJump) {
    pageJump.max = currentTotalPages.toString();
    pageJump.value = currentPage.toString();
  }

  if (firstPage) {
    firstPage.disabled = currentPage <= 1;
  }

  if (prevPage) {
    prevPage.disabled = currentPage <= 1;
  }

  if (nextPage) {
    nextPage.disabled = currentPage >= currentTotalPages;
  }

  if (lastPage) {
    lastPage.disabled = currentPage >= currentTotalPages;
  }

  noClientRows?.classList.toggle("d-none", filteredRows.length !== 0 || productRows.length === 0);
}

function goToPage(page) {
  currentPage = Math.min(Math.max(1, page), currentTotalPages);
  applyClientFilters(false);
}

function getToast() {
  if (!actionConfirmToast || !window.bootstrap?.Toast) {
    return null;
  }

  return window.bootstrap.Toast.getOrCreateInstance(actionConfirmToast, { autohide: false });
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
    if (form.dataset.toastConfirmed === "true") {
      delete form.dataset.toastConfirmed;
      return;
    }

    const submitter = event.submitter;
    const message = submitter?.dataset.confirm || form.dataset.confirm;
    if (!message) {
      return;
    }

    event.preventDefault();
    showActionToast(form, submitter, message, submitter?.dataset.actionType === "delete");
  });
});

document.querySelectorAll("[data-toast-cancel]").forEach((button) => {
  button.addEventListener("click", () => {
    pendingForm = null;
    pendingSubmitter = null;
    getToast()?.hide();
  });
});

actionToastBackdrop?.addEventListener("click", () => {
  pendingForm = null;
  pendingSubmitter = null;
  getToast()?.hide();
});

document.querySelector("[data-toast-confirm]")?.addEventListener("click", () => {
  submitPendingForm();
});

document.querySelectorAll("[data-delete-scope]").forEach((button) => {
  button.addEventListener("click", () => {
    submitPendingForm(button.dataset.deleteScope);
  });
});

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
  filterMainProductOptions();
  refreshSuggestion();
});

createMainProductSearch?.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    closeMainProductCombo();
  }
});

document.querySelectorAll("#mainProductOptions .combo-option").forEach((option) => {
  option.addEventListener("click", () => selectMainProductOption(option));
});

document.addEventListener("click", (event) => {
  if (mainProductCombo && !mainProductCombo.contains(event.target)) {
    closeMainProductCombo();
  }
});

createMainProductSearch?.closest("form")?.addEventListener("submit", (event) => {
  syncCreateMainProductId();
  if (productType?.value === "Sub" && !createMainProductId?.value) {
    event.preventDefault();
    alert("Alt ürün oluşturmak için listeden bağlı ana ürün seçmelisiniz.");
  }
});

let filterTimer;
document.querySelectorAll(".list-filter").forEach((input) => {
  const eventName = input.tagName === "SELECT" || input.type === "checkbox" ? "change" : "input";
  input.addEventListener(eventName, () => {
    clearTimeout(filterTimer);
    filterTimer = setTimeout(() => applyClientFilters(true), 180);
  });
});

sortButtons.forEach((button) => {
  button.addEventListener("click", () => {
    const key = button.dataset.sort;
    if (!key) {
      return;
    }

    if (currentSort.key === key) {
      currentSort.direction = currentSort.direction === "asc" ? "desc" : "asc";
    } else {
      currentSort = {
        key,
        direction: key === "year" || key === "term" ? "desc" : "asc",
      };
    }

    updateSortButtons();
    applyClientFilters(true);
  });
});

firstPage?.addEventListener("click", () => goToPage(1));

prevPage?.addEventListener("click", () => goToPage(currentPage - 1));

nextPage?.addEventListener("click", () => goToPage(currentPage + 1));

lastPage?.addEventListener("click", () => goToPage(currentTotalPages));

goPage?.addEventListener("click", () => {
  goToPage(Number(pageJump?.value || 1));
});

pageJump?.addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    event.preventDefault();
    goToPage(Number(pageJump.value || 1));
  }
});

codeMode?.addEventListener("change", toggleManualCode);
manualCode?.addEventListener("input", refreshSuggestion);
productType?.addEventListener("change", () => {
  refreshSuggestion();
  toggleCreateProductFields();
});

if (window.location.pathname.toLowerCase() === "/products") {
  window.history.replaceState(null, "", "/");
}

toggleManualCode();
toggleCreateProductFields();
updateSortButtons();
applyClientFilters(true);

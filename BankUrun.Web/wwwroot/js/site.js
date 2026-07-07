const codeMode = document.querySelector("#codeMode");
const manualCodeWrap = document.querySelector("#manualCodeWrap");
const manualCode = document.querySelector("#manualCode");
const productType = document.querySelector("#productType");
const codeSuggestion = document.querySelector("#codeSuggestion");
const createMainProductWrap = document.querySelector("#createMainProductWrap");
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
const prevPage = document.querySelector("#prevPage");
const nextPage = document.querySelector("#nextPage");
const visibleCount = document.querySelector("#visibleCount");
const noClientRows = document.querySelector("#noClientRows");
const productRows = Array.from(document.querySelectorAll(".product-row"));

let currentPage = 1;
let filteredRows = [];

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

  const params = new URLSearchParams({
    type: productType.value,
    code,
  });

  if (productType.value === "Sub" && createMainProductId?.value) {
    params.set("mainProductId", createMainProductId.value);
  }

  const response = await fetch(`/code-suggestion?${params.toString()}`);
  const result = await response.json();
  codeSuggestion.className = result.available ? "form-text text-success" : "form-text text-warning";
  codeSuggestion.textContent = result.available
    ? `${result.requested} uygun.`
    : (result.suggestion ? `${result.requested} dolu. Öneri: ${result.suggestion}` : result.message);
}

function syncCreateMainProductId() {
  if (!createMainProductSearch || !createMainProductId) {
    return;
  }

  const options = Array.from(document.querySelectorAll("#mainProductOptions option"));
  const match = options.find((option) => option.value === createMainProductSearch.value);
  createMainProductId.value = match?.dataset.id || "";
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

function applyClientFilters(resetPage = true) {
  if (resetPage) {
    currentPage = 1;
  }

  filteredRows = productRows.filter(rowMatchesFilters);
  const size = Number(pageSize?.value || 10);
  const totalPages = Math.max(1, Math.ceil(filteredRows.length / size));
  currentPage = Math.min(currentPage, totalPages);

  productRows.forEach(hideRowPair);

  const start = (currentPage - 1) * size;
  const pageRows = filteredRows.slice(start, start + size);
  pageRows.forEach(showRowPair);

  const first = filteredRows.length === 0 ? 0 : start + 1;
  const last = Math.min(start + size, filteredRows.length);

  if (paginationSummary) {
    paginationSummary.textContent = filteredRows.length === 0
      ? "0 kayıt"
      : `${filteredRows.length} kayıttan ${first}-${last} arası gösteriliyor`;
  }

  if (pageIndicator) {
    pageIndicator.textContent = `${currentPage} / ${totalPages}`;
  }

  if (prevPage) {
    prevPage.disabled = currentPage <= 1;
  }

  if (nextPage) {
    nextPage.disabled = currentPage >= totalPages;
  }

  if (visibleCount) {
    visibleCount.textContent = filteredRows.length.toString();
  }

  noClientRows?.classList.toggle("d-none", filteredRows.length !== 0 || productRows.length === 0);
}

document.querySelectorAll("form").forEach((form) => {
  form.addEventListener("submit", (event) => {
    const message = event.submitter?.dataset.confirm || form.dataset.confirm;
    if (message && !confirm(message)) {
      event.preventDefault();
    }
  });
});

createMainProductSearch?.addEventListener("input", () => {
  syncCreateMainProductId();
  refreshSuggestion();
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

prevPage?.addEventListener("click", () => {
  currentPage = Math.max(1, currentPage - 1);
  applyClientFilters(false);
});

nextPage?.addEventListener("click", () => {
  currentPage += 1;
  applyClientFilters(false);
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
applyClientFilters(true);

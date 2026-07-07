const codeMode = document.querySelector("#codeMode");
const manualCodeWrap = document.querySelector("#manualCodeWrap");
const manualCode = document.querySelector("#manualCode");
const productType = document.querySelector("#productType");
const codeSuggestion = document.querySelector("#codeSuggestion");
const createMainProductWrap = document.querySelector("#createMainProductWrap");
const createMainProductSearch = document.querySelector("#createMainProductSearch");
const createMainProductId = document.querySelector("#createMainProductId");
const listFilterForm = document.querySelector("#listFilterForm");

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
  createMainProductSearch.required = isSubProduct;
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

  const response = await fetch(`/Products/SuggestCode?type=${encodeURIComponent(productType.value)}&code=${encodeURIComponent(code)}`);
  const result = await response.json();
  codeSuggestion.className = result.available ? "form-text text-success" : "form-text text-warning";
  codeSuggestion.textContent = result.available
    ? `${result.requested} uygun.`
    : (result.suggestion ? `${result.requested} dolu. Öneri: ${result.suggestion}` : result.message);
}

document.querySelectorAll("form[data-confirm]").forEach((form) => {
  form.addEventListener("submit", (event) => {
    const message = event.submitter?.dataset.confirm || form.dataset.confirm;
    if (message && !confirm(message)) {
      event.preventDefault();
    }
  });
});

document.querySelectorAll("form:not([data-confirm])").forEach((form) => {
  form.addEventListener("submit", (event) => {
    const message = event.submitter?.dataset.confirm;
    if (message && !confirm(message)) {
      event.preventDefault();
    }
  });
});

function syncCreateMainProductId() {
  if (!createMainProductSearch || !createMainProductId) {
    return;
  }

  const options = Array.from(document.querySelectorAll("#mainProductOptions option"));
  const match = options.find((option) => option.value === createMainProductSearch.value);
  createMainProductId.value = match?.dataset.id || "";
}

function buildCleanFilterUrl(form) {
  const params = new URLSearchParams();
  const search = form.elements.Search?.value?.trim();
  const year = form.elements.Year?.value?.trim();
  const term = form.elements.Term?.value?.trim();
  const pageSize = form.elements.PageSize?.value;
  const includeInactive = form.elements.IncludeInactive?.checked;
  const showMainProducts = form.elements.ShowMainProducts?.checked;
  const showSubProducts = form.elements.ShowSubProducts?.checked;

  if (search) {
    params.set("Search", search);
  }

  if (year) {
    params.set("Year", year);
  }

  if (term) {
    params.set("Term", term);
  }

  if (includeInactive) {
    params.set("IncludeInactive", "true");
  }

  if (showMainProducts === false) {
    params.set("ShowMainProducts", "false");
  }

  if (showSubProducts === false) {
    params.set("ShowSubProducts", "false");
  }

  if (pageSize && pageSize !== "10") {
    params.set("PageSize", pageSize);
  }

  const query = params.toString();
  return query ? `${form.action}?${query}` : form.action;
}

codeMode?.addEventListener("change", toggleManualCode);
manualCode?.addEventListener("input", refreshSuggestion);
productType?.addEventListener("change", () => {
  refreshSuggestion();
  toggleCreateProductFields();
});
createMainProductSearch?.addEventListener("input", syncCreateMainProductId);

createMainProductSearch?.closest("form")?.addEventListener("submit", (event) => {
  syncCreateMainProductId();
  if (productType?.value === "Sub" && !createMainProductId?.value) {
    event.preventDefault();
    alert("Alt ürün oluşturmak için listeden bağlı ana ürün seçmelisiniz.");
  }
});

listFilterForm?.addEventListener("submit", (event) => {
  event.preventDefault();
  window.location.href = buildCleanFilterUrl(listFilterForm);
});

document.querySelectorAll(".auto-submit-filter").forEach((input) => {
  if (input.classList.contains("debounced-filter")) {
    let submitTimer;
    input.addEventListener("input", () => {
      clearTimeout(submitTimer);
      submitTimer = setTimeout(() => input.form?.requestSubmit(), 450);
    });
    return;
  }

  input.addEventListener("change", () => input.form?.requestSubmit());
});
toggleManualCode();
toggleCreateProductFields();

const codeMode = document.querySelector("#codeMode");
const manualCodeWrap = document.querySelector("#manualCodeWrap");
const manualCode = document.querySelector("#manualCode");
const productType = document.querySelector("#productType");
const codeSuggestion = document.querySelector("#codeSuggestion");
const assignmentMainProduct = document.querySelector("#assignmentMainProduct");
const assignmentPeriod = document.querySelector("#assignmentPeriod");

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

function refreshAssignmentPeriods() {
  if (!assignmentMainProduct || !assignmentPeriod) {
    return;
  }

  const selectedMainProductId = assignmentMainProduct.value;
  let visibleOptionCount = 0;
  assignmentPeriod.value = "";

  Array.from(assignmentPeriod.options).forEach((option) => {
    if (!option.value) {
      option.hidden = false;
      option.textContent = selectedMainProductId ? "Yıl / dönem seç" : "Önce ana ürün seç";
      return;
    }

    const isVisible = option.dataset.mainProductId === selectedMainProductId;
    option.hidden = !isVisible;
    if (isVisible) {
      visibleOptionCount += 1;
    }
  });

  assignmentPeriod.disabled = !selectedMainProductId || visibleOptionCount === 0;
  if (selectedMainProductId && visibleOptionCount === 0) {
    assignmentPeriod.options[0].textContent = "Bu ana ürün için dönem yok";
  }
}

function normalizeSearchText(value) {
  return (value || "").trim().toLocaleUpperCase("tr-TR");
}

function filterSelectOptions(input) {
  const select = document.querySelector(input.dataset.selectFilter);
  if (!select) {
    return;
  }

  const query = normalizeSearchText(input.value);
  Array.from(select.options).forEach((option) => {
    if (!option.value) {
      option.hidden = false;
      return;
    }

    option.hidden = query.length > 0 && !normalizeSearchText(option.textContent).includes(query);
  });

  if (select.selectedOptions.length > 0 && select.selectedOptions[0].hidden) {
    select.value = "";
    select.dispatchEvent(new Event("change"));
  }
}

codeMode?.addEventListener("change", toggleManualCode);
manualCode?.addEventListener("input", refreshSuggestion);
productType?.addEventListener("change", refreshSuggestion);
assignmentMainProduct?.addEventListener("change", refreshAssignmentPeriods);
document.querySelectorAll("[data-select-filter]").forEach((input) => {
  input.addEventListener("input", () => filterSelectOptions(input));
});

document.querySelectorAll(".auto-submit-filter").forEach((input) => {
  input.addEventListener("change", () => input.form?.requestSubmit());
});
toggleManualCode();
refreshAssignmentPeriods();

async function exportToImage(receiptId, elementId) {
  const element = document.getElementById(elementId);

  if (!element) {
    throw new Error(`Element not found: ${elementId}`);
  }

  if (typeof html2canvas !== "function") {
    throw new Error("html2canvas is not loaded");
  }

  const canvas = await html2canvas(element, {
    backgroundColor: "#ffffff"
  });

  const image = canvas.toDataURL("image/png");
  const anchor = document.createElement("a");

  anchor.href = image;
  anchor.download = `receipt-table-${receiptId}.png`;

  anchor.click();
}
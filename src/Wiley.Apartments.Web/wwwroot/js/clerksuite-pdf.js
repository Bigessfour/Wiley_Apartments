window.ClerkSuitePdf = {
  _blobUrl(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return URL.createObjectURL(new Blob([bytes], { type: "application/pdf" }));
  },

  print(base64) {
    if (!base64) {
      return;
    }

    const url = this._blobUrl(base64);
    const iframe = document.createElement("iframe");
    iframe.setAttribute("title", "Print PDF");
    iframe.style.position = "fixed";
    iframe.style.right = "0";
    iframe.style.bottom = "0";
    iframe.style.width = "1px";
    iframe.style.height = "1px";
    iframe.style.opacity = "0";
    iframe.style.border = "0";
    iframe.src = url;
    iframe.onload = () => {
      try {
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
      } finally {
        setTimeout(() => {
          URL.revokeObjectURL(url);
          iframe.remove();
        }, 60_000);
      }
    };
    document.body.appendChild(iframe);
  },

  download(base64, fileName) {
    if (!base64) {
      return;
    }

    const url = this._blobUrl(base64);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName || "document.pdf";
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  },
};

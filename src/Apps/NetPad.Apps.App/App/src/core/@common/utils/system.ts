export class System {
    public static downloadFile(fileName: string, mimeType = "text/plain", data: Uint8Array) {
        const downloadLink = document.createElement("A") as HTMLAnchorElement;
        try {
            downloadLink.download = fileName;
            downloadLink.href = URL.createObjectURL(new Blob([data as BlobPart], {type: mimeType}));
            downloadLink.target = '_blank';
            downloadLink.click();
        } finally {
            downloadLink.remove();
        }
    }

    public static downloadTextAsFile(fileName: string, mimeType = "text/plain", text: string) {
        const downloadLink = document.createElement("A") as HTMLAnchorElement;
        try {
            downloadLink.download = fileName;
            downloadLink.href = URL.createObjectURL(new Blob([text], {type: mimeType}));
            downloadLink.target = '_blank';
            downloadLink.click();
        } finally {
            downloadLink.remove();
        }
    }
}

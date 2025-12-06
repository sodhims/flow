window.downloadFile = function (filename, content) {
    try {
        const blob = new Blob([content], { type: 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        console.log('Download triggered for: ' + filename);
    } catch (e) {
        console.error('Download error:', e);
    }
};
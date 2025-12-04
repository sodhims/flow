// PDF Export functionality for DFD Editor
// This file should be placed in wwwroot/js/pdf-export.js

window.downloadPDF = async function (svgContent, filename) {
    try {
        // Load jsPDF library if not already loaded
        if (typeof window.jspdf === 'undefined') {
            await loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js');
            await loadScript('https://cdnjs.cloudflare.com/ajax/libs/svg2pdf.js/2.2.1/svg2pdf.min.js');
        }

        // Parse the SVG content
        const parser = new DOMParser();
        const svgDoc = parser.parseFromString(svgContent, 'image/svg+xml');
        const svgElement = svgDoc.documentElement;

        // Get original SVG dimensions
        const originalWidth = parseFloat(svgElement.getAttribute('width')) || 2000;
        const originalHeight = parseFloat(svgElement.getAttribute('height')) || 2000;

        // Calculate content bounds by examining elements
        let minX = Infinity, minY = Infinity, maxX = 0, maxY = 0;
        
        // Check rect elements (nodes, swimlanes, backgrounds)
        svgElement.querySelectorAll('rect').forEach(el => {
            const x = parseFloat(el.getAttribute('x') || '0');
            const y = parseFloat(el.getAttribute('y') || '0');
            const width = parseFloat(el.getAttribute('width') || '0');
            const height = parseFloat(el.getAttribute('height') || '0');
            // Skip full-page background rectangles
            if (width >= originalWidth - 10 && height >= originalHeight - 10) return;
            minX = Math.min(minX, x);
            minY = Math.min(minY, y);
            maxX = Math.max(maxX, x + width);
            maxY = Math.max(maxY, y + height);
        });

        // Check ellipse elements
        svgElement.querySelectorAll('ellipse').forEach(el => {
            const cx = parseFloat(el.getAttribute('cx') || '0');
            const cy = parseFloat(el.getAttribute('cy') || '0');
            const rx = parseFloat(el.getAttribute('rx') || '0');
            const ry = parseFloat(el.getAttribute('ry') || '0');
            minX = Math.min(minX, cx - rx);
            minY = Math.min(minY, cy - ry);
            maxX = Math.max(maxX, cx + rx);
            maxY = Math.max(maxY, cy + ry);
        });

        // Check polygon elements (diamonds, parallelograms)
        svgElement.querySelectorAll('polygon').forEach(el => {
            const points = el.getAttribute('points');
            if (points) {
                points.split(/\s+/).forEach(point => {
                    const [x, y] = point.split(',').map(Number);
                    if (!isNaN(x) && !isNaN(y)) {
                        minX = Math.min(minX, x);
                        minY = Math.min(minY, y);
                        maxX = Math.max(maxX, x);
                        maxY = Math.max(maxY, y);
                    }
                });
            }
        });

        // Check text elements
        svgElement.querySelectorAll('text').forEach(el => {
            const x = parseFloat(el.getAttribute('x') || '0');
            const y = parseFloat(el.getAttribute('y') || '0');
            minX = Math.min(minX, x - 50);
            minY = Math.min(minY, y - 20);
            maxX = Math.max(maxX, x + 50);
            maxY = Math.max(maxY, y + 20);
        });

        // Check path elements (edges, connections)
        svgElement.querySelectorAll('path').forEach(el => {
            const d = el.getAttribute('d');
            if (d) {
                const matches = d.matchAll(/[ML]\s*(-?\d+\.?\d*)[,\s]+(-?\d+\.?\d*)/gi);
                for (const match of matches) {
                    const x = parseFloat(match[1]);
                    const y = parseFloat(match[2]);
                    if (!isNaN(x) && !isNaN(y)) {
                        minX = Math.min(minX, x);
                        minY = Math.min(minY, y);
                        maxX = Math.max(maxX, x);
                        maxY = Math.max(maxY, y);
                    }
                }
            }
        });

        // Apply padding
        const padding = 30;
        minX = Math.max(0, minX - padding);
        minY = Math.max(0, minY - padding);
        maxX = Math.min(originalWidth, maxX + padding);
        maxY = Math.min(originalHeight, maxY + padding);

        // Fallback to full canvas if no content detected
        if (!isFinite(minX) || !isFinite(minY)) {
            minX = 0;
            minY = 0;
            maxX = originalWidth;
            maxY = originalHeight;
        }

        const contentWidth = maxX - minX;
        const contentHeight = maxY - minY;

        // Set viewBox to crop to content area
        svgElement.setAttribute('viewBox', `${minX} ${minY} ${contentWidth} ${contentHeight}`);
        svgElement.setAttribute('width', contentWidth);
        svgElement.setAttribute('height', contentHeight);

        // Create PDF (landscape or portrait based on content)
        const { jsPDF } = window.jspdf;
        const orientation = contentWidth > contentHeight ? 'landscape' : 'portrait';
        
        // Use A4 as base and scale
        const a4Width = orientation === 'landscape' ? 297 : 210;
        const a4Height = orientation === 'landscape' ? 210 : 297;
        
        // Scale to fit A4 with margins
        const marginMm = 10;
        const availableWidth = a4Width - (marginMm * 2);
        const availableHeight = a4Height - (marginMm * 2);
        
        const scaleX = availableWidth / (contentWidth * 0.264583); // px to mm
        const scaleY = availableHeight / (contentHeight * 0.264583);
        const scale = Math.min(scaleX, scaleY, 1); // Don't scale up

        const finalWidth = contentWidth * 0.264583 * scale;
        const finalHeight = contentHeight * 0.264583 * scale;
        
        const offsetX = marginMm + (availableWidth - finalWidth) / 2;
        const offsetY = marginMm + (availableHeight - finalHeight) / 2;

        const pdf = new jsPDF({
            orientation: orientation,
            unit: 'mm',
            format: 'a4'
        });

        // Convert SVG to PDF
        await pdf.svg(svgElement, {
            x: offsetX,
            y: offsetY,
            width: finalWidth,
            height: finalHeight
        });

        // Download the PDF
        pdf.save(filename || 'diagram-export.pdf');

        console.log('PDF exported successfully - cropped to content bounds');
    } catch (error) {
        console.error('Error exporting PDF:', error);
        // Fallback to PNG export
        try {
            await window.downloadPNGFallback(svgContent, filename);
        } catch (e) {
            alert('Error exporting: ' + error.message);
        }
    }
};

// Helper function to load external scripts
function loadScript(url) {
    return new Promise((resolve, reject) => {
        // Check if already loaded
        if (document.querySelector(`script[src="${url}"]`)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

// PNG Fallback export using canvas
window.downloadPNGFallback = function(svgContent, filename) {
    return new Promise((resolve, reject) => {
        try {
            const parser = new DOMParser();
            const svgDoc = parser.parseFromString(svgContent, 'image/svg+xml');
            const svgElement = svgDoc.documentElement;
            
            const width = parseFloat(svgElement.getAttribute('width')) || 2000;
            const height = parseFloat(svgElement.getAttribute('height')) || 2000;
            
            // Create canvas
            const canvas = document.createElement('canvas');
            const scale = 2; // 2x for better quality
            canvas.width = width * scale;
            canvas.height = height * scale;
            const ctx = canvas.getContext('2d');
            ctx.scale(scale, scale);
            ctx.fillStyle = 'white';
            ctx.fillRect(0, 0, width, height);
            
            // Convert SVG to image
            const img = new Image();
            const blob = new Blob([svgContent], { type: 'image/svg+xml;charset=utf-8' });
            const url = URL.createObjectURL(blob);
            
            img.onload = function() {
                ctx.drawImage(img, 0, 0, width, height);
                URL.revokeObjectURL(url);
                
                canvas.toBlob(function(pngBlob) {
                    const pngUrl = URL.createObjectURL(pngBlob);
                    const a = document.createElement('a');
                    a.href = pngUrl;
                    a.download = (filename || 'diagram-export').replace('.pdf', '') + '.png';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    URL.revokeObjectURL(pngUrl);
                    resolve();
                }, 'image/png', 1.0);
            };
            
            img.onerror = function(e) {
                URL.revokeObjectURL(url);
                reject(new Error('Failed to render SVG to canvas'));
            };
            
            img.src = url;
        } catch (error) {
            reject(error);
        }
    });
};

// Method using browser print dialog with optional area selection
window.downloadPDFViaPrint = function (svgContent, x, y, width, height) {
    try {
        // Parse the SVG to modify if area is selected
        let finalSvgContent = svgContent;

        if (x !== null && y !== null && width !== null && height !== null) {
            // Create a viewBox that crops to the selected area
            const parser = new DOMParser();
            const svgDoc = parser.parseFromString(svgContent, 'image/svg+xml');
            const svgElement = svgDoc.documentElement;

            // Set viewBox to crop area
            svgElement.setAttribute('viewBox', `${x} ${y} ${width} ${height}`);
            svgElement.setAttribute('width', width);
            svgElement.setAttribute('height', height);

            // Serialize back to string
            finalSvgContent = new XMLSerializer().serializeToString(svgElement);
        }

        // Create a new window for printing
        const printWindow = window.open('', '_blank', 'width=800,height=600');

        printWindow.document.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <title>DFD Export - Print to PDF</title>
                <style>
                    body { 
                        margin: 0; 
                        padding: 20px;
                        display: flex;
                        justify-content: center;
                        align-items: flex-start;
                        background: white;
                    }
                    svg {
                        max-width: 100%;
                        height: auto;
                        border: 1px solid #ccc;
                    }
                    @media print {
                        body { 
                            padding: 0;
                            display: block;
                        }
                        svg {
                            border: none;
                        }
                        @page { 
                            margin: 0.5cm;
                            size: landscape;
                        }
                    }
                </style>
            </head>
            <body>
                ${finalSvgContent}
                <script>
                    // Auto-trigger print dialog after load
                    window.onload = function() {
                        setTimeout(() => {
                            window.print();
                        }, 500);
                    };
                <\/script>
            </body>
            </html>
        `);

        printWindow.document.close();

        console.log('Print dialog will open automatically');
    } catch (error) {
        console.error('Error opening print dialog:', error);
        alert('Error opening print dialog: ' + error.message);
    }
};

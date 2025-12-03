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

        // Get SVG dimensions
        const width = parseFloat(svgElement.getAttribute('width')) || 1920;
        const height = parseFloat(svgElement.getAttribute('height')) || 1080;

        // Create PDF (landscape orientation for typical diagrams)
        const { jsPDF } = window.jspdf;
        const pdf = new jsPDF({
            orientation: width > height ? 'landscape' : 'portrait',
            unit: 'px',
            format: [width, height]
        });

        // Convert SVG to PDF
        await pdf.svg(svgElement, {
            x: 0,
            y: 0,
            width: width,
            height: height
        });

        // Download the PDF
        pdf.save(filename);

        console.log('PDF exported successfully');
    } catch (error) {
        console.error('Error exporting PDF:', error);
        alert('Error exporting PDF: ' + error.message);
    }
};

// Helper function to load external scripts
function loadScript(url) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

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

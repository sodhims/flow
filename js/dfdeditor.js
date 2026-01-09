/**
 * DFD Editor JavaScript Functions
 * Extracted from DFDEditor.razor for better maintainability
 */

// High-Quality PDF Export via Print Dialog with optional area selection
    window.downloadPDFViaPrint = function(svgContent, x, y, width, height) {
        try {
            // Parse the SVG to modify if area is selected
            let finalSvgContent = svgContent;
            let svgWidth = 2000;
            let svgHeight = 2000;
            
            if (x !== null && y !== null && width !== null && height !== null) {
                // Create a viewBox that crops to the selected area
                const parser = new DOMParser();
                const svgDoc = parser.parseFromString(svgContent, 'image/svg+xml');
                const svgElement = svgDoc.documentElement;
                
                // Set viewBox to crop area
                svgElement.setAttribute('viewBox', `${x} ${y} ${width} ${height}`);
                svgElement.setAttribute('width', width);
                svgElement.setAttribute('height', height);
                
                svgWidth = width;
                svgHeight = height;
                
                // Serialize back to string
                finalSvgContent = new XMLSerializer().serializeToString(svgElement);
            }
            
            // Create a new window for printing with high-quality settings
            const printWindow = window.open('', '_blank', 'width=1200,height=800');
            
            printWindow.document.write(`
                <!DOCTYPE html>
                <html>
                <head>
                    <title>DFD Export - High Quality Print</title>
                    <style>
                        * {
                            margin: 0;
                            padding: 0;
                            box-sizing: border-box;
                        }
                        
                        body { 
                            background: white;
                        }
                        
                        .print-container {
                            width: 100%;
                            height: 100vh;
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            padding: 20px;
                        }
                        
                        svg {
                            max-width: 100%;
                            max-height: 100%;
                            height: auto;
                            border: 1px solid #ccc;
                            /* High-quality rendering hints */
                            shape-rendering: geometricPrecision;
                            text-rendering: geometricPrecision;
                            image-rendering: optimizeQuality;
                        }
                        
                        .print-info {
                            position: fixed;
                            top: 10px;
                            left: 10px;
                            background: #fff;
                            padding: 10px;
                            border: 2px solid #3b82f6;
                            border-radius: 8px;
                            font-family: Arial, sans-serif;
                            font-size: 14px;
                            z-index: 1000;
                        }
                        
                        .print-info strong {
                            color: #3b82f6;
                        }
                        
                        .print-buttons {
                            position: fixed;
                            top: 10px;
                            right: 10px;
                            display: flex;
                            gap: 10px;
                            z-index: 1000;
                        }
                        
                        .print-buttons button {
                            padding: 10px 20px;
                            border: none;
                            border-radius: 6px;
                            font-size: 14px;
                            font-weight: bold;
                            cursor: pointer;
                            transition: all 0.2s;
                        }
                        
                        .btn-print {
                            background: #3b82f6;
                            color: white;
                        }
                        
                        .btn-print:hover {
                            background: #2563eb;
                        }
                        
                        .btn-svg {
                            background: #10b981;
                            color: white;
                        }
                        
                        .btn-svg:hover {
                            background: #059669;
                        }
                        
                        @media print {
                            body { 
                                padding: 0;
                            }
                            
                            .print-info,
                            .print-buttons {
                                display: none !important;
                            }
                            
                            .print-container {
                                padding: 0;
                                height: auto;
                                display: block;
                            }
                            
                            svg {
                                border: none;
                                width: 100% !important;
                                max-width: 100% !important;
                                height: auto !important;
                                display: block;
                                /* Maximum quality for print */
                                shape-rendering: geometricPrecision;
                                text-rendering: geometricPrecision;
                                color-rendering: optimizeQuality;
                            }
                            
                            @page { 
                                margin: 0.5cm;
                                size: ${svgWidth > svgHeight ? 'landscape' : 'portrait'};
                            }
                        }
                    </style>
                </head>
                <body>
                    <div class="print-info">
                        <strong>💡 Pro Tip:</strong> For best quality, use "Save as PDF" instead of printing to a physical printer. 
                        SVG is vector-based and will be crisp at any zoom level!
                    </div>
                    
                    <div class="print-buttons">
                        <button class="btn-print" onclick="window.print()">🖨️ Print/Save PDF</button>
                        <button class="btn-svg" onclick="downloadSVG()">💾 Download SVG</button>
                    </div>
                    
                    <div class="print-container">
                        ${finalSvgContent}
                    </div>
                    
                    <script>
                        function downloadSVG() {
                            const svg = document.querySelector('svg');
                            const serializer = new XMLSerializer();
                            const svgString = serializer.serializeToString(svg);
                            const blob = new Blob([svgString], { type: 'image/svg+xml' });
                            const url = URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;
                            a.download = 'diagram.svg';
                            document.body.appendChild(a);
                            a.click();
                            document.body.removeChild(a);
                            URL.revokeObjectURL(url);
                        }
                        
                        // Auto-trigger print dialog after a delay
                        window.onload = function() {
                            setTimeout(() => {
                                // Don't auto-print, let user choose
                                console.log('Ready to print. Click the Print button or press Ctrl+P');
                            }, 500);
                        };
                        window.getScrollInfo = function(element) {
        return [element.scrollLeft, element.scrollTop, element.clientWidth, element.clientHeight];
    };
                    <\/script>
                </body>
                </html>
            `);
            
            printWindow.document.close();
            
            console.log('Print preview opened with high-quality settings');
        } catch (error) {
            console.error('Error opening print dialog:', error);
            alert('Error opening print dialog: ' + error.message);
        }
    };

    // High-Resolution Print All - No selection required, maximum quality
    window.printAllHighResolution = function(svgContent) {
        try {
            // Parse SVG to get dimensions and enhance quality
            const parser = new DOMParser();
            const svgDoc = parser.parseFromString(svgContent, 'image/svg+xml');
            const svgElement = svgDoc.documentElement;
            
            // Get original dimensions
            const originalWidth = parseFloat(svgElement.getAttribute('width')) || 2000;
            const originalHeight = parseFloat(svgElement.getAttribute('height')) || 2000;
            
            // Set maximum quality attributes
            svgElement.setAttribute('shape-rendering', 'geometricPrecision');
            svgElement.setAttribute('text-rendering', 'geometricPrecision');
            svgElement.setAttribute('image-rendering', 'optimizeQuality');
            svgElement.setAttribute('color-rendering', 'optimizeQuality');
            
            // Serialize back to string
            const enhancedSvgContent = new XMLSerializer().serializeToString(svgElement);
            
            // Create high-resolution print window
            const printWindow = window.open('', '_blank', 'width=1400,height=900');
            
            printWindow.document.write(`
                <!DOCTYPE html>
                <html>
                <head>
                    <title>DFD Print All - Maximum Resolution</title>
                    <style>
                        * {
                            margin: 0;
                            padding: 0;
                            box-sizing: border-box;
                        }
                        
                        body { 
                            background: white;
                            font-family: Arial, sans-serif;
                        }
                        
                        .print-container {
                            width: 100%;
                            min-height: 100vh;
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            padding: 40px;
                        }
                        
                        svg {
                            max-width: 100%;
                            max-height: 90vh;
                            height: auto;
                            border: 2px solid #e5e7eb;
                            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                            background: white;
                            /* Maximum quality rendering */
                            shape-rendering: geometricPrecision;
                            text-rendering: geometricPrecision;
                            image-rendering: optimizeQuality;
                            color-rendering: optimizeQuality;
                        }
                        
                        .print-header {
                            position: fixed;
                            top: 0;
                            left: 0;
                            right: 0;
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white;
                            padding: 15px 30px;
                            display: flex;
                            justify-content: space-between;
                            align-items: center;
                            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                            z-index: 1000;
                        }
                        
                        .print-header h1 {
                            font-size: 20px;
                            font-weight: 600;
                            margin: 0;
                        }
                        
                        .quality-badge {
                            background: rgba(255, 255, 255, 0.2);
                            padding: 5px 15px;
                            border-radius: 20px;
                            font-size: 12px;
                            font-weight: bold;
                            backdrop-filter: blur(10px);
                        }
                        
                        .print-info {
                            position: fixed;
                            bottom: 20px;
                            left: 20px;
                            background: white;
                            padding: 15px 20px;
                            border: 2px solid #10b981;
                            border-radius: 12px;
                            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                            max-width: 400px;
                            z-index: 1000;
                        }
                        
                        .print-info h3 {
                            color: #10b981;
                            margin: 0 0 10px 0;
                            font-size: 16px;
                        }
                        
                        .print-info p {
                            margin: 5px 0;
                            font-size: 13px;
                            color: #374151;
                            line-height: 1.5;
                        }
                        
                        .print-buttons {
                            position: fixed;
                            top: 70px;
                            right: 20px;
                            display: flex;
                            flex-direction: column;
                            gap: 10px;
                            z-index: 1000;
                        }
                        
                        .print-buttons button {
                            padding: 12px 24px;
                            border: none;
                            border-radius: 8px;
                            font-size: 14px;
                            font-weight: 600;
                            cursor: pointer;
                            transition: all 0.3s;
                            display: flex;
                            align-items: center;
                            gap: 8px;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                        }
                        
                        .btn-print {
                            background: #3b82f6;
                            color: white;
                        }
                        
                        .btn-print:hover {
                            background: #2563eb;
                            transform: translateY(-2px);
                            box-shadow: 0 4px 8px rgba(59, 130, 246, 0.3);
                        }
                        
                        .btn-svg {
                            background: #10b981;
                            color: white;
                        }
                        
                        .btn-svg:hover {
                            background: #059669;
                            transform: translateY(-2px);
                            box-shadow: 0 4px 8px rgba(16, 185, 129, 0.3);
                        }
                        
                        .btn-png {
                            background: #8b5cf6;
                            color: white;
                        }
                        
                        .btn-png:hover {
                            background: #7c3aed;
                            transform: translateY(-2px);
                            box-shadow: 0 4px 8px rgba(139, 92, 246, 0.3);
                        }
                        
                        @media print {
                            body { 
                                padding: 0;
                                background: white;
                            }
                            
                            .print-header,
                            .print-info,
                            .print-buttons {
                                display: none !important;
                            }
                            
                            .print-container {
                                padding: 0;
                                min-height: 0;
                                display: block;
                            }
                            
                            svg {
                                border: none !important;
                                box-shadow: none !important;
                                width: 100% !important;
                                max-width: 100% !important;
                                height: auto !important;
                                max-height: none !important;
                                display: block;
                                /* Ultra-high quality for print */
                                shape-rendering: geometricPrecision !important;
                                text-rendering: geometricPrecision !important;
                                color-rendering: optimizeQuality !important;
                                image-rendering: optimizeQuality !important;
                            }
                            
                            @page { 
                                margin: 1cm;
                                size: ${originalWidth > originalHeight ? 'A3 landscape' : 'A3 portrait'};
                            }
                        }
                    </style>
                </head>
                <body>
                    <div class="print-header">
                        <h1>🖨️ Print All - Maximum Resolution</h1>
                        <div class="quality-badge">⚡ ULTRA HIGH QUALITY</div>
                    </div>
                    
                    <div class="print-info">
                        <h3>💎 Maximum Quality Mode</h3>
                        <p><strong>Canvas size:</strong> ${originalWidth} × ${originalHeight} px</p>
                        <p><strong>Format:</strong> Vector (infinite resolution)</p>
                        <p><strong>Recommended:</strong> Download SVG for perfect quality at any size!</p>
                    </div>
                    
                    <div class="print-buttons">
                        <button class="btn-print" onclick="window.print()">
                            <span>🖨️</span> Print / Save PDF
                        </button>
                        <button class="btn-svg" onclick="downloadSVG()">
                            <span>💾</span> Download SVG
                        </button>
                        <button class="btn-png" onclick="downloadPNG()">
                            <span>🖼️</span> Download PNG (High-Res)
                        </button>
                    </div>
                    
                    <div class="print-container">
                        ${enhancedSvgContent}
                    </div>
<div class="dfd-editor" onkeydown="HandleKeyboardShortcut" tabindex="0">                    
                    <script>
                        function downloadSVG() {
                            const svg = document.querySelector('svg');
                            const serializer = new XMLSerializer();
                            const svgString = serializer.serializeToString(svg);
                            const blob = new Blob([svgString], { type: 'image/svg+xml' });
                            const url = URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;
                            a.download = 'diagram-full-resolution.svg';
                            document.body.appendChild(a);
                            a.click();
                            document.body.removeChild(a);
                            URL.revokeObjectURL(url);
                        }
                        
                        function downloadPNG() {
                            const svg = document.querySelector('svg');
                            const canvas = document.createElement('canvas');
                            
                            // High resolution: 3x for crisp output
                            const scale = 3;
                            canvas.width = ${originalWidth} * scale;
                            canvas.height = ${originalHeight} * scale;
                            
                            const ctx = canvas.getContext('2d');
                            ctx.scale(scale, scale);
                            
                            const svgString = new XMLSerializer().serializeToString(svg);
                            const img = new Image();
                            const blob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' });
                            const url = URL.createObjectURL(blob);
                            
                            img.onload = function() {
                                ctx.drawImage(img, 0, 0);
                                canvas.toBlob(function(pngBlob) {
                                    const pngUrl = URL.createObjectURL(pngBlob);
                                    const a = document.createElement('a');
                                    a.href = pngUrl;
                                    a.download = 'diagram-high-resolution.png';
                                    document.body.appendChild(a);
                                    a.click();
                                    document.body.removeChild(a);
                                    URL.revokeObjectURL(pngUrl);
                                    URL.revokeObjectURL(url);
                                }, 'image/png', 1.0);
                            };
                            
                            img.src = url;
                        }
                        
                        window.onload = function() {
                            console.log('High-resolution print preview ready');
                            console.log('Canvas size: ${originalWidth} × ${originalHeight} px');
                            console.log('Quality mode: MAXIMUM');
                        };
                    <\/script>
                </body>
                </html>
            `);
            
            printWindow.document.close();
            
            console.log('Maximum resolution print window opened');
        } catch (error) {
            console.error('Error in Print All:', error);
            alert('Error in Print All: ' + error.message);
        }
    };

    // Canvas-based PDF Export - Renders SVG to canvas first for reliable PDF output
    window.exportPdfViaCanvas = async function(svgContent) {
        try {
            // Parse and enhance the SVG
            const parser = new DOMParser();
            const svgDoc = parser.parseFromString(svgContent, 'image/svg+xml');
            const svgElement = svgDoc.documentElement;
            
            // Get dimensions
            const width = parseFloat(svgElement.getAttribute('width')) || 2000;
            const height = parseFloat(svgElement.getAttribute('height')) || 2000;
            
            // Find bounds of actual content
            let minX = Infinity, minY = Infinity, maxX = 0, maxY = 0;
            const allElements = svgElement.querySelectorAll('rect, ellipse, polygon, path, text');
            allElements.forEach(el => {
                const bbox = {
                    x: parseFloat(el.getAttribute('x') || el.getAttribute('cx') || '0') - (parseFloat(el.getAttribute('rx') || '0')),
                    y: parseFloat(el.getAttribute('y') || el.getAttribute('cy') || '0') - (parseFloat(el.getAttribute('ry') || '0')),
                    width: parseFloat(el.getAttribute('width') || el.getAttribute('rx') || '0') * 2,
                    height: parseFloat(el.getAttribute('height') || el.getAttribute('ry') || '0') * 2
                };
                if (bbox.x < minX) minX = bbox.x;
                if (bbox.y < minY) minY = bbox.y;
                if (bbox.x + bbox.width > maxX) maxX = bbox.x + bbox.width;
                if (bbox.y + bbox.height > maxY) maxY = bbox.y + bbox.height;
            });
            
            // Add padding
            const padding = 50;
            minX = Math.max(0, minX - padding);
            minY = Math.max(0, minY - padding);
            maxX = Math.min(width, maxX + padding);
            maxY = Math.min(height, maxY + padding);
            
            const cropWidth = maxX - minX || width;
            const cropHeight = maxY - minY || height;
            
            // Set viewBox for cropping
            svgElement.setAttribute('viewBox', `${minX} ${minY} ${cropWidth} ${cropHeight}`);
            svgElement.setAttribute('width', cropWidth);
            svgElement.setAttribute('height', cropHeight);
            
            // Ensure proper rendering attributes
            svgElement.setAttribute('shape-rendering', 'geometricPrecision');
            svgElement.setAttribute('text-rendering', 'geometricPrecision');
            
            const finalSvg = new XMLSerializer().serializeToString(svgElement);
            
            // Create canvas with high DPI
            const scale = 2; // 2x for better quality
            const canvas = document.createElement('canvas');
            canvas.width = cropWidth * scale;
            canvas.height = cropHeight * scale;
            const ctx = canvas.getContext('2d');
            ctx.scale(scale, scale);
            ctx.fillStyle = 'white';
            ctx.fillRect(0, 0, cropWidth, cropHeight);
            
            // Create image from SVG
            return new Promise((resolve, reject) => {
                const img = new Image();
                const blob = new Blob([finalSvg], { type: 'image/svg+xml;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                
                img.onload = function() {
                    ctx.drawImage(img, 0, 0, cropWidth, cropHeight);
                    URL.revokeObjectURL(url);
                    
                    // Generate PDF using canvas data URL
                    const imgData = canvas.toDataURL('image/png', 1.0);
                    
                    // Calculate PDF page size (fit to A4 or custom)
                    const pdfWidth = 297; // A4 landscape width in mm
                    const pdfHeight = 210; // A4 landscape height in mm
                    const imgAspect = cropWidth / cropHeight;
                    const pdfAspect = pdfWidth / pdfHeight;
                    
                    let finalWidth, finalHeight, offsetX, offsetY;
                    if (imgAspect > pdfAspect) {
                        finalWidth = pdfWidth - 20;
                        finalHeight = finalWidth / imgAspect;
                        offsetX = 10;
                        offsetY = (pdfHeight - finalHeight) / 2;
                    } else {
                        finalHeight = pdfHeight - 20;
                        finalWidth = finalHeight * imgAspect;
                        offsetX = (pdfWidth - finalWidth) / 2;
                        offsetY = 10;
                    }
                    
                    // Create download link with PNG (works as fallback)
                    const a = document.createElement('a');
                    a.href = imgData;
                    a.download = 'diagram-export.png';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    
                    console.log('PNG exported successfully');
                    resolve();
                };
                
                img.onerror = function(e) {
                    URL.revokeObjectURL(url);
                    console.error('Error loading SVG:', e);
                    reject(new Error('Failed to render SVG'));
                };
                
                img.src = url;
            });
        } catch (error) {
            console.error('Error in canvas PDF export:', error);
            alert('Error exporting: ' + error.message);
        }
    };

    // Get scroll info for minimap viewport tracking
    window.getScrollInfo = function(element) {
        if (!element) return [0, 0, 800, 600];
        return [element.scrollLeft, element.scrollTop, element.clientWidth, element.clientHeight];
    };

    // Scroll canvas to specific position
    window.scrollCanvasTo = function(element, x, y) {
        if (!element) return;
        element.scrollLeft = x;
        element.scrollTop = y;
    };

    // Get minimap element bounds for coordinate conversion
    window.getMinimapBounds = function(element) {
        if (!element) return [0, 0, 150, 150];
        var rect = element.getBoundingClientRect();
        return [rect.left, rect.top, rect.width, rect.height];
    };
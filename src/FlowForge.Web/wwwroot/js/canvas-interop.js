/**
 * FlowForge Canvas Interop
 * JavaScript interop for SVG canvas interactions:
 * drag-and-drop, pan/zoom, element positioning, clipboard.
 */

/**
 * Get the bounding rect of an element.
 * @param {HTMLElement} element
 * @returns {{ x: number, y: number, width: number, height: number }}
 */
export function getBoundingRect(element) {
    if (!element) return { x: 0, y: 0, width: 0, height: 0 };
    const rect = element.getBoundingClientRect();
    return {
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height
    };
}

/**
 * Get the SVG point from a mouse event, accounting for SVG transforms.
 * @param {SVGSVGElement} svgElement
 * @param {number} clientX
 * @param {number} clientY
 * @returns {{ x: number, y: number }}
 */
export function getSvgPoint(svgElement, clientX, clientY) {
    if (!svgElement) return { x: 0, y: 0 };
    const pt = svgElement.createSVGPoint();
    pt.x = clientX;
    pt.y = clientY;
    const ctm = svgElement.getScreenCTM();
    if (!ctm) return { x: clientX, y: clientY };
    const svgPt = pt.matrixTransform(ctm.inverse());
    return { x: svgPt.x, y: svgPt.y };
}

/**
 * Copy text to clipboard.
 * @param {string} text
 * @returns {Promise<boolean>}
 */
export async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        // Fallback for older browsers
        try {
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.opacity = '0';
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
            return true;
        } catch {
            return false;
        }
    }
}

/**
 * Initialize drag-and-drop for a node element within an SVG canvas.
 * @param {DotNetObjectReference} dotNetRef - Reference to the Blazor component
 * @param {string} nodeId - The node's unique identifier
 * @param {SVGElement} nodeElement - The SVG group element for the node
 * @param {SVGSVGElement} svgCanvas - The root SVG element
 */
export function initNodeDrag(dotNetRef, nodeId, nodeElement, svgCanvas) {
    if (!nodeElement || !svgCanvas) return;

    let isDragging = false;
    let startX, startY;
    let originalX, originalY;

    nodeElement.addEventListener('mousedown', (e) => {
        if (e.button !== 0) return; // left button only
        e.stopPropagation();
        isDragging = true;

        const pt = getSvgPoint(svgCanvas, e.clientX, e.clientY);
        startX = pt.x;
        startY = pt.y;

        const transform = nodeElement.getAttribute('transform') || 'translate(0, 0)';
        const match = transform.match(/translate\(([^,]+),\s*([^)]+)\)/);
        originalX = match ? parseFloat(match[1]) : 0;
        originalY = match ? parseFloat(match[2]) : 0;

        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);
    });

    function handleMouseMove(e) {
        if (!isDragging) return;

        const pt = getSvgPoint(svgCanvas, e.clientX, e.clientY);
        const dx = pt.x - startX;
        const dy = pt.y - startY;

        // Snap to 20px grid
        const newX = Math.round((originalX + dx) / 20) * 20;
        const newY = Math.round((originalY + dy) / 20) * 20;

        nodeElement.setAttribute('transform', `translate(${newX}, ${newY})`);

        // Notify Blazor (debounced)
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnNodeMoved', nodeId, newX, newY);
        }
    }

    function handleMouseUp() {
        isDragging = false;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
    }
}

/**
 * Initialize pan and zoom for an SVG canvas.
 * @param {SVGSVGElement} svgCanvas
 * @param {DotNetObjectReference} dotNetRef
 */
export function initPanZoom(svgCanvas, dotNetRef) {
    if (!svgCanvas) return;

    let isPanning = false;
    let panStartX, panStartY;
    let currentPanX = 0, currentPanY = 0;
    let currentZoom = 1;

    svgCanvas.addEventListener('wheel', (e) => {
        e.preventDefault();
        const delta = e.deltaY > 0 ? -0.05 : 0.05;
        currentZoom = Math.max(0.2, Math.min(3, currentZoom + delta));

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnZoomChanged', currentZoom);
        }
    }, { passive: false });

    svgCanvas.addEventListener('mousedown', (e) => {
        // Middle button or Ctrl+Left for panning
        if (e.button === 1 || (e.button === 0 && e.ctrlKey)) {
            e.preventDefault();
            isPanning = true;
            panStartX = e.clientX - currentPanX;
            panStartY = e.clientY - currentPanY;
        }
    });

    document.addEventListener('mousemove', (e) => {
        if (!isPanning) return;
        currentPanX = e.clientX - panStartX;
        currentPanY = e.clientY - panStartY;

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnPanChanged', currentPanX, currentPanY);
        }
    });

    document.addEventListener('mouseup', () => {
        isPanning = false;
    });
}

/**
 * Smoothly scroll an element into view.
 * @param {HTMLElement} element
 */
export function scrollIntoView(element) {
    if (!element) return;
    element.scrollIntoView({ behavior: 'smooth', block: 'end' });
}

/**
 * Get the current window dimensions.
 * @returns {{ width: number, height: number }}
 */
export function getWindowDimensions() {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
}

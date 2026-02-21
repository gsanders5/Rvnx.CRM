function initializeNetworkGraph(nodes, links) {
    const container = document.getElementById('network-graph');
    if (!container) return;

    const width = container.clientWidth;
    const height = container.clientHeight;

    const svgNS = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(svgNS, "svg");
    svg.setAttribute("width", "100%");
    svg.setAttribute("height", "100%");
    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    container.appendChild(svg);

    // Fullscreen behavior
    const fullscreenBtn = document.getElementById('fullscreen-btn');
    const networkCard = document.getElementById('network-card');
    
    if (fullscreenBtn && networkCard) {
        fullscreenBtn.addEventListener('click', () => {
            if (!document.fullscreenElement) {
                if (networkCard.requestFullscreen) {
                    networkCard.requestFullscreen();
                } else if (networkCard.webkitRequestFullscreen) { /* Safari */
                    networkCard.webkitRequestFullscreen();
                } else if (networkCard.msRequestFullscreen) { /* IE11 */
                    networkCard.msRequestFullscreen();
                }
            } else {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                } else if (document.webkitExitFullscreen) { /* Safari */
                    document.webkitExitFullscreen();
                } else if (document.msExitFullscreen) { /* IE11 */
                    document.msExitFullscreen();
                }
            }
        });

        document.addEventListener('fullscreenchange', () => {
            if (document.fullscreenElement) {
                networkCard.classList.add('fixed-top', 'w-100', 'h-100', 'z-3', 'm-0');
                container.style.height = "calc(100vh - 72px)"; // Account for card header
                fullscreenBtn.innerHTML = '<i class="bi bi-fullscreen-exit"></i>';
            } else {
                networkCard.classList.remove('fixed-top', 'w-100', 'h-100', 'z-3', 'm-0');
                container.style.height = "500px";
                fullscreenBtn.innerHTML = '<i class="bi bi-arrows-fullscreen"></i>';
            }
        });
    }

    // Drag to Pan State
    let isPanning = false;
    let panStartX = 0;
    let panStartY = 0;
    let currentPanX = 0;
    let currentPanY = 0;

    // A group to hold everything so we can pan the camera
    const cameraLayer = document.createElementNS(svgNS, "g");
    svg.appendChild(cameraLayer);

    // Apply panning listener to the SVG itself
    svg.style.cursor = "grab";
    svg.addEventListener('mousedown', (e) => {
        if (e.target === svg) { // Ensure we didn't click a node
            isPanning = true;
            svg.style.cursor = "grabbing";
            panStartX = e.clientX - currentPanX;
            panStartY = e.clientY - currentPanY;
        }
    });

    document.addEventListener('mousemove', (e) => {
        if (isPanning) {
            currentPanX = e.clientX - panStartX;
            currentPanY = e.clientY - panStartY;
            cameraLayer.setAttribute("transform", `translate(${currentPanX}, ${currentPanY})`);
        }
    });

    document.addEventListener('mouseup', () => {
        if (isPanning) {
            isPanning = false;
            svg.style.cursor = "grab";
        }
    });

    // Create Link elements first
    const linkElements = links.map(link => {
        const line = document.createElementNS(svgNS, "line");
        line.setAttribute("stroke", "#6c757d");
        line.setAttribute("stroke-width", "2");
        line.setAttribute("stroke-opacity", "0.5");
        cameraLayer.appendChild(line);
        return { data: link, el: line };
    });

    // Create Node elements
    const nodeElements = nodes.map(node => {
        const g = document.createElementNS(svgNS, "g");
        g.style.cursor = "grab";

        const circle = document.createElementNS(svgNS, "circle");
        circle.setAttribute("r", "20");
        circle.setAttribute("fill", "#0d6efd");
        circle.setAttribute("stroke", "#fff");
        circle.setAttribute("stroke-width", "2");
        g.appendChild(circle);

        const text = document.createElementNS(svgNS, "text");
        text.setAttribute("dy", "35");
        text.setAttribute("text-anchor", "middle");
        text.setAttribute("fill", "currentColor");
        text.style.fill = "var(--bs-body-color)";
        text.setAttribute("font-size", "12");
        text.textContent = node.name;
        g.appendChild(text);

        cameraLayer.appendChild(g);

        // Drag logic
        g.addEventListener('mousedown', (e) => startDrag(e, node));
        g.addEventListener('click', (e) => {
            if (!node.wasDragged) {
                window.location.href = `/Contacts/Details/${node.id}`;
            }
        });

        return { data: node, el: g };
    });

    // Initialize positions
    nodes.forEach(node => {
        node.x = Math.random() * width;
        node.y = Math.random() * height;
        node.vx = 0;
        node.vy = 0;
    });

    // Drag State
    let draggedNode = null;

    function startDrag(e, node) {
        e.preventDefault();
        // Prevent pan start
        e.stopPropagation();
        
        draggedNode = node;
        node.wasDragged = false;
        document.addEventListener('mousemove', drag);
        document.addEventListener('mouseup', endDrag);
    }

    function drag(e) {
        if (!draggedNode) return;
        draggedNode.wasDragged = true;
        const rect = svg.getBoundingClientRect();
        
        // Subtract current pan offset to convert screen space to SVG space
        draggedNode.x = (e.clientX - rect.left) - currentPanX;
        draggedNode.y = (e.clientY - rect.top) - currentPanY;
        
        draggedNode.vx = 0; // Stop velocity while dragging
        draggedNode.vy = 0;
    }

    function endDrag() {
        draggedNode = null;
        document.removeEventListener('mousemove', drag);
        document.removeEventListener('mouseup', endDrag);
    }

    function tick() {
        // Simulation Parameters
        const k = 3000; // Repulsion constant
        const linkDistance = 180;
        const damping = 0.9;
        const centerForce = 0.005;

        // Repulsion
        for (let i = 0; i < nodes.length; i++) {
            const n1 = nodes[i];
            for (let j = i + 1; j < nodes.length; j++) {
                const n2 = nodes[j];
                const dx = n1.x - n2.x;
                const dy = n1.y - n2.y;
                let dist = Math.sqrt(dx * dx + dy * dy);
                if (dist === 0) dist = 0.1;

                const force = k / (dist * dist);
                const fx = (dx / dist) * force;
                const fy = (dy / dist) * force;

                if (n1 !== draggedNode) {
                    n1.vx += fx;
                    n1.vy += fy;
                }
                if (n2 !== draggedNode) {
                    n2.vx -= fx;
                    n2.vy -= fy;
                }
            }
        }

        // Attraction
        links.forEach(link => {
            const source = nodes.find(n => n.id === link.source);
            const target = nodes.find(n => n.id === link.target);
            if (!source || !target) return;

            const dx = target.x - source.x;
            const dy = target.y - source.y;
            let dist = Math.sqrt(dx * dx + dy * dy);
            if (dist === 0) dist = 0.1;

            const force = (dist - linkDistance) * 0.05;
            const fx = (dx / dist) * force;
            const fy = (dy / dist) * force;

            if (source !== draggedNode) {
                source.vx += fx;
                source.vy += fy;
            }
            if (target !== draggedNode) {
                target.vx -= fx;
                target.vy -= fy;
            }
        });

        // Center & Bounds & Update
        nodes.forEach(node => {
            if (node === draggedNode) return;

            // Center
            node.vx += (width / 2 - node.x) * centerForce;
            node.vy += (height / 2 - node.y) * centerForce;

            // Update
            node.x += node.vx;
            node.y += node.vy;
            node.vx *= damping;
            node.vy *= damping;

            // Bounds - Removed to allow nodes to spread into the panned space!
        });

        // Render Updates
        nodeElements.forEach(item => {
            item.el.setAttribute("transform", `translate(${item.data.x}, ${item.data.y})`);
        });

        linkElements.forEach(item => {
             const source = nodes.find(n => n.id === item.data.source);
             const target = nodes.find(n => n.id === item.data.target);
             if (source && target) {
                 item.el.setAttribute("x1", source.x);
                 item.el.setAttribute("y1", source.y);
                 item.el.setAttribute("x2", target.x);
                 item.el.setAttribute("y2", target.y);
             }
        });

        requestAnimationFrame(tick);
    }

    if (nodes.length > 0) {
        tick();
    }
}

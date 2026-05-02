function initializeNetworkGraph(nodes, links) {
  const container = document.getElementById("network-graph");
  if (!container) return;

  let currentWidth = container.clientWidth;
  let currentHeight = container.clientHeight;

  const svgNS = "http://www.w3.org/2000/svg";
  const svg = document.createElementNS(svgNS, "svg");
  svg.setAttribute("width", "100%");
  svg.setAttribute("height", "100%");
  svg.setAttribute("viewBox", `0 0 ${currentWidth} ${currentHeight}`);
  svg.style.touchAction = "none";
  container.appendChild(svg);

  const resizeObserver = new ResizeObserver(() => {
    if (container.clientWidth > 0 && container.clientHeight > 0) {
      currentWidth = container.clientWidth;
      currentHeight = container.clientHeight;
      svg.setAttribute("viewBox", `0 0 ${currentWidth} ${currentHeight}`);
    }
  });
  resizeObserver.observe(container);

  const fullscreenBtn = document.getElementById("fullscreen-btn");
  const networkCard = document.getElementById("network-card");

  if (fullscreenBtn && networkCard) {
    fullscreenBtn.addEventListener("click", () => {
      if (!document.fullscreenElement) {
        if (networkCard.requestFullscreen) {
          networkCard.requestFullscreen();
        } else if (networkCard.webkitRequestFullscreen) {
          /* Safari */
          networkCard.webkitRequestFullscreen();
        } else if (networkCard.msRequestFullscreen) {
          /* IE11 */
          networkCard.msRequestFullscreen();
        }
      } else {
        if (document.exitFullscreen) {
          document.exitFullscreen();
        } else if (document.webkitExitFullscreen) {
          /* Safari */
          document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
          /* IE11 */
          document.msExitFullscreen();
        }
      }
    });

    document.addEventListener("fullscreenchange", () => {
      if (document.fullscreenElement) {
        networkCard.classList.add("fixed-top", "w-100", "h-100", "z-3", "m-0");
        container.style.height = "calc(100vh - 72px)"; // Account for card header
        fullscreenBtn.innerHTML = '<i class="bi bi-fullscreen-exit"></i>';
      } else {
        networkCard.classList.remove(
          "fixed-top",
          "w-100",
          "h-100",
          "z-3",
          "m-0",
        );
        container.style.height = "500px";
        fullscreenBtn.innerHTML = '<i class="bi bi-arrows-fullscreen"></i>';
      }
    });
  }

  let isPanning = false;
  let panPointerId = null;
  let panStartX = 0;
  let panStartY = 0;
  let currentPanX = 0;
  let currentPanY = 0;
  let currentScale = 1;

  const activePointers = new Map();
  let pinchState = null;

  const cameraLayer = document.createElementNS(svgNS, "g");
  svg.appendChild(cameraLayer);

  const defs = document.createElementNS(svgNS, "defs");
  const clipPath = document.createElementNS(svgNS, "clipPath");
  clipPath.setAttribute("id", "circle-clip");
  const clipCircle = document.createElementNS(svgNS, "circle");
  clipCircle.setAttribute("r", "20");
  clipCircle.setAttribute("cx", "0");
  clipCircle.setAttribute("cy", "0");
  clipPath.appendChild(clipCircle);
  defs.appendChild(clipPath);
  svg.appendChild(defs);

  const updateTransform = () => {
    cameraLayer.setAttribute(
      "transform",
      `translate(${currentPanX}, ${currentPanY}) scale(${currentScale})`,
    );
  };

  svg.style.cursor = "grab";

  const clampScale = (s) => Math.max(0.1, Math.min(5, s));

  const startPinch = () => {
    const ids = [...activePointers.keys()];
    if (ids.length < 2) return;
    const [id1, id2] = ids;
    const p1 = activePointers.get(id1);
    const p2 = activePointers.get(id2);
    const rect = svg.getBoundingClientRect();
    const centerX = (p1.x + p2.x) / 2 - rect.left;
    const centerY = (p1.y + p2.y) / 2 - rect.top;
    const dist = Math.hypot(p1.x - p2.x, p1.y - p2.y) || 1;
    pinchState = {
      id1,
      id2,
      initialDist: dist,
      initialScale: currentScale,
      worldX: (centerX - currentPanX) / currentScale,
      worldY: (centerY - currentPanY) / currentScale,
    };
  };

  const updatePinch = () => {
    if (!pinchState) return;
    const p1 = activePointers.get(pinchState.id1);
    const p2 = activePointers.get(pinchState.id2);
    if (!p1 || !p2) return;
    const currentDist = Math.hypot(p1.x - p2.x, p1.y - p2.y);
    const newScale = clampScale(
      pinchState.initialScale * (currentDist / pinchState.initialDist),
    );
    const rect = svg.getBoundingClientRect();
    const centerX = (p1.x + p2.x) / 2 - rect.left;
    const centerY = (p1.y + p2.y) / 2 - rect.top;

    currentPanX = centerX - pinchState.worldX * newScale;
    currentPanY = centerY - pinchState.worldY * newScale;
    currentScale = newScale;
    updateTransform();
  };

  // Capture phase: always track the pointer and detect pinch before any
  // child (node) handlers run in the bubble phase.
  svg.addEventListener(
    "pointerdown",
    (e) => {
      activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

      if (activePointers.size >= 2) {
        if (isPanning) {
          isPanning = false;
          panPointerId = null;
          svg.style.cursor = "grab";
        }
        if (draggedNode) {
          // Prevent a navigation click when aborting a drag for pinch
          draggedNode.wasDragged = true;
          draggedNode = null;
          dragPointerId = null;
        }
        startPinch();
      }
    },
    true,
  );

  svg.addEventListener("pointerdown", (e) => {
    if (pinchState) return;
    if (draggedNode) return;
    if (e.target !== svg) return;

    isPanning = true;
    panPointerId = e.pointerId;
    svg.style.cursor = "grabbing";
    panStartX = e.clientX - currentPanX;
    panStartY = e.clientY - currentPanY;
  });

  document.addEventListener("pointermove", (e) => {
    if (!activePointers.has(e.pointerId)) return;
    activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

    if (pinchState) {
      updatePinch();
      return;
    }

    if (isPanning && e.pointerId === panPointerId) {
      currentPanX = e.clientX - panStartX;
      currentPanY = e.clientY - panStartY;
      updateTransform();
      return;
    }

    if (draggedNode && e.pointerId === dragPointerId) {
      drag(e);
    }
  });

  const handlePointerEnd = (e) => {
    if (!activePointers.delete(e.pointerId)) return;

    if (pinchState && activePointers.size < 2) {
      pinchState = null;
    }

    if (isPanning && e.pointerId === panPointerId) {
      isPanning = false;
      panPointerId = null;
      svg.style.cursor = "grab";
    }

    if (draggedNode && e.pointerId === dragPointerId) {
      endDrag();
    }
  };

  document.addEventListener("pointerup", handlePointerEnd);
  document.addEventListener("pointercancel", handlePointerEnd);

  svg.addEventListener("wheel", (e) => {
    e.preventDefault();
    const zoomIntensity = 0.1;
    const direction = e.deltaY > 0 ? -1 : 1;
    const factor = 1 + direction * zoomIntensity;
    const newScale = clampScale(currentScale * factor);
    const scaleRatio = newScale / currentScale;

    const rect = svg.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    currentPanX = mouseX - (mouseX - currentPanX) * scaleRatio;
    currentPanY = mouseY - (mouseY - currentPanY) * scaleRatio;
    currentScale = newScale;

    updateTransform();
  });

  const zoomInBtn = document.getElementById("zoom-in-btn");
  const zoomOutBtn = document.getElementById("zoom-out-btn");
  const resetZoomBtn = document.getElementById("reset-zoom-btn");

  const handleButtonZoom = (factor) => {
    const newScale = clampScale(currentScale * factor);
    const scaleRatio = newScale / currentScale;

    const rect = svg.getBoundingClientRect();
    const cx = rect.width / 2;
    const cy = rect.height / 2;

    currentPanX = cx - (cx - currentPanX) * scaleRatio;
    currentPanY = cy - (cy - currentPanY) * scaleRatio;
    currentScale = newScale;
    updateTransform();
  };

  if (zoomInBtn) {
    zoomInBtn.addEventListener("click", () => handleButtonZoom(1.2));
  }

  if (zoomOutBtn) {
    zoomOutBtn.addEventListener("click", () => handleButtonZoom(1 / 1.2));
  }

  if (resetZoomBtn) {
    resetZoomBtn.addEventListener("click", () => {
      currentScale = 1;
      currentPanX = 0;
      currentPanY = 0;
      updateTransform();
    });
  }

  const getGenderColor = (gender) => {
    if (!gender) return "#9e9e9e"; // Unset/Unknown
    const g = gender.toLowerCase();
    if (g === "male") return "#1a73e8";
    if (g === "female") return "#e91e8c";
    if (g === "non-binary") return "#7c3aed";
    return "#9e9e9e"; // Other/Unspecified
  };

  const linkElements = links.map((link) => {
    const line = document.createElementNS(svgNS, "line");
    line.setAttribute("stroke", "#6c757d");
    line.setAttribute("stroke-width", "2");
    line.setAttribute("stroke-opacity", "0.5");
    cameraLayer.appendChild(line);
    return { data: link, el: line };
  });

  const nodeElements = nodes.map((node) => {
    const g = document.createElementNS(svgNS, "g");
    g.style.cursor = "grab";

    if (node.isDeceased) {
      // Subdued treatment matches the fa-book-skull indicator used elsewhere — lower
      // opacity on the whole node group plus a tooltip title element accessible to
      // both pointer hover and assistive tech.
      g.setAttribute("opacity", "0.55");
      const groupTitle = document.createElementNS(svgNS, "title");
      groupTitle.textContent = `${node.name} (Deceased)`;
      g.appendChild(groupTitle);
    }

    const circle = document.createElementNS(svgNS, "circle");
    circle.setAttribute("r", "20");
    circle.setAttribute("fill", getGenderColor(node.gender));
    circle.setAttribute("stroke", "#fff");
    circle.setAttribute("stroke-width", "2");
    g.appendChild(circle);

    if (node.photoUrl) {
      const image = document.createElementNS(svgNS, "image");
      image.setAttribute("href", node.photoUrl);
      image.setAttribute("width", "40");
      image.setAttribute("height", "40");
      image.setAttribute("x", "-20");
      image.setAttribute("y", "-20");
      image.setAttribute("clip-path", "url(#circle-clip)");

      // Error handling: if image fails, remove it or hide it
      image.addEventListener("error", () => {
        image.style.display = "none";
        // Also hide the ring if we hide the image?
        // No, keep the ring as it frames the fallback circle too, or remove it.
        // The fallback circle has stroke. The ring has stroke.
        // If image is hidden, we see fallback circle which has stroke.
        // If ring is on top, we see ring stroke.
        // If we have ring + fallback circle stroke, it's fine.
      });

      g.appendChild(image);

      // Ring on top to maintain stroke consistency
      const ring = document.createElementNS(svgNS, "circle");
      ring.setAttribute("r", "20");
      ring.setAttribute("fill", "none");
      ring.setAttribute("stroke", "#fff");
      ring.setAttribute("stroke-width", "2");
      g.appendChild(ring);
    }

    const text = document.createElementNS(svgNS, "text");
    text.setAttribute("dy", "35");
    text.setAttribute("text-anchor", "middle");
    text.setAttribute("fill", "currentColor");
    text.setAttribute("font-size", "12");
    text.textContent = node.name;
    g.appendChild(text);

    cameraLayer.appendChild(g);

    g.addEventListener("mouseover", () => {
      linkElements.forEach((linkItem) => {
        if (
          linkItem.data.source === node.id ||
          linkItem.data.target === node.id
        ) {
          linkItem.el.setAttribute("stroke", "#0d6efd"); // Bootstrap primary blue
          linkItem.el.setAttribute("stroke-width", "3");
          linkItem.el.setAttribute("stroke-opacity", "1");
        }
      });
    });

    g.addEventListener("mouseout", () => {
      linkElements.forEach((linkItem) => {
        if (
          linkItem.data.source === node.id ||
          linkItem.data.target === node.id
        ) {
          linkItem.el.setAttribute("stroke", "#6c757d"); // Default gray
          linkItem.el.setAttribute("stroke-width", "2");
          linkItem.el.setAttribute("stroke-opacity", "0.5");
        }
      });
    });

    g.addEventListener("pointerdown", (e) => startDrag(e, node));
    g.addEventListener("click", (e) => {
      if (!node.wasDragged) {
        window.location.href = `/Contacts/Details/${node.id}`;
      }
    });

    return { data: node, el: g };
  });

  nodes.forEach((node) => {
    node.x = Math.random() * currentWidth;
    node.y = Math.random() * currentHeight;
    node.vx = 0;
    node.vy = 0;
  });

  let draggedNode = null;
  let dragPointerId = null;
  let simulationAlpha = 1.0;

  function startDrag(e, node) {
    // The svg capture-phase pointerdown already tracked this pointer.
    // If a pinch has started or another pointer is already active, don't
    // start a node drag.
    if (pinchState) return;
    if (activePointers.size >= 2) return;

    e.preventDefault();

    // Wake up physics slightly when interacting so the network responds naturally
    if (simulationAlpha < 0.1) {
      simulationAlpha = 0.1;
    }

    draggedNode = node;
    dragPointerId = e.pointerId;
    node.wasDragged = false;
  }

  function drag(e) {
    if (!draggedNode) return;
    draggedNode.wasDragged = true;
    const rect = svg.getBoundingClientRect();

    draggedNode.x = (e.clientX - rect.left - currentPanX) / currentScale;
    draggedNode.y = (e.clientY - rect.top - currentPanY) / currentScale;

    draggedNode.vx = 0; // Stop velocity while dragging
    draggedNode.vy = 0;
  }

  function endDrag() {
    draggedNode = null;
    dragPointerId = null;
  }

  function tick() {
    // Decay alpha by roughly 1% per frame, leaving a tiny fraction for loose interaction
    if (simulationAlpha > 0.005) {
      simulationAlpha *= 0.99;
    } else {
      simulationAlpha = 0.005;
    }

    const k = 2500 * simulationAlpha; // Repulsion constant
    const linkForceWeight = 0.05 * simulationAlpha;
    const centerForce = 0.008 * simulationAlpha;

    const damping = 0.85;
    const linkDistance = 180;

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

    links.forEach((link) => {
      const source = nodes.find((n) => n.id === link.source);
      const target = nodes.find((n) => n.id === link.target);
      if (!source || !target) return;

      const dx = target.x - source.x;
      const dy = target.y - source.y;
      let dist = Math.sqrt(dx * dx + dy * dy);
      if (dist === 0) dist = 0.1;

      const force = (dist - linkDistance) * linkForceWeight;
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

    nodes.forEach((node) => {
      if (node === draggedNode) return;

      node.vx += (currentWidth / 2 - node.x) * centerForce;
      node.vy += (currentHeight / 2 - node.y) * centerForce;

      node.x += node.vx;
      node.y += node.vy;
      node.vx *= damping;
      node.vy *= damping;

      // Bounds - Removed to allow nodes to spread into the panned space!
    });

    nodeElements.forEach((item) => {
      item.el.setAttribute(
        "transform",
        `translate(${item.data.x}, ${item.data.y})`,
      );
    });

    linkElements.forEach((item) => {
      const source = nodes.find((n) => n.id === item.data.source);
      const target = nodes.find((n) => n.id === item.data.target);
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

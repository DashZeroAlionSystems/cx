(function(){
  'use strict';
  const { Core } = window.WB;

  const UI = {};
  let dragEdge = null;

  UI.renderGrid = function renderGrid() {
    try {
      if (!Core.elements.canvas || !Core.elements.chkGrid) {
        return; // Elements not ready yet
      }
      
      Core.elements.canvas.style.backgroundImage = Core.elements.chkGrid.checked 
        ? 'linear-gradient(#1f2633 1px, transparent 1px),linear-gradient(90deg,#1f2633 1px, transparent 1px)' 
        : 'none';
    } catch (error) {
      Core.handleError(error, 'Render Grid');
    }
  };

  UI.centerOfNode = function centerOfNode(id){ 
    const node = Core.state.nodes.find(x => x.id === id); 
    if (!node) return {x: 0, y: 0}; 
    const { width, height } = Core.CONSTANTS.NODE_DIMENSIONS;
    return { 
      x: node.x + width / 2, 
      y: node.y + height / 2 
    }; 
  };

  function onDragMove(e){ 
    if (!dragEdge) return; 
    
    try {
      const startPoint = UI.centerOfNode(dragEdge.fromId); 
      const currentPoint = Core.getCanvasPoint(e); 
      const midX = (startPoint.x + currentPoint.x) / 2; 
      const pathD = `M ${startPoint.x} ${startPoint.y} C ${midX} ${startPoint.y}, ${midX} ${currentPoint.y}, ${currentPoint.x} ${currentPoint.y}`; 
      dragEdge.temp.setAttribute('d', pathD); 
    } catch (error) {
      Core.handleError(error, 'Drag Move');
    }
  }
  
  function onDragEnd(e){ 
    if (!dragEdge) return; 
    
    try {
      const target = e.target; 
      const dropNode = target && target.closest && target.closest('.node'); 
      
      if (dropNode && target.classList && target.classList.contains('in')){ 
        UI.addEdge(dragEdge.fromId, dropNode.dataset.id, dragEdge.fromPort || 'out'); 
      }
      
      dragEdge.temp?.remove(); 
      dragEdge = null; 
      document.removeEventListener('pointermove', onDragMove); 
    } catch (error) {
      Core.handleError(error, 'Drag End');
    }
  }

  function startDragEdge(fromId, fromPort){
    try {
      dragEdge = { 
        fromId, 
        fromPort, 
        temp: document.createElementNS('http://www.w3.org/2000/svg','path') 
      };
      
      const tempPath = dragEdge.temp;
      tempPath.setAttribute('class', 'edge flow');
      tempPath.setAttribute('stroke-dasharray', '4 4');
      tempPath.setAttribute('opacity', '0.9');
      Core.elements.edgesSvg.appendChild(tempPath);
      
    document.addEventListener('pointermove', onDragMove);
    document.addEventListener('pointerup', onDragEnd, { once: true });
    } catch (error) {
      Core.handleError(error, 'Start Drag Edge');
    }
  }

  UI.findNodeEl = function findNodeEl(id){ return Core.elements.canvas.querySelector('.node[data-id="'+id+'"]'); };

  UI.createArrowMarker = function createArrowMarker() {
    const defs = document.createElementNS('http://www.w3.org/2000/svg','defs');
    const marker = document.createElementNS('http://www.w3.org/2000/svg','marker');
    
    marker.setAttribute('id', 'arrow');
    marker.setAttribute('viewBox', '0 0 10 10');
    marker.setAttribute('refX', '10');
    marker.setAttribute('refY', '5');
    marker.setAttribute('markerWidth', String(Core.CONSTANTS.ARROW_MARKER_SIZE));
    marker.setAttribute('markerHeight', String(Core.CONSTANTS.ARROW_MARKER_SIZE));
    marker.setAttribute('orient', 'auto-start-reverse');
    
    const poly = document.createElementNS('http://www.w3.org/2000/svg','polyline');
    poly.setAttribute('points', '0,0 10,5 0,10 1,5 0,0');
    poly.setAttribute('style', 'fill:#7a8aa3');
    
    marker.appendChild(poly);
    defs.appendChild(marker);
    return defs;
  };

  UI.createEdgePath = function createEdgePath(edge) {
    const startPoint = UI.centerOfNode(edge.from);
    const endPoint = UI.centerOfNode(edge.to);
    const midX = (startPoint.x + endPoint.x) / 2;
    const pathD = `M ${startPoint.x} ${startPoint.y} C ${midX} ${startPoint.y}, ${midX} ${endPoint.y}, ${endPoint.x} ${endPoint.y}`;
    
    const path = document.createElementNS('http://www.w3.org/2000/svg','path');
    path.setAttribute('d', pathD);
    path.setAttribute('class', 'edge flow');
    path.dataset.id = edge.id;
    path.setAttribute('marker-end', 'url(#arrow)');
    
    if (edge.kind === 'data') {
      path.classList.add('data');
    }
    
    return { path, midX, midY: (startPoint.y + endPoint.y) / 2 };
  };

  UI.createEdgeLabel = function createEdgeLabel(label, midX, midY) {
    if (!label) return null;
    
    const text = document.createElementNS('http://www.w3.org/2000/svg','text');
    text.setAttribute('x', String(midX));
    text.setAttribute('y', String(midY - 6));
    text.setAttribute('fill', '#9aa3b2');
    text.setAttribute('font-size', '10');
    text.setAttribute('text-anchor', 'middle');
    text.textContent = label;
    
    return text;
  };

  UI.drawEdges = function drawEdges() {
    try {
      // Use DocumentFragment for better performance
      const fragment = document.createDocumentFragment();
      
      // Create arrow marker once
      const defs = UI.createArrowMarker();
      fragment.appendChild(defs);
      
      // Draw all edges in fragment
      Core.state.edges.forEach(edge => {
        const { path, midX, midY } = UI.createEdgePath(edge);
        fragment.appendChild(path);
        
        // Add edge label if exists
        const label = Core.state.edgeLabels.get(edge.id);
        const labelElement = UI.createEdgeLabel(label, midX, midY);
        if (labelElement) {
          fragment.appendChild(labelElement);
        }
      });
      
      // Clear and append all at once
      Core.elements.edgesSvg.innerHTML = '';
      Core.elements.edgesSvg.appendChild(fragment);
    } catch (error) {
      Core.handleError(error, 'Draw Edges');
    }
  };

  // Execution visuals
  UI.clearExecution = function clearExecution(){
    try{
      document.querySelectorAll('.node').forEach(n => {
        n.classList.remove('exec-active','exec-done');
      });
      if (Core.elements.edgesSvg){
        Core.elements.edgesSvg.querySelectorAll('path.edge').forEach(p => p.classList.remove('exec-active'));
      }
    } catch(err){ Core.handleError(err, 'Clear Execution'); }
  };

  UI.markNodeActive = function markNodeActive(id){
    const el = UI.findNodeEl(id);
    if (el) el.classList.add('exec-active');
  };

  UI.markNodeDone = function markNodeDone(id){
    const el = UI.findNodeEl(id);
    if (el){
      el.classList.remove('exec-active');
      el.classList.add('exec-done');
    }
  };

  UI.markEdgeActive = function markEdgeActive(edgeId){
    if (!Core.elements.edgesSvg) return;
    const el = Core.elements.edgesSvg.querySelector(`path.edge[data-id="${edgeId}"]`);
    if (el) el.classList.add('exec-active');
  };

  UI.clearEdgeActive = function clearEdgeActive(edgeId){
    if (!Core.elements.edgesSvg) return;
    const el = Core.elements.edgesSvg.querySelector(`path.edge[data-id="${edgeId}"]`);
    if (el) el.classList.remove('exec-active');
  };

  UI.draw = function draw() {
    try {
      // Use DocumentFragment for better performance
      const nodeFragment = document.createDocumentFragment();
      
      // Clear canvas
      Core.elements.canvas.innerHTML = '';
      
      // Create all nodes in fragment first
      for (const node of Core.state.nodes) {
        const nodeEl = UI.createNodeEl(node);
        if (nodeEl) {
          nodeFragment.appendChild(nodeEl);
        }
      }
      
      // Append all nodes at once
      Core.elements.canvas.appendChild(nodeFragment);
      
      // Draw edges
      UI.drawEdges();
    } catch (error) {
      Core.handleError(error, 'Draw');
    }
  };

  UI.addNode = function addNode(type, x, y) {
    try {
      const node = {
        id: Core.uid('n'),
        type,
        title: type,
        x: Math.max(0, x | 0),
        y: Math.max(0, y | 0)
      };
      Core.state.nodes.push(node);
      UI.draw();
      return node;
    } catch (error) {
      Core.handleError(error, 'Add Node');
      return null;
    }
  };

  UI.deleteNode = function deleteNode(id) {
    try {
      Core.state.nodes = Core.state.nodes.filter(n => n.id !== id);
      Core.state.edges = Core.state.edges.filter(e => e.from !== id && e.to !== id);
      
      if (Core.state.selectedId === id) {
        Core.state.selectedId = null;
      }
      
      UI.draw();
    } catch (error) {
      Core.handleError(error, 'Delete Node');
    }
  };

  UI.addEdge = function addEdge(from, to, port) {
    try {
      if (from === to) return;
      
      const id = Core.uid('e');
      const edge = {
        id,
        from,
        to,
        type: 'success',
        port: port || 'out'
      };
      
      Core.state.edges.push(edge);
      
      if (port && port !== 'out') {
        Core.state.edgeLabels.set(id, port);
      }
      
      UI.drawEdges();
    } catch (error) {
      Core.handleError(error, 'Add Edge');
    }
  };

  UI.select = function select(id){ Core.state.selectedId=id; const node=Core.state.nodes.find(n=>n.id===id); document.querySelectorAll('.node').forEach(n=>n.classList.toggle('selected', n.dataset.id===id)); UI.showProps(node||null); };

  UI.createPropertyRow = function createPropertyRow(label, value, inputType = 'div') {
    const row = document.createElement('div');
    row.className = 'row';
    
    const labelEl = document.createElement('label');
    labelEl.textContent = label;
    
    if (inputType === 'div') {
      const valueEl = document.createElement('div');
      valueEl.textContent = value;
      row.appendChild(labelEl);
      row.appendChild(valueEl);
    } else {
      const inputEl = document.createElement('input');
      inputEl.type = inputType;
      inputEl.value = value;
      inputEl.id = `p${label}`;
      row.appendChild(labelEl);
      row.appendChild(inputEl);
    }
    
    return row;
  };

  UI.setupPropertyHandlers = function setupPropertyHandlers(node) {
    const titleInput = Core.elements.propPanel.querySelector('#pTitle')
      || Core.elements.propPanel.querySelector('#pNote');
    if (titleInput) {
      titleInput.addEventListener('input', e => {
        node.title = e.target.value;
        const el = UI.findNodeEl(node.id);
        if (el) {
          el.querySelector('.node-title').textContent = node.title;
        }
      });
    }
    
    ['X', 'Y'].forEach(axis => {
      const input = Core.elements.propPanel.querySelector(`#p${axis}`);
      if (!input) return;
      
      input.addEventListener('input', e => {
        const value = parseInt(e.target.value || '0', 10);
        if (axis === 'X') {
          node.x = value;
        } else {
          node.y = value;
        }
        
        const el = UI.findNodeEl(node.id);
        if (el) {
          el.style.left = node.x + 'px';
          el.style.top = node.y + 'px';
        }
        UI.drawEdges();
      });
    });
  };

  UI.showProps = function showProps(node) {
    try {
      if (!node) {
        Core.elements.propPanel.classList.add('empty');
        Core.elements.propPanel.textContent = 'Select a node';
        return;
      }
      
      Core.elements.propPanel.classList.remove('empty');
      
      const isSticky = node.type === 'Sticky';
      const titleLabel = isSticky ? 'Note' : 'Title';
      
      Core.elements.propPanel.innerHTML = '';
      
      // Add property rows
      Core.elements.propPanel.appendChild(UI.createPropertyRow('Id', node.id));
      Core.elements.propPanel.appendChild(UI.createPropertyRow('Type', node.type));
      Core.elements.propPanel.appendChild(UI.createPropertyRow(titleLabel, node.title || '', 'text'));
      Core.elements.propPanel.appendChild(UI.createPropertyRow('X', node.x, 'number'));
      Core.elements.propPanel.appendChild(UI.createPropertyRow('Y', node.y, 'number'));
      
      UI.setupPropertyHandlers(node);
    } catch (error) {
      Core.handleError(error, 'Show Properties');
    }
  };

  UI.setupNodeBasicProperties = function setupNodeBasicProperties(el, node) {
    el.dataset.id = node.id;
    el.dataset.type = node.type;
    el.style.left = node.x + 'px';
    el.style.top = node.y + 'px';
    
    el.querySelector('.node-icon').textContent = Core.ICONS[node.type] || '';
    el.querySelector('.node-type').textContent = `${Core.ICONS[node.type] || ''} ${node.type}`.trim();
    
    const titleEl = el.querySelector('.node-title');
    titleEl.textContent = node.title || node.type;
    
    if (node.type === 'Sticky') {
      el.classList.add('pill');
      titleEl.contentEditable = true;
    }
    
    if (['Server', 'ServerFarm', 'ServerSwarm'].includes(node.type)) {
      el.classList.add('pulse');
    }
  };

  UI.createPortElement = function createPortElement(port, node) {
    const div = document.createElement('div');
    div.className = 'port out';
    div.dataset.port = port.id;
    div.title = port.label || port.id;
    div.setAttribute('aria-label', `Connect from ${port.label || port.id} port`);
    
    const label = document.createElement('span');
    label.style.marginLeft = '8px';
    label.style.color = 'var(--muted)';
    label.textContent = port.label;
    
    const row = document.createElement('div');
    row.style.display = 'flex';
    row.style.alignItems = 'center';
    row.style.gap = '6px';
    row.appendChild(div);
    row.appendChild(label);
    
    div.addEventListener('pointerdown', e => {
      e.stopPropagation();
      Core.state.connectFrom = node.id;
      Core.state.connectFromPort = port.id;
      startDragEdge(node.id, port.id);
    });
    
    return row;
  };

  UI.setupPortConnections = function setupPortConnections(el, node) {
    const outPort = el.querySelector('.port.out');
    const inPort = el.querySelector('.port.in');
    const outList = el.querySelector('.ports-out-list');
    
    outList.innerHTML = '';
    const ports = Core.defaultPorts(node).out;
    
    for (const port of ports) {
      const portElement = UI.createPortElement(port, node);
      outList.appendChild(portElement);
    }
    
    outPort.addEventListener('pointerdown', e => {
      e.stopPropagation();
      Core.state.connectFrom = node.id;
      Core.state.connectFromPort = 'out';
      startDragEdge(node.id, 'out');
    });
    
    inPort.addEventListener('pointerdown', e => {
      e.stopPropagation();
      if (Core.state.connectFrom && Core.state.connectFrom !== node.id) {
        UI.addEdge(Core.state.connectFrom, node.id, Core.state.connectFromPort || 'out');
        Core.state.connectFrom = null;
        Core.state.connectFromPort = null;
      }
    });
  };

  UI.setupNodeDrag = function setupNodeDrag(el, node) {
    el.addEventListener('pointerdown', ev => {
      if (ev.target.closest('.port')) return;
      if (ev.target.closest('.node-delete')) return;
      
      UI.select(node.id);
      
      const startX = ev.clientX;
      const startY = ev.clientY;
      const rect = el.getBoundingClientRect();
      const offsetX = startX - rect.left;
      const offsetY = startY - rect.top;
      
      function move(e) {
        if (!Core.elements.canvas || !Core.elements.chkSnap) return;
        
        const newX = e.clientX - offsetX - Core.elements.canvas.getBoundingClientRect().left;
        const newY = e.clientY - offsetY - Core.elements.canvas.getBoundingClientRect().top;
        const snapSize = Core.elements.chkSnap.checked ? Core.CONSTANTS.SNAP_GRID_SIZE : Core.CONSTANTS.SNAP_PRECISION;
        
        node.x = Math.max(0, Math.round(newX / snapSize) * snapSize);
        node.y = Math.max(0, Math.round(newY / snapSize) * snapSize);
        
        el.style.left = node.x + 'px';
        el.style.top = node.y + 'px';
        UI.drawEdges();
      }
      
      function up() {
        window.removeEventListener('pointermove', move);
        window.removeEventListener('pointerup', up);
      }
      
      window.addEventListener('pointermove', move);
      window.addEventListener('pointerup', up);
    });
  };

  UI.setupNodeEventHandlers = function setupNodeEventHandlers(el, node) {
    el.querySelector('.node-title').addEventListener('input', e => {
      node.title = e.target.textContent || '';
      if (Core.state.selectedId === node.id) {
        UI.showProps(node);
      }
    });
    
    el.querySelector('.node-delete').addEventListener('click', () => {
      UI.deleteNode(node.id);
    });
    
    el.querySelector('.node-dup').addEventListener('click', () => {
      const originalNode = Core.state.nodes.find(x => x.id === node.id);
      if (!originalNode) return;
      
      const copy = {
        ...originalNode,
        id: Core.uid('n'),
        x: originalNode.x + Core.CONSTANTS.DUPLICATE_OFFSET.x,
        y: originalNode.y + Core.CONSTANTS.DUPLICATE_OFFSET.y,
        title: originalNode.title + ' copy'
      };
      Core.state.nodes.push(copy);
      UI.draw();
    });
    
    el.addEventListener('dblclick', () => {
      if (node.type === 'ServerFarm') {
        spawnServers(node, Core.CONSTANTS.SERVER_FARM_COUNT, Core.CONSTANTS.SERVER_FARM_RADIUS);
      }
      if (node.type === 'ServerSwarm') {
        spawnServers(node, Core.CONSTANTS.SERVER_SWARM_COUNT, Core.CONSTANTS.SERVER_SWARM_RADIUS);
      }
    });
    
    el.addEventListener('click', () => UI.select(node.id));
    
    // Keyboard navigation
    el.addEventListener('keydown', (e) => {
      switch(e.key) {
        case 'Enter':
        case ' ':
          e.preventDefault();
          UI.select(node.id);
          break;
        case 'Delete':
        case 'Backspace':
          e.preventDefault();
          UI.deleteNode(node.id);
          break;
        case 'd':
          if (e.ctrlKey || e.metaKey) {
            e.preventDefault();
            const originalNode = Core.state.nodes.find(x => x.id === node.id);
            if (originalNode) {
              const copy = {
                ...originalNode,
                id: Core.uid('n'),
                x: originalNode.x + Core.CONSTANTS.DUPLICATE_OFFSET.x,
                y: originalNode.y + Core.CONSTANTS.DUPLICATE_OFFSET.y,
                title: originalNode.title + ' copy'
              };
              Core.state.nodes.push(copy);
              UI.draw();
            }
          }
          break;
        case 'ArrowUp':
          e.preventDefault();
          node.y = Math.max(0, node.y - (e.shiftKey ? 10 : 1));
          el.style.top = node.y + 'px';
          UI.drawEdges();
          break;
        case 'ArrowDown':
          e.preventDefault();
          node.y += (e.shiftKey ? 10 : 1);
          el.style.top = node.y + 'px';
          UI.drawEdges();
          break;
        case 'ArrowLeft':
          e.preventDefault();
          node.x = Math.max(0, node.x - (e.shiftKey ? 10 : 1));
          el.style.left = node.x + 'px';
          UI.drawEdges();
          break;
        case 'ArrowRight':
          e.preventDefault();
          node.x += (e.shiftKey ? 10 : 1);
          el.style.left = node.x + 'px';
          UI.drawEdges();
          break;
      }
    });
  };

  UI.createNodeEl = function createNodeEl(node) {
    try {
      const el = Core.elements.tplNode.content.firstElementChild.cloneNode(true);
      
      UI.setupNodeBasicProperties(el, node);
      UI.setupPortConnections(el, node);
      UI.setupNodeDrag(el, node);
      UI.setupNodeEventHandlers(el, node);
      
      return el;
    } catch (error) {
      Core.handleError(error, 'Create Node Element');
      return null;
    }
  };

  function spawnServers(originNode, count, radius) {
    try {
      const angleStep = (Math.PI * 2) / count;
      const created = [];
      
      for (let i = 0; i < count; i++) {
        const angle = i * angleStep;
        const newX = Math.round(originNode.x + Math.cos(angle) * radius);
        const newY = Math.round(originNode.y + Math.sin(angle) * radius);
        
        const server = UI.addNode('Server', newX, newY);
        server.title = `Server-${i + 1}`;
        created.push(server);
      }
      
      const ports = Core.defaultPorts(originNode).out.map(p => p.id);
      for (let i = 0; i < created.length; i++) {
        const port = ports[i] || 'out';
        UI.addEdge(originNode.id, created[i].id, port);
      }
      
    UI.draw();
    } catch (error) {
      Core.handleError(error, 'Spawn Servers');
    }
  }

  window.WB.UI = UI;
})();

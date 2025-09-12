(function(){
  'use strict';
  const Core = {};

  // Constants
  Core.CONSTANTS = {
    NODE_DIMENSIONS: { width: 220, height: 100 },
    SNAP_GRID_SIZE: 10,
    SNAP_PRECISION: 1,
    SERVER_FARM_COUNT: 3,
    SERVER_FARM_RADIUS: 120,
    SERVER_SWARM_COUNT: 6,
    SERVER_SWARM_RADIUS: 160,
    DUPLICATE_OFFSET: { x: 32, y: 24 },
    ARROW_MARKER_SIZE: 6,
    EDGE_STROKE_WIDTH: 2.2,
    ANIMATION_DURATION: 1800
  };

  Core.ICONS = { 
    Start:'▶', End:'■', Task:'▦', If:'◆', Switch:'⇄', 
    Sticky:'📝', Server:'🖥️', ServerFarm:'🏭', ServerSwarm:'🕸️' 
  };

  Core.NODE_RULES = { 
    Start: {inMin:0, inMax:0, outMin:1, outMax:1}, 
    End: {inMin:1, outMin:0, outMax:0}, 
    Task: {inMin:1, outMin:1, outMax:1}, 
    If: {inMin:1, outMin:2, outMax:2}, 
    Switch: {inMin:1, outMin:2}, 
    Sticky: {inMin:0, outMin:0, outMax:0}, 
    Server: {inMin:0, outMin:1, outMax:2}, 
    ServerFarm: {inMin:0, outMin:3}, 
    ServerSwarm: {inMin:0, outMin:6} 
  };

  Core.EDGE_TYPES = ['success','failure','onError','conditional','data'];

  Core.state = { 
    nodes: [], 
    edges: [], 
    edgeLabels: new Map(), 
    selectedId: null, 
    connectFrom: null, 
    connectFromPort: null, 
    nextId: 1 
  };

  Core.uid = function uid(prefix){ 
    return (prefix || 'id') + (Core.state.nextId++); 
  };

  Core.defaultPorts = function defaultPorts(node){
    switch(node.type) {
      case 'If': 
        return { out:[{id:'true',label:'true'},{id:'false',label:'false'}] };
      case 'Switch': 
        return { 
          out:[
            {id:'GET',label:'GET'},
            {id:'POST',label:'POST'},
            {id:'UPDATE',label:'Update'},
            {id:'OPTIONS',label:'OPTIONS'},
            {id:'DELETE',label:'DELETE'},
            {id:'INVALID',label:'INVALID'}
          ] 
        };
      case 'ServerFarm': 
        return { 
          out:[
            {id:'srv1',label:'node-1'},
            {id:'srv2',label:'node-2'},
            {id:'srv3',label:'node-3'}
          ] 
        };
      case 'ServerSwarm': 
        return { 
          out: Array.from({length:6}).map((_,i)=>({
            id:'sw'+(i+1),
            label:'sw-'+(i+1)
          })) 
        };
      case 'Server': 
        return { out:[{id:'out',label:'conn'}] };
      default: 
        return { out:[{id:'out',label:''}] };
    }
  };

  Core.elements = { 
    canvas: null, 
    edgesSvg: null, 
    ctxMenu: null, 
    tplNode: null, 
    propPanel: null, 
    fileInput: null, 
    chkGrid: null, 
    chkSnap: null, 
    issuesBadge: null, 
    selExamples: null, 
    btnLoadExample: null,
    btnRun: null,
    btnStop: null 
  };

  Core.getCanvasPoint = function getCanvasPoint(e){ 
    const rect = Core.elements.canvas.getBoundingClientRect(); 
    return { 
      x: e.clientX - rect.left, 
      y: e.clientY - rect.top 
    }; 
  };

  // Error handling utility
  Core.handleError = function handleError(error, context = 'Unknown') {
    console.error(`WorkflowBuilder Error [${context}]:`, error);
    
    // Show user-friendly error message
    const message = error.message || error.toString();
    Core.showNotification(`Error in ${context}: ${message}`, 'error');
    
    return error;
  };

  // User notification system
  Core.showNotification = function showNotification(message, type = 'info', duration = 3000) {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    
    // Style the notification
    Object.assign(notification.style, {
      position: 'fixed',
      top: '20px',
      right: '20px',
      padding: '12px 16px',
      borderRadius: '8px',
      color: '#fff',
      fontSize: '14px',
      fontWeight: '500',
      zIndex: '10000',
      maxWidth: '300px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
      transform: 'translateX(100%)',
      transition: 'transform 0.3s ease'
    });
    
    // Set background color based on type
    const colors = {
      info: '#3b82f6',
      success: '#10b981',
      warning: '#f59e0b',
      error: '#ef4444'
    };
    notification.style.backgroundColor = colors[type] || colors.info;
    
    document.body.appendChild(notification);
    
    // Animate in
    setTimeout(() => {
      notification.style.transform = 'translateX(0)';
    }, 10);
    
    // Auto remove
    setTimeout(() => {
      notification.style.transform = 'translateX(100%)';
      setTimeout(() => {
        if (notification.parentNode) {
          notification.parentNode.removeChild(notification);
        }
      }, 300);
    }, duration);
  };

  // Validation utility
  Core.validateState = function validateState() {
    try {
      if (!Core.elements.canvas) throw new Error('Canvas element not found');
      if (!Core.elements.edgesSvg) throw new Error('Edges SVG element not found');
      if (!Core.elements.tplNode) throw new Error('Node template not found');
      return true;
    } catch (error) {
      Core.handleError(error, 'State Validation');
      return false;
    }
  };

  // Simple runner state
  Core.runtime = {
    isRunning: false,
    abort: false,
    stepDelayMs: 600
  };

  function sleep(ms){ return new Promise(r => setTimeout(r, ms)); }

  function getOutgoingEdges(nodeId){
    return Core.state.edges.filter(e => e.from === nodeId);
  }

  function chooseNextEdge(node, outgoing){
    if (outgoing.length === 0) return null;
    if (node.type === 'If'){
      return outgoing.find(e => (e.port||'').toLowerCase() === 'true')
        || outgoing.find(e => (e.port||'').toLowerCase() === 'false')
        || outgoing[0];
    }
    if (node.type === 'Switch'){
      const prefs = ['GET','POST','UPDATE','OPTIONS','DELETE','INVALID'];
      for (const p of prefs){
        const m = outgoing.find(e => (e.port||'').toUpperCase() === p);
        if (m) return m;
      }
      return outgoing[0];
    }
    if (outgoing.length === 1) return outgoing[0];
    return outgoing[0];
  }

  Core.stopRun = function stopRun(){
    Core.runtime.abort = true;
  };

  Core.startRun = async function startRun(){
    if (Core.runtime.isRunning) return;
    try {
      Core.runtime.isRunning = true;
      Core.runtime.abort = false;

      // Serialize and validate
      const model = window.WB.IO.serialize();
      const issues = window.WB.IO.validate(model);
      if (issues.length > 0){
        Core.showNotification(`${issues.length} issue(s) – fix before running`, 'warning');
        Core.runtime.isRunning = false;
        return;
      }

      // Find start
      const start = Core.state.nodes.find(n => n.type === 'Start');
      if (!start){
        Core.showNotification('No Start node found', 'error');
        Core.runtime.isRunning = false;
        return;
      }

      // Visual reset
      window.WB.UI.clearExecution();

      let current = start;
      const visited = new Set();

      while (current && !Core.runtime.abort){
        window.WB.UI.markNodeActive(current.id);
        await sleep(Core.runtime.stepDelayMs);
        window.WB.UI.markNodeDone(current.id);

        if (current.type === 'End') break;

        const outgoing = getOutgoingEdges(current.id);
        const nextEdge = chooseNextEdge(current, outgoing);
        if (!nextEdge) break;

        window.WB.UI.markEdgeActive(nextEdge.id);
        await sleep(Math.max(200, Core.runtime.stepDelayMs / 2));
        window.WB.UI.clearEdgeActive(nextEdge.id);

        const nextNode = Core.state.nodes.find(n => n.id === nextEdge.to);
        if (!nextNode) break;

        // Simple loop guard
        const key = `${current.id}->${nextNode.id}`;
        if (visited.has(key)){
          Core.showNotification('Loop detected, stopping run', 'warning');
          break;
        }
        visited.add(key);
        current = nextNode;
      }

      if (Core.runtime.abort){
        Core.showNotification('Execution stopped', 'info');
      } else {
        Core.showNotification('Execution finished', 'success');
      }
    } catch (error) {
      Core.handleError(error, 'Run');
    } finally {
      Core.runtime.isRunning = false;
    }
  };

  window.WB = window.WB || {};
  window.WB.Core = Core;
})();

(function(){
  'use strict';
  const Core = {};

  Core.CONSTANTS = {
    NODE_DIMENSIONS: { width: 220, height: 100 },
    SNAP_GRID_SIZE: 10,
    SNAP_PRECISION: 1
  };

  Core.state = { nodes: [], edges: [], nextId: 1 };
  Core.uid = function uid(prefix){ return (prefix || 'id') + (Core.state.nextId++); };

  Core.elements = { canvas:null, edgesSvg:null, tplNode:null };

  Core.handleError = function handleError(error, context = 'Unknown'){
    console.error(`WorkflowBuilder Error [${context}]:`, error);
    const message = error.message || error.toString();
    Core.showNotification(`Error in ${context}: ${message}`, 'error');
    return error;
  };

  Core.showNotification = function showNotification(message, type = 'info', duration = 3000){
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    document.body.appendChild(notification);
    setTimeout(() => { notification.remove(); }, duration);
  };

  Core.validateState = function validateState(){
    try {
      if (!Core.elements.canvas) throw new Error('Canvas element not found');
      if (!Core.elements.edgesSvg) throw new Error('Edges SVG element not found');
      if (!Core.elements.tplNode) throw new Error('Node template not found');
      return true;
    } catch (error) { Core.handleError(error, 'State Validation'); return false; }
  };

  Core.runtime = { isRunning:false, abort:false, stepDelayMs:600 };
  function sleep(ms){ return new Promise(r => setTimeout(r, ms)); }

  Core.startRun = async function startRun(){
    if (Core.runtime.isRunning) return;
    try {
      Core.runtime.isRunning = true;
      Core.runtime.abort = false;
      const model = window.WB.IO.serialize();
      const issues = window.WB.IO.validate(model);
      if (issues.length > 0){ Core.showNotification(`${issues.length} issue(s)`, 'warning'); Core.runtime.isRunning = false; return; }
      const start = Core.state.nodes.find(n => n.type === 'Start');
      if (!start){ Core.showNotification('No Start node found', 'error'); Core.runtime.isRunning = false; return; }
      window.WB.UI.clearExecution();
      let current = start; const visited = new Set();
      while (current && !Core.runtime.abort){
        window.WB.UI.markNodeActive(current.id);
        await sleep(Core.runtime.stepDelayMs);
        window.WB.UI.markNodeDone(current.id);
        if (current.type === 'End') break;
        const outgoing = Core.state.edges.filter(e => e.from === current.id);
        const nextEdge = outgoing[0]; if (!nextEdge) break;
        const nextNode = Core.state.nodes.find(n => n.id === nextEdge.to); if (!nextNode) break;
        const key = `${current.id}->${nextNode.id}`; if (visited.has(key)) { Core.showNotification('Loop detected', 'warning'); break; }
        visited.add(key); current = nextNode;
      }
      Core.showNotification(Core.runtime.abort ? 'Execution stopped' : 'Execution finished', 'info');
    } catch (error) { Core.handleError(error, 'Run'); }
    finally { Core.runtime.isRunning = false; }
  };

  window.WB = window.WB || {}; window.WB.Core = Core;
})();
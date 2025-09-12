(function(){
  'use strict';
  const { Core } = window.WB;

  const IO = {};

  IO.serialize = function serialize() {
    try {
      return {
        nodes: Core.state.nodes.map(n => ({
          id: n.id,
          type: n.type,
          title: n.title,
          x: n.x,
          y: n.y
        })),
        edges: Core.state.edges.map(e => ({
          id: e.id,
          from: e.from,
          to: e.to,
          type: e.type,
          port: e.port
        }))
      };
    } catch (error) {
      Core.handleError(error, 'Serialization');
      return { nodes: [], edges: [] };
    }
  };

  IO.deserialize = function deserialize(model) {
    try {
      if (!model || typeof model !== 'object') {
        throw new Error('Invalid model data');
      }

      Core.state.nodes = (model.nodes || []).map(n => ({
        id: n.id,
        type: n.type,
        title: n.title,
        x: n.x | 0,
        y: n.y | 0
      }));

      Core.state.edges = (model.edges || []).map(e => ({
        id: e.id,
        from: e.from,
        to: e.to,
        type: e.type || 'success',
        port: e.port || 'out'
      }));

      // Rebuild edge labels map
      Core.state.edgeLabels = new Map();
      for (const edge of Core.state.edges) {
        if (edge.port && edge.port !== 'out') {
          Core.state.edgeLabels.set(edge.id, edge.port);
        }
      }

      // Calculate next ID
      const allIds = Core.state.nodes.concat(Core.state.edges).map(x => {
        const match = (x.id || '').match(/(\d+)$/);
        return match ? parseInt(match[1], 10) : 0;
      });
      Core.state.nextId = 1 + Math.max(0, ...allIds);
      Core.state.selectedId = null;

      Core.showNotification('Workflow loaded successfully', 'success');
    } catch (error) {
      Core.handleError(error, 'Deserialization');
    }
  };

  IO.validate = function validate(model){ const issues=[]; const nodesById=new Map(model.nodes.map(n=>[n.id,n])); const inputs=new Map(model.nodes.map(n=>[n.id,0])); const outputs=new Map(model.nodes.map(n=>[n.id,0])); for(const e of model.edges){ if(!nodesById.has(e.from)||!nodesById.has(e.to)){ issues.push({scope:'edge', id:e.id, message:'Edge connects to missing node'}); continue; } outputs.set(e.from,(outputs.get(e.from)||0)+1); inputs.set(e.to,(inputs.get(e.to)||0)+1); } for(const n of model.nodes){ const r=Core.NODE_RULES[n.type]||{inMin:0,outMin:0}; const inC=inputs.get(n.id)||0; const outC=outputs.get(n.id)||0; if(inC<(r.inMin||0)) issues.push({scope:'node', id:n.id, message:`${n.type} requires at least ${r.inMin||0} input(s)`}); if(r.inMax!=null && inC>r.inMax) issues.push({scope:'node', id:n.id, message:`${n.type} allows at most ${r.inMax} input(s)`}); if(outC<(r.outMin||0)) issues.push({scope:'node', id:n.id, message:`${n.type} requires at least ${r.outMin||0} output(s)`}); if(r.outMax!=null && outC>r.outMax) issues.push({scope:'node', id:n.id, message:`${n.type} allows at most ${r.outMax} output(s)`}); } const startCount = model.nodes.filter(n=>n.type==='Start').length; const endCount = model.nodes.filter(n=>n.type==='End').length; if(startCount!==1) issues.push({scope:'graph', id:'graph', message:'Workflow should have exactly one Start'}); if(endCount<1) issues.push({scope:'graph', id:'graph', message:'Workflow should have at least one End'}); return issues; };

  IO.toL1K = function toL1K(model) {
    try {
      const lines = [];
      lines.push('workflow {');
      
      for (const node of model.nodes) {
        const escapedTitle = (node.title || '').replace(/"/g, '\\"');
        lines.push(`  node ${node.id} ${node.type} "${escapedTitle}" @(${node.x},${node.y})`);
      }
      
      for (const edge of model.edges) {
        const label = edge.port && edge.port !== 'out' ? ` :${edge.port}` : '';
        lines.push(`  ${edge.from} -> ${edge.to}${label}`);
      }
      
      lines.push('}');
      return lines.join('\n');
    } catch (error) {
      Core.handleError(error, 'L1K Export');
      return 'workflow {\n}';
    }
  };

  IO.fromL1K = function fromL1K(text) {
    try {
      const model = { nodes: [], edges: [] };
      const nodeRegex = /^\s*node\s+(\S+)\s+(\w+)\s+"([^"]*)"\s*@\((\d+),(\d+)\)\s*$/;
      const edgeRegex = /^\s*(\S+)\s*->\s*(\S+)(?:\s*:(\S+))?\s*$/;
      
      for (const rawLine of text.split(/\r?\n/)) {
        const line = rawLine.trim();
        if (!line || line === 'workflow{' || line === 'workflow {' || line === '}') {
          continue;
        }
        
        let match = line.match(nodeRegex);
        if (match) {
          model.nodes.push({
            id: match[1],
            type: match[2],
            title: match[3],
            x: parseInt(match[4], 10),
            y: parseInt(match[5], 10)
          });
          continue;
        }
        
        match = line.match(edgeRegex);
        if (match) {
          const id = `e${model.edges.length + 1}`;
          model.edges.push({
            id,
            from: match[1],
            to: match[2],
            type: 'success',
            port: match[3] || 'out'
          });
        }
      }
      
      return model;
    } catch (error) {
      Core.handleError(error, 'L1K Import');
      return { nodes: [], edges: [] };
    }
  };

  IO.generateJS = function generateJS(model){ const lines=[]; lines.push('// Generated workflow'); lines.push('const nodes = '+JSON.stringify(model.nodes,null,2)+';'); lines.push('const edges = '+JSON.stringify(model.edges,null,2)+';'); lines.push('export default { nodes, edges };'); return lines.join('\n'); };
  IO.generateCS = function generateCS(model){ const json=JSON.stringify(model).replace(/"/g,'\"'); return '/* Generated Workflow JSON */\npublic static readonly string WorkflowJson = "'+json+'";'; };

  IO.loadExample = function loadExample(kind){ const examples = window.WB.examples || {}; return (examples[kind]||examples.linear)(); };

  IO.downloadText = function downloadText(filename, text){ const blob = new Blob([text], {type:'text/plain'}); const url=URL.createObjectURL(blob); const a=document.createElement('a'); a.href=url; a.download=filename; a.click(); URL.revokeObjectURL(url); };

  window.WB.IO = IO;
})();

(function(){
  'use strict';
  
  // Ensure modules are loaded
  if (!window.WB || !window.WB.Core || !window.WB.UI || !window.WB.IO) {
    console.error('WorkflowBuilder modules not properly loaded');
    return;
  }
  
  const { Core } = window.WB;
  const { UI } = window.WB;
  const { IO } = window.WB;

  // Initialize application
  function initializeApp() {
    try {
      // Bind DOM elements with proper ID mapping
      const elementMappings = {
        canvas: 'canvas',
        edgesSvg: 'edges',  // HTML uses 'edges', code expects 'edgesSvg'
        ctxMenu: 'ctxMenu',
        tplNode: 'tplNode',
        propPanel: 'propPanel',
        fileInput: 'fileInput',
        chkGrid: 'chkGrid',
        chkSnap: 'chkSnap',
        issuesBadge: 'issuesBadge',
        selExamples: 'examplesSelect',  // HTML uses 'examplesSelect'
        btnLoadExample: 'btnLoadExample'
      };
      
      for (const [coreKey, htmlId] of Object.entries(elementMappings)) {
        const element = document.getElementById(htmlId);
        if (!element) {
          throw new Error(`Required element not found: ${htmlId} (mapped to ${coreKey})`);
        }
        Core.elements[coreKey] = element;
      }
      
      // Validate state
      if (!Core.validateState()) {
        throw new Error('State validation failed');
      }
      
      Core.showNotification('WorkflowBuilder initialized', 'success', 2000);
    } catch (error) {
      Core.handleError(error, 'Application Initialization');
    }
  }

  // Bind toolbar event handlers
  function setupToolbarHandlers() {
    const btnImport = document.getElementById('btnImport');
    const btnExport = document.getElementById('btnExport');
    const btnImportL1K = document.getElementById('btnImportL1K');
    const btnExportL1K = document.getElementById('btnExportL1K');
    const btnGenJS = document.getElementById('btnGenJS');
    const btnGenCS = document.getElementById('btnGenCS');
    const btnValidate = document.getElementById('btnValidate');
    const btnRun = document.getElementById('btnRun');
    const btnStop = document.getElementById('btnStop');

    // Export/Import handlers
    btnExport?.addEventListener('click', () => {
      try {
        const data = IO.serialize();
        IO.downloadText('workflow.json', JSON.stringify(data, null, 2));
        Core.showNotification('Workflow exported to JSON', 'success');
      } catch (error) {
        Core.handleError(error, 'JSON Export');
      }
    });

    btnImport?.addEventListener('click', () => {
      Core.elements.fileInput.onchange = async (ev) => {
        try {
          const file = ev.target.files?.[0];
          if (!file) return;
          
          const text = await file.text();
          const data = JSON.parse(text);
          IO.deserialize(data);
          UI.draw();
          updateIssuesBadge([]);
          Core.showNotification('Workflow imported from JSON', 'success');
        } catch (error) {
          Core.handleError(error, 'JSON Import');
        } finally {
          Core.elements.fileInput.value = '';
        }
      };
      Core.elements.fileInput.click();
    });

    // L1K handlers
    btnExportL1K?.addEventListener('click', () => {
      try {
        const data = IO.serialize();
        IO.downloadText('workflow.l1k', IO.toL1K(data));
        Core.showNotification('Workflow exported to L1K', 'success');
      } catch (error) {
        Core.handleError(error, 'L1K Export');
      }
    });

    btnImportL1K?.addEventListener('click', () => {
      Core.elements.fileInput.onchange = async (ev) => {
        try {
          const file = ev.target.files?.[0];
          if (!file) return;
          
          const text = await file.text();
          const model = IO.fromL1K(text);
          IO.deserialize(model);
          UI.draw();
          updateIssuesBadge([]);
        } catch (error) {
          Core.handleError(error, 'L1K Import');
        } finally {
          Core.elements.fileInput.value = '';
        }
      };
      Core.elements.fileInput.click();
    });

    // Code generation handlers
    btnGenJS?.addEventListener('click', () => {
      try {
        const data = IO.serialize();
        IO.downloadText('workflow.generated.js', IO.generateJS(data));
        Core.showNotification('JavaScript code generated', 'success');
      } catch (error) {
        Core.handleError(error, 'JS Generation');
      }
    });

    btnGenCS?.addEventListener('click', () => {
      try {
        const data = IO.serialize();
        IO.downloadText('Workflow.Generated.cs', IO.generateCS(data));
        Core.showNotification('C# code generated', 'success');
      } catch (error) {
        Core.handleError(error, 'CS Generation');
      }
    });

    // Validation handler
    btnValidate?.addEventListener('click', () => {
      try {
        const issues = IO.validate(IO.serialize());
        updateIssuesBadge(issues);
        highlightIssues(issues);
        
        if (issues.length === 0) {
          Core.showNotification('No issues found', 'success');
        } else {
          Core.showNotification(`${issues.length} issues found`, 'warning');
        }
      } catch (error) {
        Core.handleError(error, 'Validation');
      }
    });

    // Run/Stop handlers
    btnRun?.addEventListener('click', async () => {
      try {
        if (Core.runtime?.isRunning) return;
        btnRun.disabled = true;
        btnStop.disabled = false;
        await Core.startRun();
      } finally {
        btnRun.disabled = false;
        btnStop.disabled = true;
      }
    });

    btnStop?.addEventListener('click', () => {
      Core.stopRun();
      btnStop.disabled = true;
    });
  }

  // Setup context menu
  function setupContextMenu() {
    let ctxMenuPos = {x: 0, y: 0};
    
    Core.elements.canvas?.addEventListener('contextmenu', e => {
      e.preventDefault();
      const rect = Core.elements.canvas.getBoundingClientRect();
      ctxMenuPos = {
        x: e.clientX - rect.left,
        y: e.clientY - rect.top
      };
      
      Core.elements.ctxMenu.style.left = e.clientX + 'px';
      Core.elements.ctxMenu.style.top = e.clientY + 'px';
      Core.elements.ctxMenu.style.display = 'block';
    });
    
    document.addEventListener('click', e => {
      if (Core.elements.ctxMenu && !Core.elements.ctxMenu.contains(e.target)) {
        Core.elements.ctxMenu.style.display = 'none';
      }
    });
    
    Core.elements.ctxMenu?.querySelectorAll('button[data-type]').forEach(btn => {
      btn.addEventListener('click', () => {
        const type = btn.dataset.type;
        UI.addNode(type, ctxMenuPos.x, ctxMenuPos.y);
        Core.elements.ctxMenu.style.display = 'none';
      });
    });
  }

  // Setup example loader
  function setupExampleLoader() {
    Core.elements.btnLoadExample?.addEventListener('click', () => {
      try {
        const example = Core.elements.selExamples.value;
        if (!example) return;
        
        const model = IO.loadExample(example);
        IO.deserialize(model);
        UI.draw();
        updateIssuesBadge([]);
        Core.showNotification(`Example "${example}" loaded`, 'success');
      } catch (error) {
        Core.handleError(error, 'Example Loading');
      }
    });
  }

  // Setup palette (left toolbox) interactions
  function setupPaletteHandlers() {
    try {
      const palette = document.querySelector('.palette');
      if (!palette || !Core.elements.canvas) return;

      palette.querySelectorAll('.palette-item').forEach(btn => {
        btn.addEventListener('click', () => {
          const type = btn.dataset.type || 'Task';
          const rect = Core.elements.canvas.getBoundingClientRect();
          const x = Math.max(20, Math.round(rect.width / 2 - Core.CONSTANTS.NODE_DIMENSIONS.width / 2));
          const y = Math.max(20, Math.round(rect.height / 2 - Core.CONSTANTS.NODE_DIMENSIONS.height / 2));
          const node = UI.addNode(type, x, y);
          if (node) {
            UI.select(node.id);
          }
        });
      });
    } catch (error) {
      Core.handleError(error, 'Palette Setup');
    }
  }

  // Utility functions
  function updateIssuesBadge(issues) {
    if (!issues) return;
    
    if (issues.length === 0) {
      Core.elements.issuesBadge.style.display = 'none';
      Core.elements.issuesBadge.textContent = '0 issues';
    } else {
      Core.elements.issuesBadge.style.display = 'inline';
      Core.elements.issuesBadge.textContent = `${issues.length} issues`;
    }
  }

  function highlightIssues(issues) {
    const invalidNodeIds = new Set(
      issues.filter(i => i.scope === 'node').map(i => i.id)
    );
    
    document.querySelectorAll('.node').forEach(node => {
      node.classList.toggle('invalid', invalidNodeIds.has(node.dataset.id));
    });
  }

  // Initialize everything when DOM is ready
  function initialize() {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initialize);
      return;
    }

    initializeApp();
    setupToolbarHandlers();
    setupContextMenu();
    setupExampleLoader();
    setupPaletteHandlers();

    // Global keyboard shortcuts
    document.addEventListener('keydown', (e) => {
      try {
        if ((e.key === 'Delete' || e.key === 'Backspace') && Core.state.selectedId) {
          e.preventDefault();
          UI.deleteNode(Core.state.selectedId);
        }
      } catch (error) {
        Core.handleError(error, 'Global Keydown');
      }
    });

    // Clear selection when clicking on canvas background
    Core.elements.canvas?.addEventListener('pointerdown', (e) => {
      if (e.target === Core.elements.canvas) {
        Core.state.selectedId = null;
        document.querySelectorAll('.node').forEach(n => n.classList.remove('selected'));
        UI.showProps(null);
      }
    });

    // Escape to clear selection
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        Core.state.selectedId = null;
        document.querySelectorAll('.node').forEach(n => n.classList.remove('selected'));
        UI.showProps(null);
      }
    });
    
    // Setup grid toggle
    Core.elements.chkGrid?.addEventListener('change', UI.renderGrid);
    
    // Initial render
    UI.renderGrid();
  }

  // Start the application
  initialize();
})();

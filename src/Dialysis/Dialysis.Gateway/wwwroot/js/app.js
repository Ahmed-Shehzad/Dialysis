(function () {
  const API_BASE = '/api/v1';
  const TENANT_ID = 'default';

  function getHeaders() {
    return {
      'Content-Type': 'application/json',
      'X-Tenant-Id': TENANT_ID
    };
  }

  async function fetchApi(path) {
    const res = await fetch(API_BASE + path, { headers: getHeaders() });
    if (!res.ok) throw new Error(res.statusText);
    return res.json();
  }

  function render(el, html) {
    el.innerHTML = html;
  }

  function showLoading(el) {
    render(el, '<div class="loading">Loading…</div>');
  }

  function showError(el, msg) {
    render(el, `<div class="error">${msg}</div>`);
  }

  function patientsPage() {
    return `
      <h2 class="page-title">Patients</h2>
      <div id="patients-list"></div>
    `;
  }

  function sessionsPage() {
    return `
      <h2 class="page-title">Sessions</h2>
      <div class="form-group">
        <label>Patient ID</label>
        <input type="text" id="session-patient-id" placeholder="Patient ID">
      </div>
      <button class="btn" onclick="window.app.loadSessions()">Load Sessions</button>
      <div id="sessions-list" style="margin-top: 1rem;"></div>
    `;
  }

  async function loadPatients() {
    const el = document.getElementById('patients-list');
    showLoading(el);
    try {
      const data = await fetchApi('/patients');
      const list = data?.patients ?? (Array.isArray(data) ? data : []);
      if (list.length === 0) {
        render(el, '<p class="card">No patients found. Create patients via the API.</p>');
        return;
      }
      const html = list.map(p => `
        <div class="card">
          <h3>${p.familyName || 'Unknown'}, ${p.givenNames || ''}</h3>
          <p>ID: ${p.logicalId || p.id || ''}</p>
          <a class="btn btn-secondary" href="#" onclick="window.app.navigateToSessions('${p.logicalId || p.id || ''}'); return false;">View Sessions</a>
        </div>
      `).join('');
      render(el, html);
    } catch (e) {
      showError(el, 'Failed to load patients: ' + e.message);
    }
  }

  async function loadSessions() {
    const patientId = document.getElementById('session-patient-id')?.value?.trim();
    if (!patientId) {
      alert('Enter a patient ID');
      return;
    }
    await loadSessionsFor(patientId);
  }

  async function loadSessionsFor(patientId) {
    const el = document.getElementById('sessions-list');
    if (!el) return;
    const container = document.getElementById('content');
    const sessionsContainer = container.querySelector('#sessions-list') || container;
    showLoading(sessionsContainer);
    try {
      const data = await fetchApi('/sessions?patientId=' + encodeURIComponent(patientId));
      const list = Array.isArray(data) ? data : [];
      if (list.length === 0) {
        render(sessionsContainer, '<p class="card">No sessions found for this patient.</p>');
        return;
      }
      const html = `
        <h3 style="margin-bottom: 1rem;">Sessions for patient ${patientId}</h3>
        <table>
          <thead>
            <tr>
              <th>Session ID</th>
              <th>Started</th>
              <th>Ended</th>
              <th>Status</th>
              <th>UF (kg)</th>
            </tr>
          </thead>
          <tbody>
            ${list.map(s => `
              <tr>
                <td>${s.id}</td>
                <td>${s.startedAt ? new Date(s.startedAt).toLocaleString() : '-'}</td>
                <td>${s.endedAt ? new Date(s.endedAt).toLocaleString() : '-'}</td>
                <td><span class="status-badge status-${(s.status || '').toLowerCase().replace(/\s/g,'')}">${s.status || '-'}</span></td>
                <td>${s.ufRemovedKg ?? '-'}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      `;
      render(sessionsContainer, html);
    } catch (e) {
      showError(sessionsContainer, 'Failed to load sessions: ' + e.message);
    }
  }

  function navigate(page) {
    const content = document.getElementById('content');
    if (page === 'patients') {
      render(content, patientsPage());
      loadPatients();
    } else if (page === 'sessions') {
      render(content, sessionsPage());
    }
  }

  function navigateToSessions(patientId) {
    navigate('sessions');
    const input = document.getElementById('session-patient-id');
    if (input) {
      input.value = patientId || '';
      setTimeout(() => loadSessionsFor(patientId), 0);
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-page]').forEach(a => {
      a.addEventListener('click', (e) => {
        e.preventDefault();
        navigate(a.dataset.page);
      });
    });
    navigate('patients');
  });

  window.app = {
    loadPatients,
    loadSessions,
    loadSessionsFor,
    navigate,
    navigateToSessions
  };
})();

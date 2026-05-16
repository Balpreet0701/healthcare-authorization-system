import { useEffect, useMemo, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import {
  Activity,
  Bell,
  ClipboardCheck,
  FileText,
  FolderUp,
  LayoutDashboard,
  Link as LinkIcon,
  LogOut,
  RefreshCw,
  Search,
  ShieldCheck,
  Sparkles,
  Stethoscope,
  Upload,
  UserPlus,
  Users
} from 'lucide-react';
import { api, clearAuth, getApiUrl, getStoredUser, getToken, saveAuth } from './api.js';

const emptyPatient = {
  medicalRecordNumber: '',
  firstName: '',
  lastName: '',
  dateOfBirth: '1985-01-01',
  gender: 'Female',
  phone: '',
  email: '',
  insuranceProvider: '',
  memberNumber: ''
};

const emptyAuthorization = {
  patientId: '',
  requestedService: '',
  diagnosisCode: '',
  procedureCode: '',
  priority: 'Routine',
  clinicalNotes: '',
  dueDate: ''
};

function App() {
  const [user, setUser] = useState(getStoredUser());
  const [view, setView] = useState('dashboard');
  const [toast, setToast] = useState(null);
  const [notifications, setNotifications] = useState([]);

  const isReviewer = user?.roles?.some((role) => role === 'Reviewer' || role === 'Admin');
  const isAdmin = user?.roles?.includes('Admin');

  useEffect(() => {
    if (!user || !getToken()) {
      return;
    }

    api('/api/notifications/unread')
      .then(setNotifications)
      .catch(() => setNotifications([]));

    const connection = new HubConnectionBuilder()
      .withUrl(`${getApiUrl()}/hubs/notifications`, {
        accessTokenFactory: () => getToken()
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('notification', (message) => {
      setNotifications((current) => [message, ...current].slice(0, 20));
      setToast({ tone: 'info', message: message.message || message.title || 'Notification received' });
    });

    connection.start().catch(() => {});

    return () => {
      connection.stop().catch(() => {});
    };
  }, [user]);

  function logout() {
    clearAuth();
    setUser(null);
    setView('dashboard');
  }

  if (!user) {
    return <Login onAuthenticated={setUser} setToast={setToast} toast={toast} />;
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-lockup">
          <div className="brand-mark"><Stethoscope size={21} /></div>
          <div>
            <div className="brand-title">HealthAuth AI</div>
            <div className="brand-subtitle">Prior authorization</div>
          </div>
        </div>

        <nav className="nav-list">
          <NavButton active={view === 'dashboard'} icon={<LayoutDashboard size={18} />} label="Dashboard" onClick={() => setView('dashboard')} />
          <NavButton active={view === 'patients'} icon={<Users size={18} />} label="Patients" onClick={() => setView('patients')} />
          <NavButton active={view === 'authorizations'} icon={<ClipboardCheck size={18} />} label="Authorizations" onClick={() => setView('authorizations')} />
          {isReviewer && <NavButton active={view === 'review'} icon={<ShieldCheck size={18} />} label="Review Queue" onClick={() => setView('review')} />}
          {isReviewer && <NavButton active={view === 'analytics'} icon={<Activity size={18} />} label="Analytics" onClick={() => setView('analytics')} />}
          {isAdmin && <NavButton active={view === 'audit'} icon={<FileText size={18} />} label="Audit" onClick={() => setView('audit')} />}
        </nav>

        <div className="sidebar-footer">
          <div className="user-card">
            <div className="avatar">{user.fullName?.charAt(0) || 'U'}</div>
            <div>
              <div className="fw-semibold">{user.fullName}</div>
              <div className="muted-small">{user.roles?.join(', ')}</div>
            </div>
          </div>
          <button className="btn btn-outline-secondary w-100 btn-sm icon-button-text" onClick={logout}>
            <LogOut size={16} /> Sign out
          </button>
        </div>
      </aside>

      <main className="content">
        <header className="topbar">
          <div>
            <h1>{viewTitles[view]}</h1>
            <p>{viewSubtitles[view]}</p>
          </div>
          <NotificationTray notifications={notifications} />
        </header>

        {toast && <Toast toast={toast} onClose={() => setToast(null)} />}

        {view === 'dashboard' && <Dashboard setView={setView} isReviewer={isReviewer} />}
        {view === 'patients' && <Patients setToast={setToast} />}
        {view === 'authorizations' && <Authorizations setToast={setToast} />}
        {view === 'review' && isReviewer && <ReviewerQueue setToast={setToast} />}
        {view === 'analytics' && isReviewer && <Analytics />}
        {view === 'audit' && isAdmin && <AuditLog />}
      </main>
    </div>
  );
}

const viewTitles = {
  dashboard: 'Operational Dashboard',
  patients: 'Patient Management',
  authorizations: 'Authorization Workflow',
  review: 'Reviewer Dashboard',
  analytics: 'Analytics Dashboard',
  audit: 'Audit Logging'
};

const viewSubtitles = {
  dashboard: 'Clinical, operational, and AI review activity in one workspace.',
  patients: 'Create and maintain member records used in prior authorization requests.',
  authorizations: 'Create requests, upload documents, extract OCR text, and generate AI recommendations.',
  review: 'Review submitted cases with AI summaries and make final authorization decisions.',
  analytics: 'Measure throughput, decision mix, document processing, and turnaround time.',
  audit: 'Trace key actions across the platform for compliance review.'
};

function Login({ onAuthenticated, setToast, toast }) {
  const [email, setEmail] = useState('admin@healthauth.local');
  const [password, setPassword] = useState('Admin@12345');
  const [loading, setLoading] = useState(false);

  async function submit(event) {
    event.preventDefault();
    setLoading(true);
    try {
      const auth = await api('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });
      saveAuth(auth);
      onAuthenticated(auth.user);
    } catch (error) {
      setToast({ tone: 'danger', message: error.message });
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-screen">
      <div className="login-panel">
        <div className="brand-lockup mb-4">
          <div className="brand-mark"><Stethoscope size={24} /></div>
          <div>
            <div className="brand-title">HealthAuth AI</div>
            <div className="brand-subtitle">Enterprise prior authorization</div>
          </div>
        </div>

        {toast && <Toast toast={toast} onClose={() => setToast(null)} />}

        <form onSubmit={submit} className="stack-md">
          <div>
            <label className="form-label">Email</label>
            <input className="form-control" value={email} onChange={(event) => setEmail(event.target.value)} />
          </div>
          <div>
            <label className="form-label">Password</label>
            <input className="form-control" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
          </div>
          <button className="btn btn-primary w-100 icon-button-text" disabled={loading}>
            <ShieldCheck size={17} /> {loading ? 'Signing in' : 'Sign in'}
          </button>
        </form>

        <div className="login-accounts">
          <button type="button" onClick={() => { setEmail('admin@healthauth.local'); setPassword('Admin@12345'); }}>Admin</button>
          <button type="button" onClick={() => { setEmail('reviewer@healthauth.local'); setPassword('Reviewer@12345'); }}>Reviewer</button>
          <button type="button" onClick={() => { setEmail('intake@healthauth.local'); setPassword('Intake@12345'); }}>Intake</button>
        </div>
      </div>
    </div>
  );
}

function Dashboard({ setView, isReviewer }) {
  return (
    <div className="dashboard-grid">
      <ActionPanel icon={<UserPlus />} title="Register Patient" metric="Member intake" onClick={() => setView('patients')} />
      <ActionPanel icon={<FolderUp />} title="Submit Authorization" metric="Documents, OCR, AI" onClick={() => setView('authorizations')} />
      {isReviewer && <ActionPanel icon={<ShieldCheck />} title="Review Queue" metric="Clinical decisions" onClick={() => setView('review')} />}
      {isReviewer && <ActionPanel icon={<Activity />} title="Analytics" metric="Throughput trends" onClick={() => setView('analytics')} />}
    </div>
  );
}

function Patients({ setToast }) {
  const [patients, setPatients] = useState([]);
  const [form, setForm] = useState(emptyPatient);
  const [search, setSearch] = useState('');

  useEffect(() => {
    loadPatients();
  }, []);

  async function loadPatients(term = search) {
    const data = await api(`/api/patients${term ? `?search=${encodeURIComponent(term)}` : ''}`);
    setPatients(data);
  }

  async function savePatient(event) {
    event.preventDefault();
    try {
      await api('/api/patients', { method: 'POST', body: JSON.stringify(form) });
      setForm(emptyPatient);
      await loadPatients('');
      setToast({ tone: 'success', message: 'Patient created.' });
    } catch (error) {
      setToast({ tone: 'danger', message: error.message });
    }
  }

  return (
    <div className="two-column">
      <section className="surface">
        <SectionTitle icon={<UserPlus size={18} />} title="New Patient" />
        <form onSubmit={savePatient} className="form-grid">
          <Input label="MRN" value={form.medicalRecordNumber} onChange={(value) => setForm({ ...form, medicalRecordNumber: value })} />
          <Input label="First name" value={form.firstName} onChange={(value) => setForm({ ...form, firstName: value })} />
          <Input label="Last name" value={form.lastName} onChange={(value) => setForm({ ...form, lastName: value })} />
          <Input label="Date of birth" type="date" value={form.dateOfBirth} onChange={(value) => setForm({ ...form, dateOfBirth: value })} />
          <Select label="Gender" value={form.gender} options={['Female', 'Male', 'Non-binary', 'Other']} onChange={(value) => setForm({ ...form, gender: value })} />
          <Input label="Phone" value={form.phone} onChange={(value) => setForm({ ...form, phone: value })} />
          <Input label="Email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} />
          <Input label="Payer" value={form.insuranceProvider} onChange={(value) => setForm({ ...form, insuranceProvider: value })} />
          <Input label="Member ID" value={form.memberNumber} onChange={(value) => setForm({ ...form, memberNumber: value })} />
          <button className="btn btn-primary icon-button-text grid-span"><UserPlus size={16} /> Create patient</button>
        </form>
      </section>

      <section className="surface">
        <div className="search-row">
          <SectionTitle icon={<Users size={18} />} title="Patients" />
          <div className="input-group compact-search">
            <span className="input-group-text"><Search size={15} /></span>
            <input className="form-control" value={search} onChange={(event) => setSearch(event.target.value)} onKeyDown={(event) => event.key === 'Enter' && loadPatients()} />
          </div>
        </div>
        <div className="table-wrap">
          <table className="table align-middle">
            <thead>
              <tr><th>Patient</th><th>MRN</th><th>Payer</th><th>Member</th></tr>
            </thead>
            <tbody>
              {patients.map((patient) => (
                <tr key={patient.id}>
                  <td>
                    <div className="fw-semibold">{patient.firstName} {patient.lastName}</div>
                    <div className="muted-small">{patient.email}</div>
                  </td>
                  <td>{patient.medicalRecordNumber}</td>
                  <td>{patient.insuranceProvider}</td>
                  <td>{patient.memberNumber}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}

function Authorizations({ setToast }) {
  const [patients, setPatients] = useState([]);
  const [requests, setRequests] = useState([]);
  const [selected, setSelected] = useState(null);
  const [form, setForm] = useState(emptyAuthorization);
  const [urlForm, setUrlForm] = useState({ title: '', url: '', description: '' });
  const [file, setFile] = useState(null);

  useEffect(() => {
    refresh();
  }, []);

  async function refresh() {
    const [patientData, requestData] = await Promise.all([
      api('/api/patients'),
      api('/api/authorizations')
    ]);
    setPatients(patientData);
    setRequests(requestData);
    if (!form.patientId && patientData[0]) {
      setForm((current) => ({ ...current, patientId: patientData[0].id }));
    }
  }

  async function createRequest(event) {
    event.preventDefault();
    try {
      const payload = { ...form, patientId: Number(form.patientId || patients[0]?.id), dueDate: form.dueDate || null };
      const created = await api('/api/authorizations', { method: 'POST', body: JSON.stringify(payload) });
      setSelected(created);
      setForm({ ...emptyAuthorization, patientId: patients[0]?.id || '' });
      await refresh();
      setToast({ tone: 'success', message: 'Authorization request created.' });
    } catch (error) {
      setToast({ tone: 'danger', message: error.message });
    }
  }

  async function openRequest(id) {
    setSelected(await api(`/api/authorizations/${id}`));
  }

  async function submitRequest() {
    await api(`/api/authorizations/${selected.id}/submit`, { method: 'POST' });
    setToast({ tone: 'success', message: 'Submitted for review. AI analysis is queued.' });
    await openRequest(selected.id);
    await refresh();
  }

  async function reanalyze() {
    await api(`/api/authorizations/${selected.id}/reanalyze`, { method: 'POST' });
    setToast({ tone: 'info', message: 'AI analysis queued.' });
  }

  async function uploadDocument(event) {
    event.preventDefault();
    if (!file) return;
    const data = new FormData();
    data.append('file', file);
    await api(`/api/authorizations/${selected.id}/documents`, { method: 'POST', body: data });
    setFile(null);
    setToast({ tone: 'success', message: 'Document uploaded. OCR is queued.' });
    await openRequest(selected.id);
  }

  async function downloadDocument(document) {
    const response = await fetch(`${getApiUrl()}/api/documents/${document.id}/download`, {
      headers: { Authorization: `Bearer ${getToken()}` }
    });

    if (!response.ok) {
      setToast({ tone: 'danger', message: 'Unable to download document.' });
      return;
    }

    const blob = await response.blob();
    const objectUrl = URL.createObjectURL(blob);
    const link = window.document.createElement('a');
    link.href = objectUrl;
    link.download = document.fileName;
    link.click();
    URL.revokeObjectURL(objectUrl);
  }

  async function addUrl(event) {
    event.preventDefault();
    await api(`/api/authorizations/${selected.id}/url-attachments`, { method: 'POST', body: JSON.stringify(urlForm) });
    setUrlForm({ title: '', url: '', description: '' });
    await openRequest(selected.id);
  }

  return (
    <div className="three-pane">
      <section className="surface">
        <SectionTitle icon={<ClipboardCheck size={18} />} title="New Request" />
        <form onSubmit={createRequest} className="stack-md">
          <Select label="Patient" value={form.patientId} options={patients.map((p) => ({ value: p.id, label: `${p.firstName} ${p.lastName} - ${p.medicalRecordNumber}` }))} onChange={(value) => setForm({ ...form, patientId: value })} />
          <Input label="Requested service" value={form.requestedService} onChange={(value) => setForm({ ...form, requestedService: value })} />
          <div className="form-grid two">
            <Input label="Diagnosis" value={form.diagnosisCode} onChange={(value) => setForm({ ...form, diagnosisCode: value })} />
            <Input label="Procedure" value={form.procedureCode} onChange={(value) => setForm({ ...form, procedureCode: value })} />
          </div>
          <div className="form-grid two">
            <Select label="Priority" value={form.priority} options={['Routine', 'Urgent', 'Stat']} onChange={(value) => setForm({ ...form, priority: value })} />
            <Input label="Due date" type="datetime-local" value={form.dueDate} onChange={(value) => setForm({ ...form, dueDate: value })} />
          </div>
          <TextArea label="Clinical notes" value={form.clinicalNotes} onChange={(value) => setForm({ ...form, clinicalNotes: value })} />
          <button className="btn btn-primary icon-button-text"><ClipboardCheck size={16} /> Create request</button>
        </form>
      </section>

      <section className="surface">
        <div className="section-heading">
          <SectionTitle icon={<FileText size={18} />} title="Requests" />
          <button className="icon-only" onClick={refresh} title="Refresh"><RefreshCw size={16} /></button>
        </div>
        <div className="request-list">
          {requests.map((request) => (
            <button key={request.id} className={`request-row ${selected?.id === request.id ? 'active' : ''}`} onClick={() => openRequest(request.id)}>
              <span>
                <strong>{request.requestNumber}</strong>
                <small>{request.patientName}</small>
              </span>
              <StatusBadge status={request.status} />
            </button>
          ))}
        </div>
      </section>

      <section className="surface detail-pane">
        {selected ? (
          <>
            <div className="detail-header">
              <div>
                <div className="muted-small">{selected.requestNumber}</div>
                <h2>{selected.requestedService}</h2>
                <p>{selected.patientName} - {selected.medicalRecordNumber}</p>
              </div>
              <StatusBadge status={selected.status} />
            </div>

            <div className="facts-row">
              <Fact label="Diagnosis" value={selected.diagnosisCode} />
              <Fact label="Procedure" value={selected.procedureCode} />
              <Fact label="Priority" value={selected.priority} />
              <Fact label="AI" value={selected.aiRecommendation || 'Pending'} />
            </div>

            <div className="ai-panel">
              <div className="d-flex align-items-center gap-2 mb-2"><Sparkles size={17} /><strong>AI Medical Summary</strong></div>
              <p>{selected.aiSummary || 'AI analysis will appear after submission or manual reanalysis.'}</p>
              {selected.aiRationale && <p className="muted-small mb-0">{selected.aiRationale}</p>}
            </div>

            <div className="action-row">
              <button className="btn btn-success btn-sm icon-button-text" onClick={submitRequest}><ShieldCheck size={15} /> Submit</button>
              <button className="btn btn-outline-primary btn-sm icon-button-text" onClick={reanalyze}><Sparkles size={15} /> Reanalyze</button>
            </div>

            <div className="subsection">
              <SectionTitle icon={<Upload size={18} />} title="Medical Documents" />
              <form onSubmit={uploadDocument} className="upload-row">
                <input className="form-control" type="file" onChange={(event) => setFile(event.target.files?.[0] || null)} />
                <button className="btn btn-outline-primary icon-button-text"><Upload size={15} /> Upload</button>
              </form>
              {selected.documents.map((document) => (
                <div className="attachment-row" key={document.id}>
                  <div>
                    <strong>{document.fileName}</strong>
                    <small>{document.ocrStatus} - {Math.round(document.fileSize / 1024)} KB</small>
                  </div>
                  <button className="btn btn-link btn-sm" onClick={() => downloadDocument(document)}>Open</button>
                </div>
              ))}
            </div>

            <div className="subsection">
              <SectionTitle icon={<LinkIcon size={18} />} title="URL Attachments" />
              <form onSubmit={addUrl} className="stack-sm">
                <Input label="Title" value={urlForm.title} onChange={(value) => setUrlForm({ ...urlForm, title: value })} />
                <Input label="URL" value={urlForm.url} onChange={(value) => setUrlForm({ ...urlForm, url: value })} />
                <Input label="Description" value={urlForm.description} onChange={(value) => setUrlForm({ ...urlForm, description: value })} />
                <button className="btn btn-outline-secondary btn-sm icon-button-text"><LinkIcon size={15} /> Attach URL</button>
              </form>
              {selected.urlAttachments.map((attachment) => (
                <a className="attachment-row link-row" key={attachment.id} href={attachment.url} target="_blank">
                  <span><strong>{attachment.title}</strong><small>{attachment.description}</small></span>
                </a>
              ))}
            </div>
          </>
        ) : (
          <div className="empty-state">Select a request to view workflow details.</div>
        )}
      </section>
    </div>
  );
}

function ReviewerQueue({ setToast }) {
  const [queue, setQueue] = useState([]);
  const [selected, setSelected] = useState(null);
  const [reason, setReason] = useState('');

  useEffect(() => {
    refresh();
  }, []);

  async function refresh() {
    setQueue(await api('/api/reviewer/queue'));
  }

  async function open(id) {
    setSelected(await api(`/api/authorizations/${id}`));
    setReason('');
  }

  async function decide(decision) {
    await api(`/api/reviewer/${selected.id}/decision`, {
      method: 'POST',
      body: JSON.stringify({ decision, reason: reason || `${decision} after clinical review.` })
    });
    setToast({ tone: 'success', message: `Request moved to ${decision}.` });
    await refresh();
    setSelected(await api(`/api/authorizations/${selected.id}`));
  }

  return (
    <div className="review-layout">
      <section className="surface">
        <SectionTitle icon={<ShieldCheck size={18} />} title="Queue" />
        <div className="request-list">
          {queue.map((request) => (
            <button key={request.id} className={`request-row ${selected?.id === request.id ? 'active' : ''}`} onClick={() => open(request.id)}>
              <span>
                <strong>{request.requestNumber}</strong>
                <small>{request.requestedService}</small>
              </span>
              <StatusBadge status={request.status} />
            </button>
          ))}
        </div>
      </section>
      <section className="surface">
        {selected ? (
          <>
            <div className="detail-header">
              <div>
                <div className="muted-small">{selected.patientName}</div>
                <h2>{selected.requestedService}</h2>
                <p>{selected.diagnosisCode} - {selected.procedureCode}</p>
              </div>
              <StatusBadge status={selected.status} />
            </div>
            <div className="ai-panel">
              <div className="d-flex align-items-center gap-2 mb-2"><Sparkles size={17} /><strong>{selected.aiRecommendation || 'AI Pending'} {selected.aiConfidenceScore ? `(${selected.aiConfidenceScore}%)` : ''}</strong></div>
              <p>{selected.aiSummary || 'AI summary has not been generated yet.'}</p>
              <p className="muted-small">{selected.aiRationale}</p>
            </div>
            <TextArea label="Decision reason" value={reason} onChange={setReason} />
            <div className="action-row mt-3">
              <button className="btn btn-success icon-button-text" onClick={() => decide('Approved')}><ShieldCheck size={16} /> Approve</button>
              <button className="btn btn-warning icon-button-text" onClick={() => decide('PendingInformation')}><FileText size={16} /> Need info</button>
              <button className="btn btn-danger icon-button-text" onClick={() => decide('Denied')}><ShieldCheck size={16} /> Deny</button>
            </div>
          </>
        ) : (
          <div className="empty-state">Select a case to review.</div>
        )}
      </section>
    </div>
  );
}

function Analytics() {
  const [analytics, setAnalytics] = useState(null);

  useEffect(() => {
    api('/api/analytics').then(setAnalytics);
  }, []);

  if (!analytics) {
    return <div className="surface">Loading analytics...</div>;
  }

  const cards = [
    ['Total', analytics.totalRequests],
    ['Pending review', analytics.pendingReview],
    ['Approved', analytics.approved],
    ['Denied', analytics.denied],
    ['OCR complete', analytics.documentsProcessed],
    ['Avg hours', analytics.averageTurnaroundHours]
  ];

  return (
    <div className="analytics-layout">
      <section className="metric-grid">
        {cards.map(([label, value]) => <MetricCard key={label} label={label} value={value} />)}
      </section>
      <section className="surface">
        <SectionTitle icon={<Activity size={18} />} title="Status Mix" />
        <Bars data={analytics.statusCounts} />
      </section>
      <section className="surface">
        <SectionTitle icon={<Activity size={18} />} title="Priority Mix" />
        <Bars data={analytics.priorityCounts} />
      </section>
    </div>
  );
}

function AuditLog() {
  const [logs, setLogs] = useState([]);

  useEffect(() => {
    api('/api/audit').then(setLogs);
  }, []);

  return (
    <section className="surface">
      <SectionTitle icon={<FileText size={18} />} title="Recent Activity" />
      <div className="table-wrap">
        <table className="table align-middle">
          <thead><tr><th>Time</th><th>User</th><th>Action</th><th>Entity</th><th>Details</th></tr></thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id}>
                <td>{new Date(log.createdAt).toLocaleString()}</td>
                <td>{log.userName}</td>
                <td>{log.action}</td>
                <td>{log.entityName} #{log.entityId}</td>
                <td>{log.details}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function NavButton({ active, icon, label, onClick }) {
  return <button className={`nav-button ${active ? 'active' : ''}`} onClick={onClick}>{icon}<span>{label}</span></button>;
}

function SectionTitle({ icon, title }) {
  return <div className="section-title">{icon}<h2>{title}</h2></div>;
}

function Input({ label, value, onChange, type = 'text' }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <input className="form-control" type={type} value={value ?? ''} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function TextArea({ label, value, onChange }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <textarea className="form-control" rows="4" value={value ?? ''} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function Select({ label, value, options, onChange }) {
  const normalized = options.map((option) => typeof option === 'string' ? { value: option, label: option } : option);
  return (
    <label className="form-field">
      <span>{label}</span>
      <select className="form-select" value={value ?? ''} onChange={(event) => onChange(event.target.value)}>
        {normalized.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    </label>
  );
}

function StatusBadge({ status }) {
  const tone = {
    Draft: 'secondary',
    Submitted: 'primary',
    InReview: 'info',
    PendingInformation: 'warning',
    Approved: 'success',
    Denied: 'danger',
    Cancelled: 'dark'
  }[status] || 'secondary';

  return <span className={`badge text-bg-${tone}`}>{status}</span>;
}

function Fact({ label, value }) {
  return <div className="fact"><span>{label}</span><strong>{value || '-'}</strong></div>;
}

function ActionPanel({ icon, title, metric, onClick }) {
  return (
    <button className="action-panel" onClick={onClick}>
      <span className="action-icon">{icon}</span>
      <strong>{title}</strong>
      <small>{metric}</small>
    </button>
  );
}

function MetricCard({ label, value }) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function Bars({ data }) {
  const max = Math.max(...data.map((item) => item.count), 1);
  return (
    <div className="bars">
      {data.map((item) => (
        <div className="bar-row" key={item.status}>
          <span>{item.status}</span>
          <div><i style={{ width: `${(item.count / max) * 100}%` }} /></div>
          <strong>{item.count}</strong>
        </div>
      ))}
    </div>
  );
}

function NotificationTray({ notifications }) {
  return (
    <div className="notification-pill">
      <Bell size={17} />
      <span>{notifications.length}</span>
    </div>
  );
}

function Toast({ toast, onClose }) {
  return (
    <div className={`toast-inline ${toast.tone || 'info'}`}>
      <span>{toast.message}</span>
      <button onClick={onClose}>Dismiss</button>
    </div>
  );
}

export default App;

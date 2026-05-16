const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5088';
const TOKEN_KEY = 'healthauth.token';
const USER_KEY = 'healthauth.user';

const demoUsers = {
  'admin@healthauth.local': {
    password: 'Admin@12345',
    user: {
      id: 'demo-admin',
      email: 'admin@healthauth.local',
      fullName: 'Admin User',
      department: 'Platform Administration',
      roles: ['Admin']
    }
  },
  'reviewer@healthauth.local': {
    password: 'Reviewer@12345',
    user: {
      id: 'demo-reviewer',
      email: 'reviewer@healthauth.local',
      fullName: 'Clinical Reviewer',
      department: 'Utilization Management',
      roles: ['Reviewer']
    }
  },
  'intake@healthauth.local': {
    password: 'Intake@12345',
    user: {
      id: 'demo-intake',
      email: 'intake@healthauth.local',
      fullName: 'Intake Coordinator',
      department: 'Prior Authorization',
      roles: ['Intake']
    }
  }
};

const demoStore = {
  patients: [
    {
      id: 1,
      medicalRecordNumber: 'MRN-100245',
      firstName: 'Avery',
      lastName: 'Johnson',
      dateOfBirth: '1981-04-18',
      gender: 'Female',
      phone: '555-0184',
      email: 'avery.johnson@example.com',
      insuranceProvider: 'Contoso Health',
      memberNumber: 'CH-8842001',
      createdAt: new Date().toISOString(),
      updatedAt: null
    },
    {
      id: 2,
      medicalRecordNumber: 'MRN-100389',
      firstName: 'Marcus',
      lastName: 'Lee',
      dateOfBirth: '1973-11-02',
      gender: 'Male',
      phone: '555-0137',
      email: 'marcus.lee@example.com',
      insuranceProvider: 'Northwind Care',
      memberNumber: 'NW-441802'
    }
  ],
  requests: [
    {
      id: 1,
      requestNumber: 'AUTH-20260516-4821',
      patientId: 1,
      patientName: 'Avery Johnson',
      medicalRecordNumber: 'MRN-100245',
      requestedService: 'MRI lumbar spine without contrast',
      diagnosisCode: 'M54.50',
      procedureCode: '72148',
      priority: 'Urgent',
      status: 'InReview',
      clinicalNotes: 'Persistent lower back pain for 8 weeks with radiculopathy into left leg. Conservative therapy, NSAIDs, and physical therapy completed without sustained relief.',
      createdAt: new Date(Date.now() - 1000 * 60 * 60 * 8).toISOString(),
      submittedAt: new Date(Date.now() - 1000 * 60 * 60 * 6).toISOString(),
      dueDate: new Date(Date.now() + 1000 * 60 * 60 * 48).toISOString(),
      aiSummary: 'Patient has persistent lumbar pain with radicular symptoms after conservative treatment. Requested MRI aligns with medical necessity review criteria when neurologic symptoms persist beyond conservative therapy.',
      aiRecommendation: 'Approve',
      aiConfidenceScore: 82.5,
      aiRationale: 'Clinical history includes duration, failed conservative therapy, and radiculopathy supporting imaging.',
      decisionReason: null,
      documents: [
        {
          id: 1,
          fileName: 'physical-therapy-notes.txt',
          contentType: 'text/plain',
          fileSize: 18420,
          ocrStatus: 'Completed',
          ocrText: 'Physical therapy completed for six weeks with continued pain and limited mobility.',
          ocrError: null,
          uploadedAt: new Date(Date.now() - 1000 * 60 * 60 * 5).toISOString()
        }
      ],
      urlAttachments: [
        {
          id: 1,
          title: 'Payer imaging guideline',
          url: 'https://example.com/guideline',
          description: 'Reference guideline attached by intake.',
          createdAt: new Date(Date.now() - 1000 * 60 * 60 * 4).toISOString()
        }
      ],
      statusHistory: [
        {
          fromStatus: 'Draft',
          toStatus: 'Submitted',
          reason: 'Submitted for review',
          createdAt: new Date(Date.now() - 1000 * 60 * 60 * 6).toISOString()
        }
      ]
    },
    {
      id: 2,
      requestNumber: 'AUTH-20260516-5938',
      patientId: 2,
      patientName: 'Marcus Lee',
      medicalRecordNumber: 'MRN-100389',
      requestedService: 'Home oxygen therapy',
      diagnosisCode: 'J44.9',
      procedureCode: 'E1390',
      priority: 'Routine',
      status: 'Submitted',
      clinicalNotes: 'COPD with exertional dyspnea. Resting saturation and walk test documentation pending upload.',
      createdAt: new Date(Date.now() - 1000 * 60 * 60 * 3).toISOString(),
      submittedAt: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(),
      dueDate: new Date(Date.now() + 1000 * 60 * 60 * 72).toISOString(),
      aiSummary: null,
      aiRecommendation: 'NeedMoreInfo',
      aiConfidenceScore: 64,
      aiRationale: 'Oxygen qualification values are not documented.',
      decisionReason: null,
      documents: [],
      urlAttachments: [],
      statusHistory: []
    }
  ],
  auditLogs: [
    {
      id: 1,
      userName: 'Admin User',
      action: 'Seed',
      entityName: 'AuthorizationRequest',
      entityId: 'sample',
      details: 'Created demo workflow data.',
      ipAddress: 'ui-demo',
      createdAt: new Date().toISOString()
    }
  ]
};

export function getApiUrl() {
  return API_URL;
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser() {
  const value = localStorage.getItem(USER_KEY);
  return value ? JSON.parse(value) : null;
}

export function saveAuth(auth) {
  localStorage.setItem(TOKEN_KEY, auth.token);
  localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
}

export function clearAuth() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export async function api(path, options = {}) {
  const token = getToken();
  const headers = new Headers(options.headers || {});

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const isFormData = options.body instanceof FormData;
  if (options.body && !isFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response;

  try {
    response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers
    });
  } catch {
    return mockApi(path, options);
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get('content-type') || '';
  const data = contentType.includes('application/json') ? await response.json() : await response.text();

  if (!response.ok) {
    const message = typeof data === 'string' ? data : data.message || JSON.stringify(data);
    throw new Error(message || `Request failed with ${response.status}`);
  }

  return data;
}

async function mockApi(path, options = {}) {
  const method = (options.method || 'GET').toUpperCase();
  const body = await parseBody(options.body);
  const cleanPath = path.split('?')[0];

  if (cleanPath === '/api/auth/login' && method === 'POST') {
    const account = demoUsers[String(body.email || '').toLowerCase()];
    if (!account || account.password !== body.password) {
      throw new Error('Invalid demo credentials.');
    }

    return {
      token: 'demo-ui-token',
      expiresAt: new Date(Date.now() + 1000 * 60 * 60 * 12).toISOString(),
      user: account.user
    };
  }

  if (cleanPath === '/api/notifications/unread') {
    return [
      {
        id: 1,
        title: 'UI demo mode',
        message: 'Backend is offline, so sample data is being used.',
        link: '/',
        isRead: false,
        createdAt: new Date().toISOString()
      }
    ];
  }

  if (cleanPath === '/api/patients' && method === 'GET') {
    const query = new URLSearchParams(path.split('?')[1] || '');
    const search = (query.get('search') || '').toLowerCase();
    return demoStore.patients.filter((patient) => {
      if (!search) return true;
      return `${patient.firstName} ${patient.lastName} ${patient.medicalRecordNumber} ${patient.memberNumber}`.toLowerCase().includes(search);
    });
  }

  if (cleanPath === '/api/patients' && method === 'POST') {
    const patient = {
      ...body,
      id: nextId(demoStore.patients),
      createdAt: new Date().toISOString(),
      updatedAt: null
    };
    demoStore.patients.push(patient);
    demoStore.auditLogs.unshift(createAudit('Create', 'Patient', patient.id, `Created patient ${patient.medicalRecordNumber}.`));
    return patient;
  }

  if (cleanPath === '/api/authorizations' && method === 'GET') {
    return demoStore.requests.map(toListItem);
  }

  if (cleanPath === '/api/authorizations' && method === 'POST') {
    const patient = demoStore.patients.find((item) => Number(item.id) === Number(body.patientId));
    if (!patient) {
      throw new Error('Patient does not exist.');
    }

    const request = {
      ...body,
      id: nextId(demoStore.requests),
      requestNumber: `AUTH-${new Date().toISOString().slice(0, 10).replaceAll('-', '')}-${Math.floor(1000 + Math.random() * 8999)}`,
      patientId: Number(body.patientId),
      patientName: `${patient.firstName} ${patient.lastName}`,
      medicalRecordNumber: patient.medicalRecordNumber,
      status: 'Draft',
      createdAt: new Date().toISOString(),
      submittedAt: null,
      dueDate: body.dueDate || null,
      aiSummary: null,
      aiRecommendation: null,
      aiConfidenceScore: null,
      aiRationale: null,
      decisionReason: null,
      documents: [],
      urlAttachments: [],
      statusHistory: []
    };
    demoStore.requests.unshift(request);
    demoStore.auditLogs.unshift(createAudit('Create', 'AuthorizationRequest', request.id, `Created ${request.requestNumber}.`));
    return request;
  }

  const authMatch = cleanPath.match(/^\/api\/authorizations\/(\d+)$/);
  if (authMatch && method === 'GET') {
    return findRequest(authMatch[1]);
  }

  const submitMatch = cleanPath.match(/^\/api\/authorizations\/(\d+)\/submit$/);
  if (submitMatch && method === 'POST') {
    const request = findRequest(submitMatch[1]);
    request.status = 'InReview';
    request.submittedAt = new Date().toISOString();
    request.aiSummary = request.aiSummary || `Demo AI summary for ${request.requestedService}. Clinical documentation is being evaluated for medical necessity and completeness.`;
    request.aiRecommendation = request.aiRecommendation || 'NeedMoreInfo';
    request.aiConfidenceScore = request.aiConfidenceScore || 68;
    request.aiRationale = request.aiRationale || 'Demo recommendation generated without the backend.';
    request.statusHistory.unshift({
      fromStatus: 'Draft',
      toStatus: 'Submitted',
      reason: 'Submitted in UI demo mode',
      createdAt: new Date().toISOString()
    });
    return null;
  }

  const reanalyzeMatch = cleanPath.match(/^\/api\/authorizations\/(\d+)\/reanalyze$/);
  if (reanalyzeMatch && method === 'POST') {
    const request = findRequest(reanalyzeMatch[1]);
    request.aiSummary = `Updated demo AI summary for ${request.requestedService}. The case contains diagnosis ${request.diagnosisCode}, procedure ${request.procedureCode}, and priority ${request.priority}.`;
    request.aiRecommendation = request.documents.length > 0 ? 'Approve' : 'NeedMoreInfo';
    request.aiConfidenceScore = request.documents.length > 0 ? 79 : 61;
    request.aiRationale = request.documents.length > 0
      ? 'Supporting documentation is present in the demo record.'
      : 'Supporting documentation is limited in the demo record.';
    return null;
  }

  const documentMatch = cleanPath.match(/^\/api\/authorizations\/(\d+)\/documents$/);
  if (documentMatch && method === 'POST') {
    const request = findRequest(documentMatch[1]);
    const file = options.body?.get?.('file');
    const document = {
      id: nextDocumentId(),
      fileName: file?.name || 'demo-document.txt',
      contentType: file?.type || 'text/plain',
      fileSize: file?.size || 1200,
      ocrStatus: 'Completed',
      ocrText: 'Demo OCR text extracted from the uploaded document.',
      ocrError: null,
      uploadedAt: new Date().toISOString()
    };
    request.documents.unshift(document);
    return document;
  }

  const urlMatch = cleanPath.match(/^\/api\/authorizations\/(\d+)\/url-attachments$/);
  if (urlMatch && method === 'POST') {
    const request = findRequest(urlMatch[1]);
    const attachment = {
      id: nextId(request.urlAttachments),
      title: body.title,
      url: body.url,
      description: body.description,
      createdAt: new Date().toISOString()
    };
    request.urlAttachments.unshift(attachment);
    return attachment;
  }

  if (cleanPath === '/api/reviewer/queue') {
    return demoStore.requests
      .filter((request) => ['Submitted', 'InReview', 'PendingInformation'].includes(request.status))
      .map(toListItem);
  }

  const decisionMatch = cleanPath.match(/^\/api\/reviewer\/(\d+)\/decision$/);
  if (decisionMatch && method === 'POST') {
    const request = findRequest(decisionMatch[1]);
    request.status = body.decision;
    request.decisionReason = body.reason;
    request.statusHistory.unshift({
      fromStatus: 'InReview',
      toStatus: body.decision,
      reason: body.reason,
      createdAt: new Date().toISOString()
    });
    demoStore.auditLogs.unshift(createAudit('ReviewDecision', 'AuthorizationRequest', request.id, `${body.decision}: ${body.reason}`));
    return null;
  }

  if (cleanPath === '/api/analytics') {
    const totalRequests = demoStore.requests.length;
    return {
      totalRequests,
      pendingReview: demoStore.requests.filter((request) => ['Submitted', 'InReview', 'PendingInformation'].includes(request.status)).length,
      approved: demoStore.requests.filter((request) => request.status === 'Approved').length,
      denied: demoStore.requests.filter((request) => request.status === 'Denied').length,
      documentsProcessed: demoStore.requests.flatMap((request) => request.documents).filter((document) => document.ocrStatus === 'Completed').length,
      averageTurnaroundHours: 18.4,
      statusCounts: countBy(demoStore.requests, 'status'),
      priorityCounts: countBy(demoStore.requests, 'priority')
    };
  }

  if (cleanPath === '/api/audit') {
    return demoStore.auditLogs;
  }

  throw new Error('Backend is offline and no demo response exists for this action.');
}

async function parseBody(body) {
  if (!body) return {};
  if (body instanceof FormData) return {};
  if (typeof body === 'string') return JSON.parse(body);
  return body;
}

function nextId(items) {
  return Math.max(0, ...items.map((item) => Number(item.id) || 0)) + 1;
}

function nextDocumentId() {
  return Math.max(0, ...demoStore.requests.flatMap((request) => request.documents).map((document) => document.id)) + 1;
}

function findRequest(id) {
  const request = demoStore.requests.find((item) => Number(item.id) === Number(id));
  if (!request) {
    throw new Error('Authorization request not found.');
  }
  return request;
}

function toListItem(request) {
  return {
    id: request.id,
    requestNumber: request.requestNumber,
    patientName: request.patientName,
    requestedService: request.requestedService,
    diagnosisCode: request.diagnosisCode,
    procedureCode: request.procedureCode,
    priority: request.priority,
    status: request.status,
    createdAt: request.createdAt,
    dueDate: request.dueDate,
    aiRecommendation: request.aiRecommendation,
    aiConfidenceScore: request.aiConfidenceScore
  };
}

function countBy(items, key) {
  const counts = items.reduce((result, item) => {
    result[item[key]] = (result[item[key]] || 0) + 1;
    return result;
  }, {});

  return Object.entries(counts).map(([status, count]) => ({ status, count }));
}

function createAudit(action, entityName, entityId, details) {
  return {
    id: nextId(demoStore.auditLogs),
    userName: getStoredUser()?.fullName || 'UI Demo',
    action,
    entityName,
    entityId: String(entityId),
    details,
    ipAddress: 'ui-demo',
    createdAt: new Date().toISOString()
  };
}

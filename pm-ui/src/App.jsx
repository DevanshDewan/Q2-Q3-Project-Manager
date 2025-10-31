// src/App.jsx
import React, { useEffect, useState } from "react";
import axios from "axios";

const api = axios.create({ baseURL: "http://localhost:5000/api" });

function extractError(err) {
  if (!err) return "Unknown error";
  if (err.response) {
    const d = err.response.data;
    if (d && typeof d === "object") {
      if (d.message) return `${d.message} (status ${err.response.status})`;
      if (d.title && d.status) return `${d.title} (status ${d.status})`;
      return `${JSON.stringify(d)} (status ${err.response.status})`;
    }
    return `${err.response.status} ${err.response.statusText || ""}`;
  }
  return err.message || String(err);
}

const styles = {
  page: { minHeight: "100vh", background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)", padding: 20, fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif" },
  card: { maxWidth: 1000, margin: "0 auto", background: "#fff", borderRadius: 16, padding: 32, boxShadow: "0 20px 60px rgba(0,0,0,0.3)" },
  headerRow: { display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 28, borderBottom: "2px solid #f0f0f0", paddingBottom: 16 },
  h1: { fontSize: 32, margin: 0, fontWeight: 700, color: "#2d3748" },
  h2: { fontSize: 20, margin: "0 0 16px 0", fontWeight: 600, color: "#2d3748" },
  smallBtn: { padding: "10px 16px", borderRadius: 8, border: "none", cursor: "pointer", fontWeight: 500, transition: "all 0.3s ease" },
  input: { width: "100%", padding: 12, borderRadius: 10, border: "1.5px solid #e2e8f0", marginBottom: 12, boxSizing: "border-box", fontSize: 14, fontFamily: "inherit", transition: "all 0.3s ease" },
  textarea: { width: "100%", padding: 12, borderRadius: 10, border: "1.5px solid #e2e8f0", minHeight: 100, marginBottom: 12, boxSizing: "border-box", fontSize: 14, fontFamily: "inherit", resize: "vertical", transition: "all 0.3s ease" },
  projectBox: { padding: 18, borderRadius: 12, border: "1px solid #e2e8f0", marginBottom: 16, display: "flex", justifyContent: "space-between", alignItems: "flex-start", background: "#f8f9fa", transition: "all 0.3s ease" },
  projectLeft: { flex: 1 },
  projectMeta: { fontSize: 12, color: "#718096", textAlign: "right", minWidth: 120 },
  taskList: { listStyle: "none", paddingLeft: 0, marginTop: 12 },
  taskItem: { display: "flex", alignItems: "center", gap: 12, padding: "10px 0", borderBottom: "1px dashed #e2e8f0" },
  taskTitle: (completed) => ({ flex: 1, color: completed ? "#a0aec0" : "#2d3748", textDecoration: completed ? "line-through" : "none", fontWeight: 500 }),
  circleBtn: { width: 24, height: 24, borderRadius: 12, border: "2px solid #cbd5e0", display: "inline-flex", alignItems: "center", justifyContent: "center", cursor: "default", flexShrink: 0, background: "#fff", transition: "all 0.3s ease" },
  iconBtn: { background: "transparent", border: "none", cursor: "pointer", padding: 8, borderRadius: 8, transition: "all 0.3s ease", display: "flex", alignItems: "center", justifyContent: "center" },
  successBtn: { background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)", color: "#fff", border: "none", padding: "12px 20px", borderRadius: 10, cursor: "pointer", fontWeight: 600, fontSize: 14, transition: "all 0.3s ease", boxShadow: "0 4px 15px rgba(102, 126, 234, 0.4)" },
  muted: { color: "#718096", fontSize: 15 },
  message: { padding: 14, background: "#fffaed", borderRadius: 10, marginBottom: 16, border: "1.5px solid #fbd38d", color: "#7c2d12", fontWeight: 500 },
  authContainer: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: 32, paddingTop: 20 },
  authBox: { background: "#f8f9fa", padding: 24, borderRadius: 12, border: "1px solid #e2e8f0" },
  badge: { fontSize: 12, background: "#667eea", color: "#fff", padding: "4px 8px", borderRadius: 12, fontWeight: 600, display: "inline-block" },
  progressBar: { width: "100%", height: 6, background: "#e2e8f0", borderRadius: 4, overflow: "hidden", marginTop: 4 },
  progressFill: (percentage) => ({ width: `${percentage}%`, height: "100%", background: "linear-gradient(90deg, #667eea, #764ba2)", transition: "width 0.3s ease" }),
};

export default function App() {
  const [token, setToken] = useState(() => localStorage.getItem("pm_token") || "");
  const [registerForm, setRegisterForm] = useState({ email: "", password: "", fullName: "" });
  const [loginForm, setLoginForm] = useState({ email: "", password: "" });
  const [message, setMessage] = useState("");
  const [projects, setProjects] = useState([]);
  const [projectForm, setProjectForm] = useState({ title: "", description: "" });
  const [loading, setLoading] = useState(false);
  const [addingTaskFor, setAddingTaskFor] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("newest");
  const [schedulerResult, setSchedulerResult] = useState(null);

  useEffect(() => {
    const t = localStorage.getItem("pm_token") || token;
    if (t) api.defaults.headers.common["Authorization"] = `Bearer ${t}`;
    else delete api.defaults.headers.common["Authorization"];
  }, []);

  useEffect(() => {
    if (token) {
      api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
      localStorage.setItem("pm_token", token);
      fetchProjects();
    } else {
      delete api.defaults.headers.common["Authorization"];
      localStorage.removeItem("pm_token");
      setProjects([]);
    }
  }, [token]);

  // -------- AUTH --------
  async function register() {
    setMessage("");
    if (!registerForm.email || !registerForm.password || !registerForm.fullName) {
      setMessage("All register fields required");
      return;
    }
    try {
      setLoading(true);
      const res = await api.post("/auth/register", { email: registerForm.email, password: registerForm.password, fullName: registerForm.fullName });
      setMessage("✓ Registered successfully! Now log in.");
      setRegisterForm({ email: "", password: "", fullName: "" });
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ Register failed: " + e);
    } finally {
      setLoading(false);
    }
  }

  async function login() {
    setMessage("");
    if (!loginForm.email || !loginForm.password) {
      setMessage("Email & password required");
      return;
    }
    try {
      setLoading(true);
      const res = await api.post("/auth/login", { email: loginForm.email, password: loginForm.password });
      const tok = res?.data?.token;
      if (!tok) {
        setMessage("Login didn't return token");
        return;
      }
      setToken(tok);
      setLoginForm({ email: "", password: "" });
      setMessage("✓ Login successful!");
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ Login failed: " + e);
    } finally {
      setLoading(false);
    }
  }

  // -------- PROJECTS --------
  async function fetchProjects() {
    setMessage("");
    try {
      setLoading(true);
      const listRes = await api.get("/Projects");
      const list = listRes.data || [];
      const detailPromises = list.map(async (p) => {
        try {
          const r = await api.get(`/Projects/${p.id}`);
          return r.data;
        } catch (err) {
          return { ...p, tasks: [] };
        }
      });
      const full = await Promise.all(detailPromises);
      setProjects(full);
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ Could not fetch projects — " + e);
      setProjects([]);
    } finally {
      setLoading(false);
    }
  }

  async function createProject() {
    const title = (projectForm.title || "").trim();
    const description = projectForm.description || "";
    if (!title) {
      setMessage("Title required");
      return;
    }
    if (title.length < 3) {
      setMessage("Title must be at least 3 characters");
      return;
    }
    try {
      setLoading(true);
      await api.post("/Projects", { title, description });
      setProjectForm({ title: "", description: "" });
      await fetchProjects();
      setMessage("✓ Project created!");
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ " + e);
    } finally {
      setLoading(false);
    }
  }

  async function deleteProject(projectId) {
    if (!confirm("Delete this project?")) return;
    try {
      setLoading(true);
      await api.delete(`/Projects/${projectId}`);
      await fetchProjects();
      setMessage("✓ Project deleted");
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ " + e);
    } finally {
      setLoading(false);
    }
  }

  // -------- TASKS --------
  async function submitAddTask(projectId, title) {
    const trimmed = (title || "").trim();
    if (!trimmed) {
      setMessage("Task title required");
      return;
    }
    try {
      setLoading(true);
      await api.post(`/Projects/${projectId}/Tasks`, { title: trimmed, dueDate: null });
      setAddingTaskFor(null);
      const projectRes = await api.get(`/Projects/${projectId}`);
      setProjects((prev) => prev.map(p => (p.id === projectId ? projectRes.data : p)));
      setMessage("✓ Task added");
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ " + e);
    } finally {
      setLoading(false);
    }
  }

  async function updateTask(projectId, taskId, fields) {
    try {
      setLoading(true);
      await api.put(`/Projects/${projectId}/Tasks/${taskId}`, fields);
      const projectRes = await api.get(`/Projects/${projectId}`);
      setProjects((prev) => prev.map(p => (p.id === projectId ? projectRes.data : p)));
      setMessage("✓ Task updated");
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ " + e);
    } finally {
      setLoading(false);
    }
  }

  async function toggleTask(projectId, task) {
    try {
      setLoading(true);
      await api.put(`/Projects/${projectId}/Tasks/${task.id}`, {
        title: task.title,
        dueDate: task.dueDate,
        isCompleted: !task.isCompleted
      });
      const projectRes = await api.get(`/Projects/${projectId}`);
      setProjects((prev) => prev.map(p => (p.id === projectId ? projectRes.data : p)));
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ " + e);
    } finally {
      setLoading(false);
    }
  }

  async function deleteTask(projectId, taskId) {
    try {
      setLoading(true);
      await api.delete(`/Projects/${projectId}/Tasks/${taskId}`);
      const projectRes = await api.get(`/Projects/${projectId}`);
      setProjects((prev) => prev.map(p => (p.id === projectId ? projectRes.data : p)));
      setMessage("✓ Task deleted");
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ " + e);
    } finally {
      setLoading(false);
    }
  }

  function logout() {
    setToken("");
    setProjects([]);
    setMessage("");
  }

  // -------- SCHEDULER --------
  async function runSchedulerFromProject(project) {
    setSchedulerResult(null);
    setMessage("");
    try {
      setLoading(true);
      const payload = {
        tasks: (project.tasks || []).map(t => ({
          title: t.title,
          estimatedHours: 1,
          dueDate: t.dueDate,
          dependencies: []
        }))
      };
      const res = await api.post(`/Projects/${project.id}/Schedule`, payload);
      setSchedulerResult(res.data);
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ Scheduler error: " + e);
    } finally {
      setLoading(false);
    }
  }

  async function runSchedulerWithCustomPayload(project) {
    const txt = prompt("Paste scheduler JSON (example: { \"tasks\": [{\"title\":\"A\",\"estimatedHours\":1,\"dependencies\":[]}] })");
    if (!txt) return;
    try {
      setLoading(true);
      const payload = JSON.parse(txt);
      const res = await api.post(`/Projects/${project.id}/Schedule`, payload);
      setSchedulerResult(res.data);
    } catch (err) {
      const e = extractError(err);
      setMessage("✗ Scheduler error: " + e);
    } finally {
      setLoading(false);
    }
  }

  // -------- EXPORT --------
  function exportProjects() {
    const data = JSON.stringify(projects, null, 2);
    const blob = new Blob([data], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `projects-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
    setMessage("✓ Projects exported!");
  }

  // -------- ICONS --------
  const IconPencil = ({ size = 16 }) => (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 20h9" /> <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
    </svg>
  );
  const IconCheck = ({ size = 16 }) => (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 6L9 17l-5-5" />
    </svg>
  );
  const IconCross = ({ size = 16 }) => (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 6L6 18" /> <path d="M6 6l12 12" />
    </svg>
  );
  const IconCircle = ({ size = 14 }) => (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
    </svg>
  );

  // -------- SORT & FILTER --------
  const filteredProjects = projects.filter(p => p.title.toLowerCase().includes(searchTerm.toLowerCase()));
  const sortedProjects = [...filteredProjects].sort((a, b) => {
    if (sortBy === "newest") return new Date(b.createdAt || 0) - new Date(a.createdAt || 0);
    if (sortBy === "oldest") return new Date(a.createdAt || 0) - new Date(b.createdAt || 0);
    if (sortBy === "alphabetical") return a.title.localeCompare(b.title);
    return 0;
  });

  return (
    <div style={styles.page}>
      <div style={styles.card}>
        <div style={styles.headerRow}>
          <h1 style={styles.h1}>📋 Project Manager</h1>
          {token ? (
            <div style={{ display: "flex", gap: 12 }}>
              <button style={{ ...styles.smallBtn, background: "#e6f2ff", color: "#2d3748", border: "1px solid #cbd5e0" }} onClick={() => fetchProjects()} disabled={loading}>🔄 Refresh</button>
              <button style={{ ...styles.smallBtn, background: "#f0fdf4", color: "#166534", border: "1px solid #dcfce7" }} onClick={exportProjects}>📥 Export</button>
              <button style={{ ...styles.smallBtn, background: "#fed7d7", color: "#742a2a" }} onClick={logout}>🚪 Logout</button>
            </div>
          ) : null}
        </div>

        {message && <div style={styles.message}>{message}</div>}

        {!token ? (
          <div style={styles.authContainer}>
            <div style={styles.authBox}>
              <h2 style={styles.h2}>📝 Register</h2>
              <input style={styles.input} placeholder="Full name" value={registerForm.fullName} onChange={e => setRegisterForm({ ...registerForm, fullName: e.target.value })} />
              <input style={styles.input} placeholder="Email" type="email" value={registerForm.email} onChange={e => setRegisterForm({ ...registerForm, email: e.target.value })} />
              <input style={styles.input} type="password" placeholder="Password" value={registerForm.password} onChange={e => setRegisterForm({ ...registerForm, password: e.target.value })} />
              <button style={styles.successBtn} onClick={register} disabled={loading}>{loading ? "⏳ Registering..." : "Register"}</button>
            </div>

            <div style={styles.authBox}>
              <h2 style={styles.h2}>🔐 Login</h2>
              <input style={styles.input} placeholder="Email" type="email" value={loginForm.email} onChange={e => setLoginForm({ ...loginForm, email: e.target.value })} />
              <input style={styles.input} type="password" placeholder="Password" value={loginForm.password} onChange={e => setLoginForm({ ...loginForm, password: e.target.value })} />
              <button style={styles.successBtn} onClick={login} disabled={loading}>{loading ? "⏳ Logging in..." : "Login"}</button>
            </div>
          </div>
        ) : (
          <div>
            <section style={{ marginBottom: 24 }}>
              <h2 style={styles.h2}>➕ Create Project</h2>
              <input style={styles.input} placeholder="Project title (min 3 chars)" value={projectForm.title} onChange={e => setProjectForm({ ...projectForm, title: e.target.value })} />
              <textarea style={styles.textarea} placeholder="Project description" value={projectForm.description} onChange={e => setProjectForm({ ...projectForm, description: e.target.value })} />
              <button style={styles.successBtn} onClick={createProject} disabled={loading}>{loading ? "⏳ Creating..." : "Create Project"}</button>
            </section>

            <section>
              <h2 style={styles.h2}>📁 Your Projects</h2>
              
              {projects.length > 0 && (
                <div style={{ marginBottom: 16, display: "flex", gap: 12, alignItems: "center", flexWrap: "wrap" }}>
                  <input 
                    style={{ flex: 1, minWidth: 200, ...styles.input, marginBottom: 0 }} 
                    placeholder="🔍 Search projects..." 
                    value={searchTerm} 
                    onChange={e => setSearchTerm(e.target.value)} 
                  />
                  <select 
                    value={sortBy} 
                    onChange={e => setSortBy(e.target.value)} 
                    style={{ padding: "10px 12px", borderRadius: 8, border: "1.5px solid #e2e8f0", cursor: "pointer", fontWeight: 500, fontSize: 14 }}
                  >
                    <option value="newest">Newest First</option>
                    <option value="oldest">Oldest First</option>
                    <option value="alphabetical">A-Z</option>
                  </select>
                </div>
              )}

              {sortedProjects.length === 0 ? (
                <div style={{ ...styles.muted, textAlign: "center", padding: "40px 20px" }}>
                  {searchTerm ? "No projects found 🔍" : "No projects yet. Create one to get started! 🚀"}
                </div>
              ) : (
                sortedProjects.map(p => {
                  const completed = (p.tasks || []).filter(t => t.isCompleted).length;
                  const total = (p.tasks || []).length;
                  const percentage = total > 0 ? Math.round((completed / total) * 100) : 0;

                  return (
                    <div key={p.id} style={styles.projectBox}>
                      <div style={styles.projectLeft}>
                        <div style={{ fontSize: 18, fontWeight: 700, color: "#2d3748", marginBottom: 6, display: "flex", alignItems: "center", gap: 8 }}>
                          {p.title}
                          <span style={styles.badge}>{total} tasks</span>
                        </div>
                        <div style={{ color: "#718096", marginBottom: 12, fontSize: 14 }}>{p.description}</div>

                        {total > 0 && (
                          <div style={{ marginBottom: 12 }}>
                            <div style={{ fontSize: 12, color: "#718096", marginBottom: 4 }}>Progress: {completed}/{total} ({percentage}%)</div>
                            <div style={styles.progressBar}>
                              <div style={styles.progressFill(percentage)} />
                            </div>
                          </div>
                        )}

                        <ul style={styles.taskList}>
                          {(p.tasks || []).map(t => (
                            <li key={t.id} style={styles.taskItem}>
                              <div style={styles.circleBtn}>
                                <IconCircle />
                              </div>
                              <div style={styles.taskTitle(t.isCompleted)}>{t.title}</div>
                              <div style={{ display: "flex", gap: 8 }}>
                                <button onClick={() => toggleTask(p.id, t)} style={{ ...styles.iconBtn, color: t.isCompleted ? "#22c55e" : "#cbd5e0" }} title="Toggle complete">
                                  <IconCheck size={18} />
                                </button>
                                <button onClick={() => {
                                  const newTitle = prompt("Edit task", t.title);
                                  if (newTitle) updateTask(p.id, t.id, { title: newTitle.trim(), dueDate: t.dueDate, isCompleted: t.isCompleted });
                                }} style={{ ...styles.iconBtn, color: "#3b82f6" }} title="Edit">
                                  <IconPencil size={18} />
                                </button>
                                <button onClick={() => { if (confirm("Delete?")) deleteTask(p.id, t.id); }} style={{ ...styles.iconBtn, color: "#ef4444" }} title="Delete">
                                  <IconCross size={18} />
                                </button>
                              </div>
                            </li>
                          ))}
                          <li style={{ marginTop: 12 }}>
                            {addingTaskFor === p.id ? (
                              <AddTaskInline onSave={(title) => submitAddTask(p.id, title)} onCancel={() => setAddingTaskFor(null)} />
                            ) : (
                              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                                <button style={{ ...styles.successBtn, background: "#3b82f6", boxShadow: "0 4px 15px rgba(59, 130, 246, 0.4)" }} onClick={() => setAddingTaskFor(p.id)}>+ Add Task</button>
                                <button style={{ ...styles.successBtn, background: "#ef4444", boxShadow: "0 4px 15px rgba(239, 68, 68, 0.4)" }} onClick={() => deleteProject(p.id)}>🗑️ Delete</button>
                                <button style={{ ...styles.successBtn, background: "#10b981", boxShadow: "0 4px 15px rgba(16, 185, 129, 0.4)" }} onClick={() => runSchedulerFromProject(p)}>⚙️ Schedule</button>
                                <button style={{ ...styles.successBtn, background: "#f59e0b", boxShadow: "0 4px 15px rgba(245, 158, 11, 0.4)" }} onClick={() => runSchedulerWithCustomPayload(p)}>📊 Custom</button>
                              </div>
                            )}
                          </li>
                        </ul>
                      </div>
                      <div style={styles.projectMeta}>
                        {p.createdAt ? new Date(p.createdAt).toLocaleDateString() : ""}
                      </div>
                    </div>
                  );
                })
              )}
            </section>

            {schedulerResult && (
              <section style={{ marginTop: 20 }}>
                <h2 style={styles.h2}>📈 Scheduler Result</h2>
                <div style={{ background: "#f0f9ff", padding: 16, borderRadius: 12, border: "1px solid #bfdbfe" }}>
                  <strong style={{ color: "#0369a1" }}>Recommended Order:</strong>
                  <ol style={{ marginTop: 8, color: "#0369a1" }}>
                    {(schedulerResult.recommendedOrder || []).map((t, i) => <li key={i}>{t}</li>)}
                  </ol>
                  {schedulerResult.diagnostics && schedulerResult.diagnostics.length > 0 && (
                    <div style={{ marginTop: 12 }}>
                      <strong style={{ color: "#dc2626" }}>Warnings:</strong>
                      <ul style={{ color: "#dc2626", marginTop: 6 }}>
                        {schedulerResult.diagnostics.map((d, i) => <li key={i}>{d}</li>)}
                      </ul>
                    </div>
                  )}
                </div>
              </section>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function AddTaskInline({ onSave, onCancel }) {
  const [title, setTitle] = useState("");
  const [saving, setSaving] = useState(false);

  async function handleSave() {
    if (!title.trim()) { alert("Task required"); return; }
    setSaving(true);
    try {
      await Promise.resolve(onSave(title.trim()));
      setTitle("");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{ display: "flex", gap: 8 }}>
      <input style={{ flex: 1, padding: 10, borderRadius: 8, border: "1.5px solid #e2e8f0", fontSize: 14, fontFamily: "inherit" }} placeholder="New task..." value={title} onChange={e => setTitle(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") handleSave(); }} />
      <button style={{ background: "#10b981", color: "#fff", border: "none", padding: "10px 16px", borderRadius: 8, cursor: "pointer", fontWeight: 600, minWidth: 60 }} onClick={handleSave} disabled={saving}>{saving ? "..." : "Save"}</button>
      <button style={{ background: "#e5e7eb", color: "#2d3748", border: "none", padding: "10px 16px", borderRadius: 8, cursor: "pointer", fontWeight: 600, minWidth: 60 }} onClick={onCancel}>Cancel</button>
    </div>
  );
}

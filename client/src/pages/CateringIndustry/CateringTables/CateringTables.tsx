import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./CateringTables.css";

import {
    archiveCatalogItem,
    createCatalogItem,
    listCatalogItems,
    updateCatalogItem,
    type CatalogItem,
} from "../../../frontapi/catalogApi";
import { getUserFromToken } from "../../../utils/auth";

export default function CateringTables() {
    const businessId = Number(localStorage.getItem("businessId"));
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [tables, setTables] = useState<CatalogItem[]>([]);

    const [showCreate, setShowCreate] = useState(false);
    const [editItem, setEditItem] = useState<CatalogItem | null>(null);

    const [name, setName] = useState("");
    const [code, setCode] = useState("");
    const [turnoverMin, setTurnoverMin] = useState("90");

    const load = async () => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            // Tables are stored as CatalogItems with type=Service (so Reservations can reference catalogItemId).
            const data = await listCatalogItems(businessId, { type: "Service" });
            const list = Array.isArray(data) ? data : [];
            setTables(list);
        } catch (e: any) {
            setTables([]);
            setError(e?.message || "Failed to load tables");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    const active = useMemo(() => tables.filter((t) => String(t.status).toLowerCase() === "active"), [tables]);
    const archived = useMemo(() => tables.filter((t) => String(t.status).toLowerCase() === "archived"), [tables]);

    const resetForm = () => {
        setName("");
        setCode("");
        setTurnoverMin("90");
    };

    const openCreate = () => {
        resetForm();
        setShowCreate(true);
    };

    const openEdit = (t: CatalogItem) => {
        setEditItem(t);
        setName(t.name);
        setCode(t.code);
        setTurnoverMin(String(t.defaultDurationMin ?? 90));
    };

    const saveCreate = async () => {
        if (!canManage) return;
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            if (!name.trim()) throw new Error("Table name is required");
            const d = Number(turnoverMin);
            if (!Number.isFinite(d) || d <= 0) throw new Error("Turnover time must be > 0");

            const c = (code || name).trim().toUpperCase().replace(/\s+/g, "_").slice(0, 24);

            await createCatalogItem(businessId, {
                name: name.trim(),
                code: c,
                type: "Service",
                basePrice: 0,
                taxClass: "STANDARD",
                defaultDurationMin: d,
                status: "Active",
            });

            setShowCreate(false);
            await load();
        } catch (e: any) {
            setError(e?.message || "Create failed");
        }
    };

    const saveEdit = async () => {
        if (!canManage) return;
        if (!editItem) return;
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            if (!name.trim()) throw new Error("Table name is required");
            const d = Number(turnoverMin);
            if (!Number.isFinite(d) || d <= 0) throw new Error("Turnover time must be > 0");

            const c = (code || name).trim().toUpperCase().replace(/\s+/g, "_").slice(0, 24);

            await updateCatalogItem(businessId, editItem.catalogItemId, {
                name: name.trim(),
                code: c,
                defaultDurationMin: d,
            });

            setEditItem(null);
            await load();
        } catch (e: any) {
            setError(e?.message || "Update failed");
        }
    };

    const doArchive = async (id: number) => {
        if (!canManage) return;
        const ok = window.confirm("Archive this table?");
        if (!ok) return;
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            await archiveCatalogItem(businessId, id);
            await load();
        } catch (e: any) {
            setError(e?.message || "Archive failed");
        }
    };

    const doReactivate = async (id: number) => {
        if (!canManage) return;
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            await updateCatalogItem(businessId, id, { status: "Active" });
            await load();
        } catch (e: any) {
            setError(e?.message || "Reactivate failed");
        }
    };

    return (
        <div className="page">
            <div className="action-bar">
                <h2 className="section-title">Tables</h2>
                <div className="action-buttons">
                    {canManage && (
                        <button className="btn btn-primary" onClick={openCreate}>
                            ➕ Create
                        </button>
                    )}
                    <button className="btn" onClick={load} disabled={loading}>
                        {loading ? "Loading…" : "Refresh"}
                    </button>
                </div>
            </div>

            {error && (
                <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                    {error}
                </div>
            )}

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                <div className="card">
                    <h3 style={{ marginTop: 0 }}>Active</h3>
                    {loading ? (
                        <div className="muted">Loading…</div>
                    ) : active.length === 0 ? (
                        <div className="muted">No active tables.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {active.map((t) => (
                                <div key={t.catalogItemId} className="card" style={{ padding: 12 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                        <div>
                                            <strong>{t.name}</strong> <span className="muted">({t.code})</span>
                                            <div className="muted">Turnover: {t.defaultDurationMin ?? 0} min</div>
                                        </div>
                                        {canManage && (
                                            <div style={{ display: "flex", gap: 10 }}>
                                                <button className="btn" onClick={() => openEdit(t)}>
                                                    Edit
                                                </button>
                                                <button className="btn btn-danger" onClick={() => doArchive(t.catalogItemId)}>
                                                    Archive
                                                </button>
                                            </div>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className="card">
                    <h3 style={{ marginTop: 0 }}>Archived</h3>
                    {loading ? (
                        <div className="muted">Loading…</div>
                    ) : archived.length === 0 ? (
                        <div className="muted">No archived tables.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {archived.map((t) => (
                                <div key={t.catalogItemId} className="card" style={{ padding: 12 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                        <div>
                                            <strong>{t.name}</strong> <span className="muted">({t.code})</span>
                                        </div>
                                        {canManage && (
                                            <button className="btn" onClick={() => doReactivate(t.catalogItemId)}>
                                                Reactivate
                                            </button>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>

            {(showCreate || editItem) && (
                <div className="modal-overlay" onClick={() => { setShowCreate(false); setEditItem(null); }}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">{showCreate ? "Create table" : "Edit table"}</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Name</label>
                                <input className="dropdown" value={name} onChange={(e) => setName(e.target.value)} />
                            </div>

                            <div className="modal-field">
                                <label>Code</label>
                                <input className="dropdown" value={code} onChange={(e) => setCode(e.target.value)} placeholder="AUTO from name if empty" />
                            </div>

                            <div className="modal-field">
                                <label>Turnover time (min)</label>
                                <input className="dropdown" type="number" value={turnoverMin} onChange={(e) => setTurnoverMin(e.target.value)} />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => { setShowCreate(false); setEditItem(null); }}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={showCreate ? saveCreate : saveEdit} disabled={!canManage}>
                                Save
                            </button>
                        </div>
                        {!canManage && <div className="muted" style={{ marginTop: 10 }}>Only Owner/Manager can modify tables.</div>}
                    </div>
                </div>
            )}
        </div>
    );
}
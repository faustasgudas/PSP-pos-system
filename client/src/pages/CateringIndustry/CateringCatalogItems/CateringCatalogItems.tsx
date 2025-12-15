import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import { getUserFromToken } from "../../../utils/auth";
import {
    archiveCatalogItem,
    createCatalogItem,
    listCatalogItems,
    updateCatalogItem,
    type CatalogItem,
} from "../../../frontapi/catalogApi";

type Tab = "Product" | "Service";

export default function CateringCatalogItems() {
    const businessId = Number(localStorage.getItem("businessId"));
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const [tab, setTab] = useState<Tab>("Product");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [items, setItems] = useState<CatalogItem[]>([]);

    const [showCreate, setShowCreate] = useState(false);
    const [editItem, setEditItem] = useState<CatalogItem | null>(null);

    const [name, setName] = useState("");
    const [code, setCode] = useState("");
    const [basePrice, setBasePrice] = useState("");
    const [taxClass, setTaxClass] = useState("STANDARD");
    const [durationMin, setDurationMin] = useState("0");

    const load = async () => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const data = await listCatalogItems(businessId, { type: tab });
            setItems(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setItems([]);
            setError(e?.message || "Failed to load catalog items");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [tab, businessId]);

    const active = useMemo(() => items.filter((i) => String(i.status).toLowerCase() === "active"), [items]);
    const archived = useMemo(() => items.filter((i) => String(i.status).toLowerCase() === "archived"), [items]);

    const resetForm = () => {
        setName("");
        setCode("");
        setBasePrice("");
        setTaxClass("STANDARD");
        setDurationMin(tab === "Service" ? "30" : "0");
    };

    const openCreate = () => {
        resetForm();
        setShowCreate(true);
    };

    const openEdit = (i: CatalogItem) => {
        setEditItem(i);
        setName(i.name);
        setCode(i.code);
        setBasePrice(String(i.basePrice));
        setTaxClass(i.taxClass || "STANDARD");
        setDurationMin(String(i.defaultDurationMin ?? (i.type === "Service" ? 30 : 0)));
    };

    const saveCreate = async () => {
        if (!canManage) return;
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            if (!name.trim()) throw new Error("Name is required");
            const p = Number(basePrice);
            if (!Number.isFinite(p) || p < 0) throw new Error("Invalid price");
            const c = (code || name).trim().toUpperCase().replace(/\s+/g, "_").slice(0, 24);
            const d = Number(durationMin);
            if (!Number.isFinite(d) || d < 0) throw new Error("Invalid duration");

            await createCatalogItem(businessId, {
                name: name.trim(),
                code: c,
                type: tab,
                basePrice: p,
                taxClass: taxClass.trim() || "STANDARD",
                defaultDurationMin: tab === "Service" ? d : 0,
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
            if (!name.trim()) throw new Error("Name is required");
            const p = Number(basePrice);
            if (!Number.isFinite(p) || p < 0) throw new Error("Invalid price");
            const c = (code || name).trim().toUpperCase().replace(/\s+/g, "_").slice(0, 24);
            const d = Number(durationMin);
            if (!Number.isFinite(d) || d < 0) throw new Error("Invalid duration");

            await updateCatalogItem(businessId, editItem.catalogItemId, {
                name: name.trim(),
                code: c,
                basePrice: p,
                taxClass: taxClass.trim() || "STANDARD",
                defaultDurationMin: editItem.type === "Service" ? d : 0,
            });
            setEditItem(null);
            await load();
        } catch (e: any) {
            setError(e?.message || "Update failed");
        }
    };

    const doArchive = async (id: number) => {
        if (!canManage) return;
        const ok = window.confirm("Archive this item?");
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
                <h2 className="section-title">Catalog Items</h2>
                <div className="action-buttons">
                    <button className={`btn ${tab === "Product" ? "btn-primary" : ""}`} onClick={() => setTab("Product")}>
                        Products
                    </button>
                    <button className={`btn ${tab === "Service" ? "btn-primary" : ""}`} onClick={() => setTab("Service")}>
                        Services
                    </button>
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
                        <div className="muted">No active items.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {active.map((i) => (
                                <div key={i.catalogItemId} className="card" style={{ padding: 12 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                        <div>
                                            <strong>{i.name}</strong> <span className="muted">({i.code})</span>
                                            <div className="muted">
                                                €{Number(i.basePrice).toFixed(2)} • Tax: {i.taxClass}
                                                {String(i.type) === "Service" ? ` • Duration: ${i.defaultDurationMin ?? 0}m` : ""}
                                            </div>
                                        </div>
                                        {canManage && (
                                            <div style={{ display: "flex", gap: 10 }}>
                                                <button className="btn" onClick={() => openEdit(i)}>
                                                    Edit
                                                </button>
                                                <button className="btn btn-danger" onClick={() => doArchive(i.catalogItemId)}>
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
                        <div className="muted">No archived items.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {archived.map((i) => (
                                <div key={i.catalogItemId} className="card" style={{ padding: 12 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                        <div>
                                            <strong>{i.name}</strong> <span className="muted">({i.code})</span>
                                        </div>
                                        {canManage && (
                                            <button className="btn" onClick={() => doReactivate(i.catalogItemId)}>
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
                <div
                    style={{
                        position: "fixed",
                        inset: 0,
                        background: "rgba(0,0,0,0.35)",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        padding: 16,
                        zIndex: 999,
                    }}
                >
                    <div className="card" style={{ width: "min(720px, 100%)", textAlign: "left" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "center" }}>
                            <div style={{ fontWeight: 800, fontSize: 18 }}>{showCreate ? `Create ${tab}` : "Edit item"}</div>
                            <button className="btn" onClick={() => { setShowCreate(false); setEditItem(null); }}>
                                Close
                            </button>
                        </div>

                        <div style={{ marginTop: 12, display: "grid", gap: 10 }}>
                            <div className="muted">Name</div>
                            <input className="dropdown" value={name} onChange={(e) => setName(e.target.value)} />

                            <div className="muted">Code</div>
                            <input className="dropdown" value={code} onChange={(e) => setCode(e.target.value)} placeholder="AUTO from name if empty" />

                            <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                                <div style={{ flex: 1, minWidth: 220 }}>
                                    <div className="muted">Base price</div>
                                    <input className="dropdown" type="number" value={basePrice} onChange={(e) => setBasePrice(e.target.value)} />
                                </div>
                                <div style={{ flex: 1, minWidth: 220 }}>
                                    <div className="muted">Tax class</div>
                                    <input className="dropdown" value={taxClass} onChange={(e) => setTaxClass(e.target.value)} />
                                </div>
                            </div>

                            {tab === "Service" && (
                                <>
                                    <div className="muted">Default duration (min)</div>
                                    <input className="dropdown" type="number" value={durationMin} onChange={(e) => setDurationMin(e.target.value)} />
                                </>
                            )}
                        </div>

                        <div style={{ marginTop: 12, display: "flex", justifyContent: "flex-end", gap: 10 }}>
                            <button className="btn" onClick={() => { setShowCreate(false); setEditItem(null); }}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={showCreate ? saveCreate : saveEdit} disabled={!canManage}>
                                Save
                            </button>
                        </div>
                        {!canManage && <div className="muted" style={{ marginTop: 10 }}>Only Owner/Manager can modify catalog items.</div>}
                    </div>
                </div>
            )}
        </div>
    );
}



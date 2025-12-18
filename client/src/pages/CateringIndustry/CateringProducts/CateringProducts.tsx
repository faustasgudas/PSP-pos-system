import { useEffect, useMemo, useState } from "react";
import "./CateringProducts.css";

import { getUserFromToken } from "../../../utils/auth";
import {
    archiveCatalogItem,
    createCatalogItem,
    listCatalogItems,
    updateCatalogItem,
    type CatalogItem,
} from "../../../frontapi/catalogApi";

export default function CateringProducts() {
    const businessId = Number(localStorage.getItem("businessId"));
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [items, setItems] = useState<CatalogItem[]>([]);

    const [showCreate, setShowCreate] = useState(false);
    const [editItem, setEditItem] = useState<CatalogItem | null>(null);

    const [name, setName] = useState("");
    const [code, setCode] = useState("");
    const [basePrice, setBasePrice] = useState("");
    const [taxClass, setTaxClass] = useState("STANDARD");

    const load = async () => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const data = await listCatalogItems(businessId, { type: "Product" });
            setItems(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setItems([]);
            setError(e?.message || "Failed to load products");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    const active = useMemo(() => items.filter((i) => String(i.status).toLowerCase() === "active"), [items]);
    const archived = useMemo(() => items.filter((i) => String(i.status).toLowerCase() === "archived"), [items]);

    const resetForm = () => {
        setName("");
        setCode("");
        setBasePrice("");
        setTaxClass("STANDARD");
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

            await createCatalogItem(businessId, {
                name: name.trim(),
                code: c,
                type: "Product",
                basePrice: p,
                taxClass: taxClass.trim() || "STANDARD",
                defaultDurationMin: 0,
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

            await updateCatalogItem(businessId, editItem.catalogItemId, {
                name: name.trim(),
                code: c,
                basePrice: p,
                taxClass: taxClass.trim() || "STANDARD",
            });
            setEditItem(null);
            await load();
        } catch (e: any) {
            setError(e?.message || "Update failed");
        }
    };

    const doArchive = async (id: number) => {
        if (!canManage) return;
        const ok = window.confirm("Archive this product?");
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
                <h2 className="section-title">Products</h2>
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
                        <div className="muted">No active products.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {active.map((i) => (
                                <div key={i.catalogItemId} className="card" style={{ padding: 12 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                        <div>
                                            <strong>{i.name}</strong> <span className="muted">({i.code})</span>
                                            <div className="muted">€{Number(i.basePrice).toFixed(2)} • Tax: {i.taxClass}</div>
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
                        <div className="muted">No archived products.</div>
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
                <div className="modal-overlay" onClick={() => { setShowCreate(false); setEditItem(null); }}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">{showCreate ? "Create product" : "Edit product"}</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Name</label>
                                <input
                                    className="dropdown"
                                    placeholder="e.g. Coca-Cola 0.33L"
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Code</label>
                                <input className="dropdown" value={code} onChange={(e) => setCode(e.target.value)} placeholder="AUTO from name if empty" />
                            </div>

                            <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                                <div style={{ flex: 1, minWidth: 220 }} className="modal-field">
                                    <label>Base price</label>
                                    <input
                                        className="dropdown"
                                        type="number"
                                        inputMode="decimal"
                                        placeholder="e.g. 2.50"
                                        value={basePrice}
                                        onChange={(e) => setBasePrice(e.target.value)}
                                    />
                                </div>
                                <div style={{ flex: 1, minWidth: 220 }} className="modal-field">
                                    <label>Tax class</label>
                                    <input
                                        className="dropdown"
                                        placeholder='e.g. "STANDARD"'
                                        value={taxClass}
                                        onChange={(e) => setTaxClass(e.target.value)}
                                    />
                                </div>
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
                        {!canManage && <div className="muted" style={{ marginTop: 10 }}>Only Owner/Manager can modify products.</div>}
                    </div>
                </div>
            )}
        </div>
    );
}
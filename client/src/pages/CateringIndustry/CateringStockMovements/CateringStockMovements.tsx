import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./CateringStockMovements.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import { createStockMovement, listStockItems, listStockMovements, type StockItemSummary, type StockMovement } from "../../../frontapi/stockApi";

function formatDelta(n: number) {
    const v = Number(n) || 0;
    return v > 0 ? `+${v}` : String(v);
}

export default function CateringStockMovements() {
    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [stockItems, setStockItems] = useState<StockItemSummary[]>([]);
    const [catalog, setCatalog] = useState<CatalogItem[]>([]);
    const [selectedStockItemId, setSelectedStockItemId] = useState<string>("");
    const [movements, setMovements] = useState<StockMovement[]>([]);

    const [newType, setNewType] = useState("Manual");
    const [newDelta, setNewDelta] = useState("");
    const [newUnitCost, setNewUnitCost] = useState("");
    const [newNote, setNewNote] = useState("");
    const [creating, setCreating] = useState(false);

    const catalogById = useMemo(() => {
        const m = new Map<number, CatalogItem>();
        (catalog || []).forEach((c) => m.set(c.catalogItemId, c));
        return m;
    }, [catalog]);

    const loadBase = async () => {
        if (!businessId) {
            setError("Missing businessId");
            setStockItems([]);
            setCatalog([]);
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);
        try {
            const [si, cat] = await Promise.all([
                listStockItems(businessId),
                listCatalogItems(businessId, { status: "Active" }),
            ]);
            setStockItems(Array.isArray(si) ? si : []);
            setCatalog(Array.isArray(cat) ? cat : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load stock items");
            setStockItems([]);
            setCatalog([]);
        } finally {
            setLoading(false);
        }
    };

    const loadMovements = async (stockItemId: number) => {
        if (!businessId) return;
        setError(null);
        try {
            const data = await listStockMovements(businessId, stockItemId);
            setMovements(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load movements");
            setMovements([]);
        }
    };

    useEffect(() => {
        loadBase();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    useEffect(() => {
        const id = Number(selectedStockItemId);
        if (!id) {
            setMovements([]);
            return;
        }
        loadMovements(id);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedStockItemId]);

    const stockItemOptions = useMemo(() => {
        return (stockItems || []).map((s) => {
            const item = catalogById.get(s.catalogItemId);
            const label = item ? `${item.name} (${s.unit})` : `StockItem #${s.stockItemId}`;
            const sub = item ? `${item.type} • ${item.code}` : `CatalogItem #${s.catalogItemId}`;
            return { value: String(s.stockItemId), label, subLabel: sub };
        });
    }, [stockItems, catalogById]);

    const doCreate = async () => {
        const stockItemId = Number(selectedStockItemId);
        if (!businessId) return setError("Missing businessId");
        if (!stockItemId) return setError("Select a stock item");

        const delta = Number(newDelta);
        if (!Number.isFinite(delta) || delta === 0) return setError("Delta must be a non-zero number");

        const unitCost = newUnitCost.trim() ? Number(newUnitCost) : null;
        if (unitCost !== null && (!Number.isFinite(unitCost) || unitCost < 0)) return setError("Invalid unit cost");

        setCreating(true);
        setError(null);
        try {
            await createStockMovement(businessId, stockItemId, {
                type: newType.trim() || "Manual",
                delta,
                unitCostSnapshot: unitCost,
                note: newNote.trim() || null,
            });
            setNewDelta("");
            setNewUnitCost("");
            setNewNote("");
            await loadMovements(stockItemId);
        } catch (e: any) {
            setError(e?.message || "Failed to create movement");
        } finally {
            setCreating(false);
        }
    };

    return (
        <div className="page">
            <div className="action-bar">
                <h2 className="section-title">Stock Movements</h2>
                <div className="action-buttons">
                    <button className="btn" onClick={loadBase} disabled={loading}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                </div>
            </div>

            {error && (
                <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                    {error}
                </div>
            )}

            <div className="card" style={{ marginBottom: 12 }}>
                <div className="muted" style={{ marginBottom: 8 }}>
                    Select stock item
                </div>
                <BeautySelect
                    value={selectedStockItemId}
                    onChange={setSelectedStockItemId}
                    placeholder={loading ? "Loading…" : "Choose item"}
                    options={[{ value: "", label: "— Select —" }, ...stockItemOptions]}
                />
            </div>

            <div className="card" style={{ marginBottom: 12 }}>
                <h3 style={{ marginTop: 0 }}>New movement</h3>
                <div className="sm-form">
                    <div>
                        <div className="muted" style={{ marginBottom: 6 }}>Type</div>
                        <input className="dropdown sm-input" value={newType} onChange={(e) => setNewType(e.target.value)} placeholder="Manual" />
                    </div>
                    <div>
                        <div className="muted" style={{ marginBottom: 6 }}>Delta</div>
                        <input className="dropdown sm-input" value={newDelta} onChange={(e) => setNewDelta(e.target.value)} placeholder="e.g. 5 or -3" inputMode="decimal" />
                    </div>
                    <div>
                        <div className="muted" style={{ marginBottom: 6 }}>Unit cost (optional)</div>
                        <input className="dropdown sm-input" value={newUnitCost} onChange={(e) => setNewUnitCost(e.target.value)} placeholder="e.g. 1.25" inputMode="decimal" />
                    </div>
                </div>
                <div style={{ marginTop: 10 }}>
                    <div className="muted" style={{ marginBottom: 6 }}>Note (optional)</div>
                    <input className="dropdown sm-input" value={newNote} onChange={(e) => setNewNote(e.target.value)} placeholder="Reason / reference…" />
                </div>
                <div style={{ marginTop: 12 }}>
                    <button className="btn btn-primary" onClick={doCreate} disabled={creating || !selectedStockItemId}>
                        {creating ? "Creating…" : "Add movement"}
                    </button>
                </div>
            </div>

            <div className="cat-table-wrap">
                <table className="cat-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Type</th>
                            <th className="right">Delta</th>
                            <th className="right">Unit cost</th>
                            <th>At</th>
                            <th>Note</th>
                        </tr>
                    </thead>
                    <tbody>
                        {!selectedStockItemId ? (
                            <tr>
                                <td colSpan={6}><span className="muted">Select a stock item to see movements</span></td>
                            </tr>
                        ) : movements.length === 0 ? (
                            <tr>
                                <td colSpan={6}><span className="muted">No movements</span></td>
                            </tr>
                        ) : (
                            movements
                                .slice()
                                .sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime())
                                .map((m) => (
                                    <tr key={m.stockMovementId} className="cat-row">
                                        <td className="mono">{m.stockMovementId}</td>
                                        <td className="muted">{m.type}</td>
                                        <td className="right" style={{ fontWeight: 700 }}>{formatDelta(m.delta)}</td>
                                        <td className="right">{m.unitCostSnapshot == null ? "—" : Number(m.unitCostSnapshot).toFixed(2)}</td>
                                        <td className="muted">{new Date(m.at).toLocaleString()}</td>
                                        <td className="muted">{m.note || "—"}</td>
                                    </tr>
                                ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}



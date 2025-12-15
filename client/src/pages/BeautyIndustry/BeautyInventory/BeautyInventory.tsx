import { useEffect, useMemo, useState } from "react";
import "./BeautyInventory.css";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import {
    createStockItem,
    createStockMovement,
    listStockItems,
    type StockItemSummary,
} from "../../../frontapi/stockApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

export default function BeautyInventory() {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [stockItems, setStockItems] = useState<StockItemSummary[]>([]);
    const [products, setProducts] = useState<CatalogItem[]>([]);

    const [showModal, setShowModal] = useState(false);
    const [saving, setSaving] = useState(false);

    const [catalogItemId, setCatalogItemId] = useState<string>("");
    const [unit, setUnit] = useState<string>("pcs");
    const [initialQty, setInitialQty] = useState<string>("0");
    const [initialCost, setInitialCost] = useState<string>("0");

    const [movementForStockItemId, setMovementForStockItemId] = useState<number | null>(null);
    const [movementType, setMovementType] = useState<"Receive" | "Adjust">("Receive");
    const [movementDelta, setMovementDelta] = useState<string>("");
    const [movementUnitCost, setMovementUnitCost] = useState<string>("");
    const [movementNote, setMovementNote] = useState<string>("");

    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const productNameById = useMemo(() => {
        const m = new Map<number, string>();
        products.forEach((p) => m.set(p.catalogItemId, p.name));
        return m;
    }, [products]);

    const load = async () => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);
        try {
            const [items, prods] = await Promise.all([
                listStockItems(businessId),
                listCatalogItems(businessId, { type: "Product" }),
            ]);
            setStockItems(Array.isArray(items) ? items : []);
            setProducts(Array.isArray(prods) ? prods : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load inventory");
            setStockItems([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    const saveItem = async () => {
        if (saving) return;
        setError(null);
        try {
            if (!businessId) throw new Error("Missing businessId");
            const cid = Number(catalogItemId);
            if (!cid) throw new Error("Select a product");
            if (!unit.trim()) throw new Error("Unit is required");

            const qty = Number(initialQty);
            const cost = Number(initialCost);
            if (!Number.isFinite(qty) || qty < 0) throw new Error("Invalid initial qty");
            if (!Number.isFinite(cost) || cost < 0) throw new Error("Invalid unit cost");

            setSaving(true);
            await createStockItem(businessId, {
                catalogItemId: cid,
                unit: unit.trim(),
                initialQtyOnHand: qty,
                initialAverageUnitCost: cost,
            });

            setShowModal(false);
            setCatalogItemId("");
            setUnit("pcs");
            setInitialQty("0");
            setInitialCost("0");
            await load();
        } catch (e: any) {
            setError(e?.message || "Failed to create stock item");
        } finally {
            setSaving(false);
        }
    };

    const openMovement = (stockItemId: number, type: "Receive" | "Adjust") => {
        setMovementForStockItemId(stockItemId);
        setMovementType(type);
        setMovementDelta("");
        setMovementUnitCost("");
        setMovementNote("");
    };

    const saveMovement = async () => {
        if (saving) return;
        setError(null);
        try {
            if (!businessId) throw new Error("Missing businessId");
            if (!movementForStockItemId) throw new Error("Missing stock item");

            const delta = Number(movementDelta);
            if (!Number.isFinite(delta) || delta === 0) throw new Error("Delta cannot be 0");

            let unitCostSnapshot: number | null = null;
            if (movementType === "Receive") {
                if (delta <= 0) throw new Error("Receive delta must be positive");
                const c = Number(movementUnitCost);
                if (!Number.isFinite(c) || c <= 0) throw new Error("Unit cost is required for Receive");
                unitCostSnapshot = c;
            } else {
                // Adjust can be +/-; unitCostSnapshot optional
                if (movementUnitCost.trim()) {
                    const c = Number(movementUnitCost);
                    if (!Number.isFinite(c) || c < 0) throw new Error("Invalid unit cost");
                    unitCostSnapshot = c;
                }
            }

            setSaving(true);
            await createStockMovement(businessId, movementForStockItemId, {
                type: movementType,
                delta,
                unitCostSnapshot,
                note: movementNote.trim() || null,
            });

            setMovementForStockItemId(null);
            await load();
        } catch (e: any) {
            setError(e?.message || "Failed to create stock movement");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="inventory-container">
            <div className="action-bar">
                <h2 className="section-title">Inventory</h2>
                <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                    <button className="btn" onClick={load} disabled={loading}>
                        {loading ? "Loading…" : "Refresh"}
                    </button>
                    {canManage && (
                        <button className="btn btn-primary" onClick={() => setShowModal(true)}>
                            ➕ Add Item
                        </button>
                    )}
                </div>
            </div>

            {error && (
                <div className="no-inventory" style={{ color: "#b01d1d" }}>
                    {error}
                    {authProblem && (
                        <div style={{ marginTop: 10 }}>
                            <button
                                className="btn"
                                onClick={() => {
                                    logout();
                                    window.location.reload();
                                }}
                            >
                                Log out
                            </button>
                        </div>
                    )}
                </div>
            )}

            <div className="inventory-list">
                {loading ? (
                    <div className="no-inventory">Loading inventory…</div>
                ) : stockItems.length > 0 ? (
                    stockItems.map(item => (
                        <div key={item.stockItemId} className="inventory-card">
                            <div>
                                <div className="inventory-name">
                                    {productNameById.get(item.catalogItemId) ?? `Product ${item.catalogItemId}`}
                                </div>
                                <div className="inventory-qty">
                                    {item.qtyOnHand} {item.unit}
                                </div>
                            </div>

                            {canManage && (
                                <div style={{ display: "flex", gap: 10 }}>
                                    <button className="btn" onClick={() => openMovement(item.stockItemId, "Receive")}>
                                        Receive
                                    </button>
                                    <button className="btn" onClick={() => openMovement(item.stockItemId, "Adjust")}>
                                        Adjust
                                    </button>
                                </div>
                            )}
                        </div>
                    ))
                ) : (
                    <div className="no-inventory">No inventory items found</div>
                )}
            </div>

            {/* ✅ ADD ITEM MODAL */}
            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">Add Inventory Item</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Product</label>
                                <select
                                    value={catalogItemId}
                                    onChange={(e) => setCatalogItemId(e.target.value)}
                                    disabled={saving}
                                >
                                    <option value="">Select product</option>
                                    {products
                                        .filter((p) => p.status === "Active")
                                        .map((p) => (
                                            <option key={p.catalogItemId} value={p.catalogItemId}>
                                                {p.name}
                                            </option>
                                        ))}
                                </select>
                            </div>

                            <div className="modal-field">
                                <label>Initial Quantity</label>
                                <input
                                    type="number"
                                    value={initialQty}
                                    onChange={(e) => setInitialQty(e.target.value)}
                                    disabled={saving}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Unit</label>
                                <input
                                    type="text"
                                    value={unit}
                                    onChange={(e) => setUnit(e.target.value)}
                                    disabled={saving}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Initial Average Unit Cost</label>
                                <input
                                    type="number"
                                    value={initialCost}
                                    onChange={(e) => setInitialCost(e.target.value)}
                                    disabled={saving}
                                />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowModal(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={saveItem} disabled={saving}>
                                {saving ? "Saving…" : "Save Item"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* MOVEMENT MODAL */}
            {movementForStockItemId && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">
                            {movementType === "Receive" ? "Receive stock" : "Adjust stock"}
                        </h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Delta ({movementType === "Receive" ? "+in" : "+/-"})</label>
                                <input
                                    type="number"
                                    value={movementDelta}
                                    onChange={(e) => setMovementDelta(e.target.value)}
                                    disabled={saving}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Unit cost {movementType === "Receive" ? "(required)" : "(optional)"}</label>
                                <input
                                    type="number"
                                    value={movementUnitCost}
                                    onChange={(e) => setMovementUnitCost(e.target.value)}
                                    disabled={saving}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Note</label>
                                <input
                                    type="text"
                                    value={movementNote}
                                    onChange={(e) => setMovementNote(e.target.value)}
                                    disabled={saving}
                                />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setMovementForStockItemId(null)}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={saveMovement} disabled={saving}>
                                {saving ? "Saving…" : "Save"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

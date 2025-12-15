import { useEffect, useMemo, useState } from "react";
import "./BeautyInventory.css";
import {
    createCatalogItem,
    listCatalogItems,
    updateCatalogItem,
    type CatalogItem,
} from "../../../frontapi/catalogApi";
import {
    createStockItem,
    createStockMovement,
    listStockItems,
    updateStockItemUnit,
    type StockItemSummary,
} from "../../../frontapi/stockApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

function formatMoney(amount: number, currency: string = "EUR") {
    const n = Number(amount) || 0;
    try {
        return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(n);
    } catch {
        return `${n.toFixed(2)} ${currency}`;
    }
}

const UNITS = ["pcs", "ml", "g"] as const;

function makeCodeFromName(name: string) {
    return name
        .trim()
        .toUpperCase()
        .replace(/[^A-Z0-9]+/g, "_")
        .replace(/^_+|_+$/g, "")
        .slice(0, 24);
}

export default function BeautyInventory() {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [stockItems, setStockItems] = useState<StockItemSummary[]>([]);
    const [products, setProducts] = useState<CatalogItem[]>([]);

    const [saving, setSaving] = useState(false);
    const [query, setQuery] = useState("");

    // ✅ POPUP Manage (vietoj expand)
    const [selected, setSelected] = useState<CatalogItem | null>(null);

    // CREATE
    const [showCreate, setShowCreate] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createPrice, setCreatePrice] = useState("0");
    const [createTaxClass, setCreateTaxClass] = useState("STANDARD");
    const [createTrackStock, setCreateTrackStock] = useState(true);
    const [createUnit, setCreateUnit] = useState<(typeof UNITS)[number]>("pcs");
    const [createInitialQty, setCreateInitialQty] = useState("0");

    // EDIT (modal)
    const [editProductId, setEditProductId] = useState<number | null>(null);
    const [editName, setEditName] = useState("");
    const [editPrice, setEditPrice] = useState("");
    const [editTaxClass, setEditTaxClass] = useState("STANDARD");
    const [editUnit, setEditUnit] = useState<(typeof UNITS)[number]>("pcs");

    // enable stock if not tracked
    const [editEnableUnit, setEditEnableUnit] = useState<(typeof UNITS)[number]>("pcs");
    const [editEnableQty, setEditEnableQty] = useState("0");

    // stock actions
    const [editSetToQty, setEditSetToQty] = useState("");
    const [editSetUnitCost, setEditSetUnitCost] = useState("");

    const [editReceiveQty, setEditReceiveQty] = useState("");
    const [editReceiveUnitCost, setEditReceiveUnitCost] = useState("");

    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const stockByCatalogId = useMemo(() => {
        const m = new Map<number, StockItemSummary>();
        stockItems.forEach((s) => m.set(s.catalogItemId, s));
        return m;
    }, [stockItems]);

    const activeProducts = useMemo(
        () => products.filter((p) => p.status === "Active"),
        [products]
    );

    const rows = useMemo(() => {
        const q = query.trim().toLowerCase();
        return activeProducts
            .map((p) => ({ product: p, stock: stockByCatalogId.get(p.catalogItemId) }))
            .filter(({ product }) => {
                if (!q) return true;
                return (
                    String(product.name).toLowerCase().includes(q) ||
                    String(product.code).toLowerCase().includes(q)
                );
            })
            .sort((a, b) => String(a.product.name).localeCompare(String(b.product.name)));
    }, [activeProducts, query, stockByCatalogId]);

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
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    const openCreate = () => {
        setCreateName("");
        setCreatePrice("0");
        setCreateTaxClass("STANDARD");
        setCreateTrackStock(true);
        setCreateUnit("pcs");
        setCreateInitialQty("0");
        setError(null);
        setShowCreate(true);
    };

    const doCreate = async () => {
        if (saving) return;
        setError(null);

        try {
            if (!businessId) throw new Error("Missing businessId");
            if (!createName.trim()) throw new Error("Product name is required");

            const p = Number(createPrice);
            if (!Number.isFinite(p) || p < 0) throw new Error("Invalid price");

            const code = makeCodeFromName(createName);
            if (!code) throw new Error("Invalid name (cannot generate code)");

            let qty = Number(createInitialQty);
            if (createTrackStock) {
                if (!createUnit.trim()) throw new Error("Unit is required");
                if (!Number.isFinite(qty) || qty < 0) throw new Error("Invalid initial qty");
            }

            setSaving(true);

            const created = await createCatalogItem(businessId, {
                name: createName.trim(),
                code,
                type: "Product",
                basePrice: p,
                taxClass: createTaxClass.trim() || "STANDARD",
                defaultDurationMin: 0,
                status: "Active",
            });

            if (createTrackStock) {
                await createStockItem(businessId, {
                    catalogItemId: created.catalogItemId,
                    unit: createUnit,
                    initialQtyOnHand: qty,
                    initialAverageUnitCost: 0,
                });
            }

            setShowCreate(false);
            await load();
        } catch (e: any) {
            setError(e?.message || "Failed to create item");
        } finally {
            setSaving(false);
        }
    };

    const openManage = (p: CatalogItem) => {
        const s = stockByCatalogId.get(p.catalogItemId);

        setSelected(p);
        setEditProductId(p.catalogItemId);
        setEditName(p.name);
        setEditPrice(String(p.basePrice ?? 0));
        setEditTaxClass(p.taxClass || "STANDARD");
        setEditUnit(((s?.unit as any) ?? "pcs") as any);

        setEditEnableUnit("pcs");
        setEditEnableQty("0");

        setEditSetToQty(s ? String(s.qtyOnHand ?? 0) : "");
        setEditSetUnitCost("");

        setEditReceiveQty("");
        setEditReceiveUnitCost("");

        setError(null);
    };

    const closeManage = () => {
        setSelected(null);
        setEditProductId(null);
    };

    const doSaveProduct = async () => {
        if (saving) return;
        setError(null);

        try {
            if (!businessId) throw new Error("Missing businessId");
            if (!editProductId) throw new Error("Missing product");
            if (!editName.trim()) throw new Error("Name is required");

            const price = Number(editPrice);
            if (!Number.isFinite(price) || price < 0) throw new Error("Invalid price");

            const code = makeCodeFromName(editName);
            if (!code) throw new Error("Invalid name (cannot generate code)");

            setSaving(true);

            await updateCatalogItem(businessId, editProductId, {
                name: editName.trim(),
                code,
                basePrice: price,
                taxClass: editTaxClass.trim() || "STANDARD",
            });

            await load();
            // atnaujinam selected reference (optional)
            const fresh = products.find((x) => x.catalogItemId === editProductId) ?? null;
            if (fresh) setSelected(fresh);
        } catch (e: any) {
            setError(e?.message || "Failed to save product");
        } finally {
            setSaving(false);
        }
    };

    const doEnableStock = async () => {
        if (saving) return;
        setError(null);

        try {
            if (!businessId) throw new Error("Missing businessId");
            if (!editProductId) throw new Error("Missing product");
            if (!editEnableUnit.trim()) throw new Error("Unit is required");

            const qty = Number(editEnableQty);
            if (!Number.isFinite(qty) || qty < 0) throw new Error("Invalid initial qty");

            setSaving(true);

            await createStockItem(businessId, {
                catalogItemId: editProductId,
                unit: editEnableUnit,
                initialQtyOnHand: qty,
                initialAverageUnitCost: 0,
            });

            await load();
        } catch (e: any) {
            setError(e?.message || "Failed to enable stock tracking");
        } finally {
            setSaving(false);
        }
    };

    const doUpdateUnitIfNeeded = async (stock: StockItemSummary) => {
        const nextUnit = editUnit.trim();
        if (!businessId) return;
        if (nextUnit && nextUnit !== stock.unit) {
            await updateStockItemUnit(businessId, stock.stockItemId, nextUnit);
        }
    };

    const doSetStockTo = async (stock: StockItemSummary) => {
        if (saving) return;
        setError(null);

        try {
            if (!businessId) throw new Error("Missing businessId");
            const target = Number(editSetToQty);
            if (!Number.isFinite(target) || target < 0) throw new Error("Invalid target qty");

            const delta = target - stock.qtyOnHand;
            if (delta === 0) return;

            let unitCostSnapshot: number | null = null;
            if (editSetUnitCost.trim()) {
                const c = Number(editSetUnitCost);
                if (!Number.isFinite(c) || c < 0) throw new Error("Invalid unit cost");
                unitCostSnapshot = c;
            }

            setSaving(true);
            await doUpdateUnitIfNeeded(stock);

            await createStockMovement(businessId, stock.stockItemId, {
                type: "Adjust",
                delta,
                unitCostSnapshot,
                note: `Set stock to ${target}`,
            });

            await load();
        } catch (e: any) {
            setError(e?.message || "Failed to set stock");
        } finally {
            setSaving(false);
        }
    };

    const doReceive = async (stock: StockItemSummary) => {
        if (saving) return;
        setError(null);

        try {
            if (!businessId) throw new Error("Missing businessId");
            const qty = Number(editReceiveQty);
            if (!Number.isFinite(qty) || qty <= 0) throw new Error("Receive qty must be > 0");

            const cost = Number(editReceiveUnitCost);
            if (!Number.isFinite(cost) || cost <= 0) throw new Error("Unit cost is required");

            setSaving(true);
            await doUpdateUnitIfNeeded(stock);

            await createStockMovement(businessId, stock.stockItemId, {
                type: "Receive",
                delta: qty,
                unitCostSnapshot: cost,
                note: "Receive stock",
            });

            await load();
        } catch (e: any) {
            setError(e?.message || "Failed to receive stock");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="inventory-container">
            <div className="action-bar">
                <h2 className="section-title">Products & Stock</h2>

                <div className="action-bar__right">
                    <input
                        className="inventory-search"
                        placeholder="Search products…"
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        disabled={loading}
                    />
                    <button className="btn btn-ghost" onClick={load} disabled={loading}>
                        {loading ? "Loading…" : "Refresh"}
                    </button>
                    {canManage && (
                        <button className="btn btn-primary" onClick={openCreate} disabled={loading}>
                            + New product
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

            <div className="inventory-table-wrap">
                <table className="inventory-table">
                    <thead>
                    <tr>
                        <th>Product</th>
                        <th className="right">Price</th>
                        <th>Tax</th>
                        <th className="right">Stock</th>
                        <th className="right">Actions</th>
                    </tr>
                    </thead>

                    <tbody>
                    {loading && (
                        <tr>
                            <td colSpan={5}>
                                <span className="muted">Loading inventory…</span>
                            </td>
                        </tr>
                    )}

                    {!loading && rows.length === 0 && (
                        <tr>
                            <td colSpan={5}>
                                <span className="muted">No products found</span>
                            </td>
                        </tr>
                    )}

                    {!loading &&
                        rows.map(({ product, stock }) => (
                            <tr key={product.catalogItemId} className="inventory-row">
                                <td>
                                    <div className="cell-main">{product.name}</div>
                                    <div className="muted" style={{ fontSize: 12 }}>
                                        {product.code}
                                    </div>
                                </td>

                                <td className="right">{formatMoney(product.basePrice, "EUR")}</td>
                                <td className="muted">{product.taxClass}</td>

                                <td className="right">
                                    {stock ? (
                                        <span className="stock-pill">
                        {stock.qtyOnHand} {stock.unit}
                      </span>
                                    ) : (
                                        <span className="muted">Not tracked</span>
                                    )}
                                </td>

                                <td className="right">
                                    {canManage && (
                                        <button
                                            className="btn btn-ghost"
                                            type="button"
                                            onClick={() => openManage(product)}
                                            disabled={saving}
                                        >
                                            Manage
                                        </button>
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* ✅ CREATE PRODUCT MODAL */}
            {showCreate && canManage && (
                <div className="modal-overlay" onClick={() => setShowCreate(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">New Product</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Name</label>
                                <input value={createName} onChange={(e) => setCreateName(e.target.value)} disabled={saving} />
                            </div>

                            <div className="modal-field">
                                <label>Base price</label>
                                <input type="number" value={createPrice} onChange={(e) => setCreatePrice(e.target.value)} disabled={saving} />
                            </div>

                            <div className="modal-field">
                                <label>Tax class</label>
                                <input value={createTaxClass} onChange={(e) => setCreateTaxClass(e.target.value)} disabled={saving} />
                            </div>

                            <div className="modal-field">
                                <label style={{ display: "flex", gap: 10, alignItems: "center" }}>
                                    <input
                                        type="checkbox"
                                        checked={createTrackStock}
                                        onChange={(e) => setCreateTrackStock(e.target.checked)}
                                        disabled={saving}
                                    />
                                    Track stock for this product
                                </label>
                            </div>

                            {createTrackStock && (
                                <>
                                    <div className="modal-field">
                                        <label>Unit</label>
                                        <select value={createUnit} onChange={(e) => setCreateUnit(e.target.value as any)} disabled={saving}>
                                            {UNITS.map((u) => (
                                                <option key={u} value={u}>
                                                    {u}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="modal-field">
                                        <label>Initial qty</label>
                                        <input type="number" value={createInitialQty} onChange={(e) => setCreateInitialQty(e.target.value)} disabled={saving} />
                                    </div>
                                </>
                            )}
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowCreate(false)} disabled={saving}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={doCreate} disabled={saving}>
                                {saving ? "Creating…" : "Create"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* ✅ MANAGE PRODUCT + STOCK MODAL */}
            {selected && editProductId && (
                <div className="modal-overlay" onClick={closeManage}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">Manage Product</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Name</label>
                                <input value={editName} onChange={(e) => setEditName(e.target.value)} disabled={saving} />
                            </div>

                            <div className="modal-field">
                                <label>Base price</label>
                                <input type="number" value={editPrice} onChange={(e) => setEditPrice(e.target.value)} disabled={saving} />
                            </div>

                            <div className="modal-field">
                                <label>Tax class</label>
                                <input value={editTaxClass} onChange={(e) => setEditTaxClass(e.target.value)} disabled={saving} />
                            </div>

                            <div className="modal-field">
                                <button className="btn btn-success" onClick={doSaveProduct} disabled={saving} type="button">
                                    {saving ? "Saving…" : "Save product"}
                                </button>
                            </div>

                            {(() => {
                                const stock = stockByCatalogId.get(editProductId);

                                if (!stock) {
                                    return (
                                        <div className="modal-field">
                                            <label>Stock tracking</label>
                                            <div className="no-inventory" style={{ padding: 0 }}>
                                                This product is not tracked in stock yet.
                                            </div>

                                            <div style={{ display: "grid", gap: 10, marginTop: 10 }}>
                                                <div>
                                                    <label style={{ fontSize: 13, opacity: 0.8 }}>Unit</label>
                                                    <select
                                                        value={editEnableUnit}
                                                        onChange={(e) => setEditEnableUnit(e.target.value as any)}
                                                        disabled={saving}
                                                    >
                                                        {UNITS.map((u) => (
                                                            <option key={u} value={u}>
                                                                {u}
                                                            </option>
                                                        ))}
                                                    </select>
                                                </div>

                                                <input
                                                    placeholder="Initial qty"
                                                    type="number"
                                                    value={editEnableQty}
                                                    onChange={(e) => setEditEnableQty(e.target.value)}
                                                    disabled={saving}
                                                />

                                                <button className="btn btn-primary" onClick={doEnableStock} disabled={saving} type="button">
                                                    Enable stock tracking
                                                </button>
                                            </div>
                                        </div>
                                    );
                                }

                                return (
                                    <>
                                        <div className="modal-field">
                                            <label>Stock ({stock.qtyOnHand} {stock.unit})</label>
                                            <select value={editUnit} onChange={(e) => setEditUnit(e.target.value as any)} disabled={saving}>
                                                {UNITS.map((u) => (
                                                    <option key={u} value={u}>
                                                        {u}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>

                                        <div className="modal-field">
                                            <label>Set stock to</label>
                                            <div style={{ display: "grid", gap: 10 }}>
                                                <input
                                                    type="number"
                                                    placeholder="Target qty"
                                                    value={editSetToQty}
                                                    onChange={(e) => setEditSetToQty(e.target.value)}
                                                    disabled={saving}
                                                />
                                                <input
                                                    type="number"
                                                    placeholder="Unit cost snapshot (optional)"
                                                    value={editSetUnitCost}
                                                    onChange={(e) => setEditSetUnitCost(e.target.value)}
                                                    disabled={saving}
                                                />
                                                <button className="btn" onClick={() => doSetStockTo(stock)} disabled={saving} type="button">
                                                    Apply set-to
                                                </button>
                                            </div>
                                        </div>

                                        <div className="modal-field">
                                            <label>Receive stock</label>
                                            <div style={{ display: "grid", gap: 10 }}>
                                                <input
                                                    type="number"
                                                    placeholder="Receive qty"
                                                    value={editReceiveQty}
                                                    onChange={(e) => setEditReceiveQty(e.target.value)}
                                                    disabled={saving}
                                                />
                                                <input
                                                    type="number"
                                                    placeholder="Unit cost (required)"
                                                    value={editReceiveUnitCost}
                                                    onChange={(e) => setEditReceiveUnitCost(e.target.value)}
                                                    disabled={saving}
                                                />
                                                <button className="btn" onClick={() => doReceive(stock)} disabled={saving} type="button">
                                                    Receive
                                                </button>
                                            </div>
                                        </div>
                                    </>
                                );
                            })()}
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={closeManage} disabled={saving}>
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

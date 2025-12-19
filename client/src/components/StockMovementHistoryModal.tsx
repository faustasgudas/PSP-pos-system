import { useEffect, useMemo, useState } from "react";
import { listStockMovements, type StockMovement } from "../frontapi/stockApi";
import { logout } from "../frontapi/authApi";

type MovementType = "" | "Receive" | "Sale" | "RefundReturn" | "Waste" | "Adjust";

const TYPE_OPTIONS: Array<{ value: MovementType; label: string }> = [
    { value: "", label: "All types" },
    { value: "Receive", label: "Receive" },
    { value: "Sale", label: "Sale" },
    { value: "RefundReturn", label: "Refund/Return" },
    { value: "Waste", label: "Waste" },
    { value: "Adjust", label: "Adjust" },
];

const PREVIEW_LIMIT = 200;

function isAuthProblemMessage(msg: string | null): boolean {
    const e = (msg ?? "").toLowerCase();
    return e.includes("unauthorized") || e.includes("forbid") || e.includes("401") || e.includes("403");
}

function safeLocalDateTime(iso: string): string {
    if (!iso) return "";
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return String(iso);
    return d.toLocaleString();
}

function ymdToIsoStart(ymd: string): string | undefined {
    if (!ymd) return undefined;
    const d = new Date(`${ymd}T00:00:00`);
    if (Number.isNaN(d.getTime())) return undefined;
    return d.toISOString();
}

function ymdToIsoEnd(ymd: string): string | undefined {
    if (!ymd) return undefined;
    const d = new Date(`${ymd}T23:59:59.999`);
    if (Number.isNaN(d.getTime())) return undefined;
    return d.toISOString();
}

export function StockMovementHistoryModal(props: {
    open: boolean;
    onClose: () => void;
    businessId: number;
    stockItemId: number;
    productName?: string;
    productCode?: string;
    unit?: string;
    canView: boolean;
    refreshKey?: number;
}) {
    const { open, onClose, businessId, stockItemId, productName, productCode, unit, canView, refreshKey } = props;

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [movements, setMovements] = useState<StockMovement[]>([]);

    const [type, setType] = useState<MovementType>("");
    const [dateFromYmd, setDateFromYmd] = useState("");
    const [dateToYmd, setDateToYmd] = useState("");

    const [showAll, setShowAll] = useState(false);

    const params = useMemo(() => {
        const dateFrom = ymdToIsoStart(dateFromYmd);
        const dateTo = ymdToIsoEnd(dateToYmd);

        return {
            type: type || undefined,
            dateFrom,
            dateTo,
        };
    }, [type, dateFromYmd, dateToYmd]);

    const authProblem = useMemo(() => isAuthProblemMessage(error), [error]);

    useEffect(() => {
        if (!open) return;
        // Reset view-only UI each time modal opens
        setShowAll(false);
    }, [open]);

    useEffect(() => {
        if (!open) return;
        if (!canView) return;

        let alive = true;

        const load = async () => {
            setLoading(true);
            setError(null);
            try {
                if (!businessId || !Number.isFinite(businessId)) throw new Error("Missing businessId");
                if (!stockItemId || !Number.isFinite(stockItemId)) throw new Error("Missing stock item");

                const data = await listStockMovements(businessId, stockItemId, params);
                if (!alive) return;
                setMovements(Array.isArray(data) ? data : []);
            } catch (e: any) {
                if (!alive) return;
                setError(e?.message || "Failed to load stock movements");
                setMovements([]);
            } finally {
                if (!alive) return;
                setLoading(false);
            }
        };

        void load();
        return () => {
            alive = false;
        };
    }, [open, canView, businessId, stockItemId, params, refreshKey]);

    const visibleMovements = useMemo(() => {
        if (showAll) return movements;
        return movements.slice(0, PREVIEW_LIMIT);
    }, [movements, showAll]);

    if (!open) return null;
    if (!canView) return null;

    return (
        <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
            <div className="modal-card modal-card--wide" onClick={(e) => e.stopPropagation()}>
                <div className="history-header">
                    <div>
                        <div className="history-title">Stock movement history</div>
                        <div className="muted" style={{ fontSize: 12 }}>
                            {productName ? <strong>{productName}</strong> : "Selected item"}
                            {productCode ? ` • ${productCode}` : ""}
                            {unit ? ` • Unit: ${unit}` : ""}
                        </div>
                    </div>

                    <div className="history-actions">
                        <button className="btn btn-ghost" type="button" onClick={onClose} disabled={loading}>
                            Close
                        </button>
                    </div>
                </div>

                <div className="history-filters">
                    <div className="history-filter">
                        <label>Type</label>
                        <select
                            value={type}
                            onChange={(e) => setType(e.target.value as MovementType)}
                            disabled={loading}
                        >
                            {TYPE_OPTIONS.map((o) => (
                                <option key={o.value || "all"} value={o.value}>
                                    {o.label}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="history-filter">
                        <label>Date from</label>
                        <input
                            type="date"
                            value={dateFromYmd}
                            onChange={(e) => setDateFromYmd(e.target.value)}
                            disabled={loading}
                        />
                    </div>

                    <div className="history-filter">
                        <label>Date to</label>
                        <input
                            type="date"
                            value={dateToYmd}
                            onChange={(e) => setDateToYmd(e.target.value)}
                            disabled={loading}
                        />
                    </div>

                    <div className="history-filter history-filter--actions">
                        <button
                            className="btn"
                            type="button"
                            onClick={() => {
                                setType("");
                                setDateFromYmd("");
                                setDateToYmd("");
                            }}
                            disabled={loading && movements.length === 0}
                        >
                            Clear
                        </button>
                    </div>
                </div>

                {error && (
                    <div className="history-error">
                        <div style={{ color: "#b01d1d" }}>{error}</div>
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

                <div className="history-meta muted" style={{ marginTop: 8, fontSize: 12 }}>
                    {loading ? "Loading…" : `${movements.length} movement${movements.length === 1 ? "" : "s"}`}
                    {!showAll && movements.length > PREVIEW_LIMIT ? ` • showing first ${PREVIEW_LIMIT}` : ""}
                </div>

                <div className="inventory-table-wrap history-table-wrap">
                    <table className="inventory-table">
                        <thead>
                            <tr>
                                <th style={{ width: 190 }}>At</th>
                                <th style={{ width: 130 }}>Type</th>
                                <th className="right" style={{ width: 120 }}>Δ Qty</th>
                                <th className="right" style={{ width: 160 }}>Unit cost</th>
                                <th style={{ width: 140 }}>Order line</th>
                                <th>Note</th>
                            </tr>
                        </thead>
                        <tbody>
                            {loading && (
                                <tr>
                                    <td colSpan={6}>
                                        <span className="muted">Loading stock movements…</span>
                                    </td>
                                </tr>
                            )}

                            {!loading && visibleMovements.length === 0 && (
                                <tr>
                                    <td colSpan={6}>
                                        <span className="muted">No stock movements found</span>
                                    </td>
                                </tr>
                            )}

                            {!loading &&
                                visibleMovements.map((m) => {
                                    const delta = Number(m.delta) || 0;
                                    const isPos = delta > 0;
                                    const isNeg = delta < 0;
                                    const deltaLabel = `${delta > 0 ? "+" : ""}${delta}${unit ? ` ${unit}` : ""}`;

                                    return (
                                        <tr key={m.stockMovementId}>
                                            <td>{safeLocalDateTime(m.at)}</td>
                                            <td>{m.type}</td>
                                            <td className="right">
                                                <span className={`delta-pill ${isPos ? "is-pos" : ""} ${isNeg ? "is-neg" : ""}`}>
                                                    {deltaLabel}
                                                </span>
                                            </td>
                                            <td className="right">
                                                {m.unitCostSnapshot == null ? (
                                                    <span className="muted">—</span>
                                                ) : (
                                                    Number(m.unitCostSnapshot).toFixed(4)
                                                )}
                                            </td>
                                            <td>
                                                {m.orderLineId == null ? (
                                                    <span className="muted">—</span>
                                                ) : (
                                                    <span>#{m.orderLineId}</span>
                                                )}
                                            </td>
                                            <td>{m.note ? m.note : <span className="muted">—</span>}</td>
                                        </tr>
                                    );
                                })}
                        </tbody>
                    </table>
                </div>

                {!showAll && movements.length > PREVIEW_LIMIT && (
                    <div className="history-more">
                        <button className="btn btn-secondary" type="button" onClick={() => setShowAll(true)} disabled={loading}>
                            Show all ({movements.length})
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}



import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import {
    addOrderLine,
    cancelOrder,
    closeOrder,
    getOrder,
    reopenOrder,
    refundOrder,
    removeOrderLine,
    updateOrder,
    updateOrderLine,
    type OrderDetail,
    type OrderLine,
} from "../../../frontapi/orderApi";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import { updateReservation } from "../../../frontapi/reservationsApi";
import { getUserFromToken } from "../../../utils/auth";
import CateringOrderSplitDialog from "./CateringOrderSplitDialog";

function calcTotal(lines: OrderLine[]) {
    return lines.reduce((sum, l) => sum + Number(l.unitPriceSnapshot) * Number(l.qty), 0);
}
export default function CateringOrderDetails(props: { orderId: number; onBack: () => void; onPay: (orderId: number) => void }) {
    const user = getUserFromToken();
    const role = user?.role ?? "";

    const businessId = Number(localStorage.getItem("businessId"));
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [order, setOrder] = useState<OrderDetail | null>(null);
    const [busy, setBusy] = useState(false);
    const [busyLineIds, setBusyLineIds] = useState<Set<number>>(new Set());
    const [showSplit, setShowSplit] = useState(false);

    const [catalogLoading, setCatalogLoading] = useState(true);
    const [catalog, setCatalog] = useState<CatalogItem[]>([]);
    const [catalogQuery, setCatalogQuery] = useState("");

    const [editTableOrArea, setEditTableOrArea] = useState<string>("");

    const canManage = role === "Owner" || role === "Manager";
    const canEdit = (order?.status ?? "") === "Open";
    const canClose = canEdit;
    const canCancel = canEdit;
    const canSplit = canEdit;
    const canPay = canEdit && (order?.lines?.length ?? 0) > 0;
    const canRefund = canManage && (order?.status ?? "") === "Closed";
    const canReopen = canManage && (order?.status ?? "") !== "Open";

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const dto = await getOrder(props.orderId);
            setOrder(dto);
            setEditTableOrArea(String(dto?.tableOrArea ?? ""));
        } catch (e: any) {
            setOrder(null);
            setError(e?.message || "Failed to load order");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [props.orderId]);

    useEffect(() => {
        if (!businessId) {
            setCatalog([]);
            setCatalogLoading(false);
            return;
        }
        setCatalogLoading(true);
        listCatalogItems(businessId, { status: "Active" })
            .then((d) => setCatalog(Array.isArray(d) ? d : []))
            .catch(() => setCatalog([]))
            .finally(() => setCatalogLoading(false));
    }, [businessId]);

    const total = useMemo(() => calcTotal(order?.lines ?? []), [order?.lines]);

    const saveTable = async () => {
        if (!canEdit || busy) return;
        if (!employeeId) return setError("Missing employeeId");
        const next = editTableOrArea.trim();
        if (!next) return setError("Table is required (e.g. T12)");

        setBusy(true);
        setError(null);
        try {
            const updated = await updateOrder(props.orderId, {
                employeeId,
                tableOrArea: next,
            });
            setOrder(updated);
        } catch (e: any) {
            setError(e?.message || "Failed to update table");
        } finally {
            setBusy(false);
        }
    };

    const markLineBusy = (lineId: number, on: boolean) => {
        setBusyLineIds((prev) => {
            const next = new Set(prev);
            if (on) next.add(lineId);
            else next.delete(lineId);
            return next;
        });
    };

    const syncReservationStatus = async (reservationId: number, status: "Completed" | "Cancelled") => {
        // Best-effort. Reservation updates are owner/manager only in backend.
        if (!businessId) return;
        try {
            await updateReservation(businessId, reservationId, { status });
        } catch (e: any) {
            setError((prev) => prev ?? `Order updated, but reservation was not updated: ${e?.message || "forbidden"}`);
        }
    };

    const filteredCatalog = useMemo(() => {
        const q = catalogQuery.trim().toLowerCase();
        let list = catalog.filter((i) => String(i.status).toLowerCase() === "active");
        if (q) list = list.filter((i) => i.name.toLowerCase().includes(q) || i.code.toLowerCase().includes(q));
        return list.slice(0, 60);
    }, [catalog, catalogQuery]);

    const addItem = async (item: CatalogItem) => {
        if (!canEdit || busy) return;
        setError(null);
        setBusy(true);
        try {
            const line = await addOrderLine(props.orderId, item.catalogItemId, 1);
            setOrder((prev) => (prev ? { ...prev, lines: [...prev.lines, line] } : prev));
        } catch (e: any) {
            setError(e?.message || "Failed to add line");
        } finally {
            setBusy(false);
        }
    };

    const inc = async (l: OrderLine) => {
        if (!canEdit) return;
        if (busy || busyLineIds.has(l.orderLineId)) return;
        setError(null);
        markLineBusy(l.orderLineId, true);
        try {
            const updated = await updateOrderLine(props.orderId, l.orderLineId, Number(l.qty) + 1, l.discountId);
            setOrder((prev) =>
                prev ? { ...prev, lines: prev.lines.map((x) => (x.orderLineId === updated.orderLineId ? updated : x)) } : prev
            );
        } catch (e: any) {
            setError(e?.message || "Failed to update line");
        } finally {
            markLineBusy(l.orderLineId, false);
        }
    };

    const dec = async (l: OrderLine) => {
        if (!canEdit) return;
        if (busy || busyLineIds.has(l.orderLineId)) return;
        setError(null);
        const next = Number(l.qty) - 1;
        if (next <= 0) return remove(l);
        markLineBusy(l.orderLineId, true);
        try {
            const updated = await updateOrderLine(props.orderId, l.orderLineId, next, l.discountId);
            setOrder((prev) =>
                prev ? { ...prev, lines: prev.lines.map((x) => (x.orderLineId === updated.orderLineId ? updated : x)) } : prev
            );
        } catch (e: any) {
            setError(e?.message || "Failed to update line");
        } finally {
            markLineBusy(l.orderLineId, false);
        }
    };

    const remove = async (l: OrderLine) => {
        if (!canEdit) return;
        if (busy || busyLineIds.has(l.orderLineId)) return;
        setError(null);
        markLineBusy(l.orderLineId, true);
        try {
            await removeOrderLine(props.orderId, l.orderLineId);
            setOrder((prev) => (prev ? { ...prev, lines: prev.lines.filter((x) => x.orderLineId !== l.orderLineId) } : prev));
        } catch (e: any) {
            setError(e?.message || "Failed to remove line");
        } finally {
            markLineBusy(l.orderLineId, false);
        }
    };

    const doClose = async () => {
        if (!canClose || busy) return;
        setBusy(true);
        setError(null);
        try {
            const updated = await closeOrder(props.orderId);
            setOrder(updated);
            if (updated?.reservationId) await syncReservationStatus(updated.reservationId, "Completed");
        } catch (e: any) {
            setError(e?.message || "Close failed");
        } finally {
            setBusy(false);
        }
    };

    const doCancel = async () => {
        if (!canCancel || busy) return;
        if (!employeeId) return setError("Missing employeeId");
        const reason = window.prompt("Cancel reason (optional):") ?? undefined;
        setBusy(true);
        setError(null);
        try {
            const updated = await cancelOrder(props.orderId, employeeId, reason);
            setOrder(updated);
            if (updated?.reservationId) await syncReservationStatus(updated.reservationId, "Cancelled");
        } catch (e: any) {
            setError(e?.message || "Cancel failed");
        } finally {
            setBusy(false);
        }
    };

    const doRefund = async () => {
        if (!canRefund || busy) return;
        if (!employeeId) return setError("Missing employeeId");
        const ok = window.confirm(`Refund order #${props.orderId}?`);
        if (!ok) return;
        const reason = window.prompt("Refund reason (optional):") ?? undefined;
        setBusy(true);
        setError(null);
        try {
            const updated = await refundOrder(props.orderId, employeeId, reason);
            setOrder(updated);
        } catch (e: any) {
            setError(e?.message || "Refund failed");
        } finally {
            setBusy(false);
        }
    };

    const doReopen = async () => {
        if (!canReopen || busy) return;
        setBusy(true);
        setError(null);
        try {
            const updated = await reopenOrder(props.orderId);
            setOrder(updated);
        } catch (e: any) {
            setError(e?.message || "Reopen failed");
        } finally {
            setBusy(false);
        }
    };

    if (loading) return <div className="page"><div className="card">Loading…</div></div>;
    if (!order)
        return (
            <div className="page">
                <div className="card">
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "center" }}>
                        <strong>Order #{props.orderId}</strong>
                        <button className="btn" onClick={props.onBack}>← Back</button>
                    </div>
                    <div className="muted" style={{ marginTop: 10 }}>{error ?? "Not found"}</div>
                </div>
            </div>
        );

    return (
        <div className="page">
            <div className="action-bar">
                <h2 className="section-title">Order #{order.orderId}</h2>
                <div className="action-buttons">
                    <button className="btn" onClick={props.onBack}>← Back</button>
                    <button className="btn" onClick={load} disabled={busy}>Refresh</button>
                    <button className="btn" onClick={() => setShowSplit(true)} disabled={!canSplit || busy || order.lines.length === 0}>
                        Split…
                    </button>
                    <button
                        className="btn btn-primary"
                        onClick={() => props.onPay(order.orderId)}
                        disabled={!canPay || busy}
                        title={!canPay ? "Only open orders with at least one line can be paid" : ""}
                    >
                        Pay
                    </button>
                    <button className="btn" onClick={doRefund} disabled={!canRefund || busy} title={!canRefund ? "Closed orders only (Owner/Manager)" : ""}>
                        Refund
                    </button>
                    <button className="btn" onClick={doReopen} disabled={!canReopen || busy} title={!canReopen ? "Owner/Manager only" : ""}>
                        Reopen
                    </button>
                    <button className="btn" onClick={doClose} disabled={!canClose || busy}>
                        Close
                    </button>
                    <button className="btn btn-danger" onClick={doCancel} disabled={!canCancel || busy}>
                        Cancel
                    </button>
                </div>
            </div>

            {error && (
                <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                    {error}
                </div>
            )}

            <div className="card" style={{ marginBottom: 12, textAlign: "left" }}>
                <div className="muted" style={{ marginBottom: 6 }}>Table</div>
                <div style={{ display: "flex", gap: 10, alignItems: "center", flexWrap: "wrap" }}>
                    <input
                        className="dropdown"
                        value={editTableOrArea}
                        onChange={(e) => setEditTableOrArea(e.target.value)}
                        disabled={!canEdit || busy}
                        placeholder="e.g. T12 / Patio"
                        style={{ maxWidth: 340 }}
                    />
                    <button className="btn btn-primary" onClick={saveTable} disabled={!canEdit || busy}>
                        Save table
                    </button>
                    {!canEdit && <span className="muted">Only open orders can be edited.</span>}
                </div>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 12 }}>
                <div className="card">
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                        <h3 style={{ margin: 0 }}>Add item</h3>
                        <input className="dropdown" value={catalogQuery} onChange={(e) => setCatalogQuery(e.target.value)} placeholder="Search…" style={{ maxWidth: 260 }} />
                    </div>
                    <div className="muted" style={{ marginTop: 6 }}>
                        Status: <strong>{order.status}</strong>
                        {order.reservationId ? ` • Reservation #${order.reservationId}` : ""}
                        {canEdit ? "" : " • (Only Open orders can be edited)"}
                    </div>

                    <div style={{ marginTop: 12, display: "grid", gap: 8 }}>
                        {catalogLoading ? (
                            <div className="muted">Loading catalog…</div>
                        ) : (
                            filteredCatalog.map((i) => (
                                <button key={i.catalogItemId} className="btn" onClick={() => addItem(i)} disabled={!canEdit || busy} style={{ textAlign: "left" }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                        <div>
                                            <strong>{i.name}</strong> <span className="muted">({i.type})</span>
                                        </div>
                                        <div className="muted">€{Number(i.basePrice).toFixed(2)}</div>
                                    </div>
                                </button>
                            ))
                        )}
                    </div>
                </div>

                <div className="card">
                    <h3 style={{ marginTop: 0 }}>Lines</h3>
                    {order.lines.length === 0 ? (
                        <div className="muted">No lines.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {order.lines
                                .slice()
                                .sort((a, b) => b.orderLineId - a.orderLineId)
                                .map((l) => {
                                    const lineBusy = busy || busyLineIds.has(l.orderLineId);
                                    return (
                                        <div key={l.orderLineId} className="card" style={{ padding: 12 }}>
                                            <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                                <div style={{ minWidth: 180 }}>
                                                    <strong>{l.itemNameSnapshot}</strong>
                                                    <div className="muted">€{Number(l.unitPriceSnapshot).toFixed(2)} each</div>
                                                </div>
                                                <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                                                    <button className="btn" onClick={() => dec(l)} disabled={!canEdit || lineBusy}>−</button>
                                                    <div className="card" style={{ padding: "8px 10px" }}>{l.qty}</div>
                                                    <button className="btn" onClick={() => inc(l)} disabled={!canEdit || lineBusy}>+</button>
                                                    <div style={{ width: 90, textAlign: "right", fontWeight: 800 }}>
                                                        €{(Number(l.unitPriceSnapshot) * Number(l.qty)).toFixed(2)}
                                                    </div>
                                                    <button className="btn btn-danger" onClick={() => remove(l)} disabled={!canEdit || lineBusy}>✕</button>
                                                </div>
                                            </div>
                                        </div>
                                    );
                                })}
                        </div>
                    )}

                    <div style={{ marginTop: 14, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                        <strong>Total</strong>
                        <strong>€{total.toFixed(2)}</strong>
                    </div>
                </div>
            </div>

            {showSplit && (
                <CateringOrderSplitDialog
                    fromOrderId={order.orderId}
                    lines={order.lines}
                    onClose={() => setShowSplit(false)}
                    onMoved={() => {
                        setShowSplit(false);
                        load();
                    }}
                />
            )}
        </div>
    );
}



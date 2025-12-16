import { useEffect, useMemo, useState } from "react";
import {
    addOrderLine,
    cancelOrder,
    closeOrder,
    getOrder,
    removeOrderLine,
    reopenOrder,
    refundOrder,
    updateOrderLine,
    type OrderDetail,
    type OrderLine,
} from "../../../frontapi/orderApi";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";
import { updateReservation } from "../../../frontapi/reservationsApi";

import "../../../App.css";
import "./BeautyOrderDetails.css";
import BeautyOrderSplitDialog from "./BeautyOrderSplitDialog";

function calcTotal(lines: OrderLine[]) {
    return lines.reduce((sum, l) => sum + Number(l.unitPriceSnapshot) * Number(l.qty), 0);
}

export default function BeautyOrderDetails(props: {
    orderId: number;
    onBack: () => void;
    onPay: (orderId: number) => void;
}) {
    const user = getUserFromToken();
    const role = user?.role ?? "";

    const businessId = Number(localStorage.getItem("businessId"));
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [order, setOrder] = useState<OrderDetail | null>(null);

    const [servicesLoading, setServicesLoading] = useState(true);
    const [services, setServices] = useState<CatalogItem[]>([]);
    const [serviceError, setServiceError] = useState<string | null>(null);

    const [busyLineIds, setBusyLineIds] = useState<Set<number>>(new Set());
    const [busyOrderAction, setBusyOrderAction] = useState(false);

    const [showSplit, setShowSplit] = useState(false);
    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await getOrder(props.orderId);
            setOrder(data);
        } catch (e: any) {
            setError(e?.message || "Failed to load order");
            setOrder(null);
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
            setServiceError("Missing businessId");
            setServices([]);
            setServicesLoading(false);
            return;
        }

        setServicesLoading(true);
        setServiceError(null);
        getActiveServices(businessId)
            .then((data) => setServices(Array.isArray(data) ? data : []))
            .catch((e: any) => setServiceError(e?.message || "Failed to load services"))
            .finally(() => setServicesLoading(false));
    }, [businessId]);

    const total = useMemo(() => calcTotal(order?.lines ?? []), [order?.lines]);

    const markLineBusy = (lineId: number, busy: boolean) => {
        setBusyLineIds((prev) => {
            const next = new Set(prev);
            if (busy) next.add(lineId);
            else next.delete(lineId);
            return next;
        });
    };

    const canEdit = (order?.status ?? "") === "Open";
    const canCancel = canEdit;
    const canClose = canEdit;
    const canSplit = canEdit;
    const canPay = canEdit;
    const canRefund = (role === "Owner" || role === "Manager") && (order?.status ?? "") === "Closed";
    const canReopen = (role === "Owner" || role === "Manager") && (order?.status ?? "") !== "Open";

    const syncReservationStatus = async (reservationId: number, status: "Completed" | "Cancelled") => {
        // Reservation update requires manager/owner in backend; don't break order flow if this fails.
        if (!businessId) return;
        try {
            await updateReservation(businessId, reservationId, { status });
        } catch (e: any) {
            // Non-blocking warning
            setError((prev) => prev ?? `Order updated, but reservation was not updated: ${e?.message || "forbidden"}`);
        }
    };

    const addService = async (service: CatalogItem) => {
        if (!canEdit || busyOrderAction) return;
        setError(null);
        try {
            const line = await addOrderLine(props.orderId, service.catalogItemId, 1);
            setOrder((prev) =>
                prev ? { ...prev, lines: [...(prev.lines ?? []), line] } : prev
            );
        } catch (e: any) {
            setError(e?.message || "Failed to add line");
        }
    };

    const inc = async (line: OrderLine) => {
        if (!canEdit) return;
        if (busyLineIds.has(line.orderLineId)) return;
        setError(null);
        markLineBusy(line.orderLineId, true);
        try {
            const updated = await updateOrderLine(
                props.orderId,
                line.orderLineId,
                Number(line.qty) + 1,
                line.discountId
            );
            setOrder((prev) =>
                prev
                    ? {
                          ...prev,
                          lines: prev.lines.map((l) =>
                              l.orderLineId === updated.orderLineId ? updated : l
                          ),
                      }
                    : prev
            );
        } catch (e: any) {
            setError(e?.message || "Failed to update line");
        } finally {
            markLineBusy(line.orderLineId, false);
        }
    };

    const dec = async (line: OrderLine) => {
        if (!canEdit) return;
        if (busyLineIds.has(line.orderLineId)) return;
        setError(null);

        const nextQty = Number(line.qty) - 1;
        if (nextQty <= 0) {
            // UX: decrement to zero => remove line
            await remove(line);
            return;
        }

        markLineBusy(line.orderLineId, true);
        try {
            const updated = await updateOrderLine(
                props.orderId,
                line.orderLineId,
                nextQty,
                line.discountId
            );
            setOrder((prev) =>
                prev
                    ? {
                          ...prev,
                          lines: prev.lines.map((l) =>
                              l.orderLineId === updated.orderLineId ? updated : l
                          ),
                      }
                    : prev
            );
        } catch (e: any) {
            setError(e?.message || "Failed to update line");
        } finally {
            markLineBusy(line.orderLineId, false);
        }
    };

    const remove = async (line: OrderLine) => {
        if (!canEdit) return;
        if (busyLineIds.has(line.orderLineId)) return;
        setError(null);
        markLineBusy(line.orderLineId, true);
        try {
            await removeOrderLine(props.orderId, line.orderLineId);
            setOrder((prev) =>
                prev
                    ? {
                          ...prev,
                          lines: prev.lines.filter((l) => l.orderLineId !== line.orderLineId),
                      }
                    : prev
            );
        } catch (e: any) {
            setError(e?.message || "Failed to remove line");
        } finally {
            markLineBusy(line.orderLineId, false);
        }
    };

    const doClose = async () => {
        if (!canClose || busyOrderAction) return;
        setError(null);
        setBusyOrderAction(true);
        try {
            const updated = await closeOrder(props.orderId);
            setOrder(updated);
            if (updated?.reservationId) {
                await syncReservationStatus(updated.reservationId, "Completed");
            }
        } catch (e: any) {
            setError(e?.message || "Failed to close order");
        } finally {
            setBusyOrderAction(false);
        }
    };

    const doCancel = async () => {
        if (!canCancel || busyOrderAction) return;
        if (!employeeId) {
            setError("Missing employeeId");
            return;
        }
        const reason = window.prompt("Cancel reason (optional):") ?? undefined;

        setError(null);
        setBusyOrderAction(true);
        try {
            const updated = await cancelOrder(props.orderId, employeeId, reason);
            setOrder(updated);
            if (updated?.reservationId) {
                await syncReservationStatus(updated.reservationId, "Cancelled");
            }
        } catch (e: any) {
            setError(e?.message || "Failed to cancel order");
        } finally {
            setBusyOrderAction(false);
        }
    };

    const doReopen = async () => {
        if (!canReopen || busyOrderAction) return;
        setError(null);
        setBusyOrderAction(true);
        try {
            const updated = await reopenOrder(props.orderId);
            setOrder(updated);
        } catch (e: any) {
            setError(e?.message || "Failed to reopen order");
        } finally {
            setBusyOrderAction(false);
        }
    };

    const doRefund = async () => {
        if (!canRefund || busyOrderAction) return;
        if (!employeeId) {
            setError("Missing employeeId");
            return;
        }

        const ok = window.confirm(`Refund order #${props.orderId}? This will adjust stock back for product lines.`);
        if (!ok) return;

        const reason = window.prompt("Refund reason (optional):") ?? undefined;

        setError(null);
        setBusyOrderAction(true);
        try {
            const updated = await refundOrder(props.orderId, employeeId, reason);
            setOrder(updated);
        } catch (e: any) {
            setError(e?.message || "Failed to refund order");
        } finally {
            setBusyOrderAction(false);
        }
    };

    if (loading) return <div className="page">Loading order…</div>;
    if (error && !order) {
        return (
            <div className="page beauty-order-details">
                <div className="beauty-order-details__top">
                    <button className="btn" onClick={props.onBack}>
                        ← Back
                    </button>
                    <button className="btn" onClick={load}>
                        Retry
                    </button>
                </div>
                <div className="beauty-order-details__error">
                    <div>{error}</div>
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
            </div>
        );
    }

    if (!order) return <div className="page">No order found</div>;

    return (
        <div className="page beauty-order-details">
            <div className="beauty-order-details__top">
                <div className="beauty-order-details__top-left">
                    <button className="btn" onClick={props.onBack}>
                        ← Back
                    </button>
                    <button className="btn" onClick={load} disabled={busyOrderAction}>
                        Refresh
                    </button>
                </div>

                <div className="beauty-order-details__top-actions">
                    <button
                        className="btn"
                        onClick={() => setShowSplit(true)}
                        disabled={!canSplit || busyOrderAction || (order.lines?.length ?? 0) === 0}
                        title={!canSplit ? "Only open orders can be split" : ""}
                    >
                        Split…
                    </button>

                    <button
                        className="btn"
                        onClick={doRefund}
                        disabled={!canRefund || busyOrderAction}
                        title={!canRefund ? "Refund is available only for Closed orders (Owner/Manager)" : ""}
                    >
                        Refund order
                    </button>

                    <button
                        className="btn"
                        onClick={doReopen}
                        disabled={!canReopen || busyOrderAction}
                        title={!canReopen ? "Reopen is available for Manager/Owner when order is not Open" : ""}
                    >
                        Reopen order
                    </button>

                    <button
                        className="btn"
                        onClick={doClose}
                        disabled={!canClose || busyOrderAction}
                        title={!canClose ? "Only open orders can be closed" : ""}
                    >
                        Close order
                    </button>

                    <button
                        className="btn"
                        onClick={doCancel}
                        disabled={!canCancel || busyOrderAction}
                        title={!canCancel ? "Only open orders can be cancelled" : ""}
                    >
                        Cancel
                    </button>

                    <button
                        className="btn btn-primary"
                        onClick={() => props.onPay(order.orderId)}
                        disabled={!canPay || busyOrderAction || (order.lines?.length ?? 0) === 0}
                        title={!canPay ? "Only open orders can be paid" : ""}
                    >
                        Pay
                    </button>
                </div>
            </div>

            <div className="beauty-order-details__meta">
                <div className="meta-card">
                    <div className="meta-title">Order #{order.orderId}</div>
                    <div className="meta-row">
                        <span className="muted">Status</span>
                        <span className={`status-pill status-${String(order.status).toLowerCase()}`}>
                            {order.status}
                        </span>
                    </div>
                    {order.reservationId && (
                        <div className="meta-row">
                            <span className="muted">Reservation</span>
                            <span>#{order.reservationId}</span>
                        </div>
                    )}
                    <div className="meta-row">
                        <span className="muted">Created</span>
                        <span>{new Date(order.createdAt).toLocaleString()}</span>
                    </div>
                    <div className="meta-row">
                        <span className="muted">Employee</span>
                        <span>{order.employeeId}</span>
                    </div>
                    {order.closedAt && (
                        <div className="meta-row">
                            <span className="muted">Closed</span>
                            <span>{new Date(order.closedAt).toLocaleString()}</span>
                        </div>
                    )}
                    <div className="meta-row total">
                        <span>Total</span>
                        <span>€{total.toFixed(2)}</span>
                    </div>
                </div>

                <div className="meta-card">
                    <div className="meta-title">Add service</div>
                    {serviceError && <div className="beauty-order-details__error">{serviceError}</div>}
                    {servicesLoading ? (
                        <div className="muted">Loading services…</div>
                    ) : (
                        <div className="service-grid">
                            {services.map((s) => (
                                <button
                                    key={s.catalogItemId}
                                    className="service-card"
                                    onClick={() => addService(s)}
                                    disabled={!canEdit || busyOrderAction}
                                    title={!canEdit ? "Order is not open" : ""}
                                >
                                    <div className="service-name">{s.name}</div>
                                    <div className="service-price">€{s.basePrice.toFixed(2)}</div>
                                </button>
                            ))}
                        </div>
                    )}
                </div>
            </div>

            {error && <div className="beauty-order-details__error">{error}</div>}

            <div className="beauty-order-details__lines">
                <h3>Order lines</h3>
                {order.lines.length === 0 ? (
                    <div className="muted">No lines yet. Add a service to start.</div>
                ) : (
                    <div className="order-table-wrap">
                        <table className="order-table">
                            <thead>
                            <tr>
                                <th className="col-item">Item</th>
                                <th className="col-price">Price</th>
                                <th className="col-qty">Qty</th>
                                <th className="col-total">Line total</th>
                                <th className="col-actions"></th>
                            </tr>
                            </thead>

                            <tbody>
                            {order.lines
                                .slice()
                                .sort((a, b) => b.orderLineId - a.orderLineId)
                                .map((l) => {
                                    const lineBusy = busyLineIds.has(l.orderLineId) || busyOrderAction;
                                    const unit = Number(l.unitPriceSnapshot);
                                    const lineTotal = unit * Number(l.qty);

                                    return (
                                        <tr key={l.orderLineId} className={lineBusy ? "is-busy" : ""}>
                                            <td className="col-item">
                                                <div className="cell-title">
                                                    <div className="item-name">{l.itemNameSnapshot}</div>
                                                    <div className="item-sub muted small">
                                                        Line #{l.orderLineId}
                                                        {l.unitDiscountSnapshot ? " • Discount applied" : ""}
                                                    </div>
                                                </div>
                                            </td>

                                            <td className="col-price">
                                                €{unit.toFixed(2)}
                                            </td>

                                            <td className="col-qty">
                                                <div className="qty-control">
                                                    <button
                                                        className="btn btn-ghost"
                                                        onClick={() => dec(l)}
                                                        disabled={!canEdit || lineBusy}
                                                        aria-label="Decrease quantity"
                                                    >
                                                        −
                                                    </button>

                                                    <div className="qty-value">{l.qty}</div>

                                                    <button
                                                        className="btn btn-ghost"
                                                        onClick={() => inc(l)}
                                                        disabled={!canEdit || lineBusy}
                                                        aria-label="Increase quantity"
                                                    >
                                                        +
                                                    </button>
                                                </div>
                                            </td>

                                            <td className="col-total">
                                                <span className="money">€{lineTotal.toFixed(2)}</span>
                                            </td>

                                            <td className="col-actions">
                                                <button
                                                    className="btn danger btn-icon"
                                                    onClick={() => remove(l)}
                                                    disabled={!canEdit || lineBusy}
                                                    title={!canEdit ? "Order is not open" : "Remove line"}
                                                    aria-label="Remove line"
                                                >
                                                    ✕
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>

                            <tfoot>
                            <tr>
                                <td colSpan={3}></td>
                                <td className="tfoot-label">Total</td>
                                <td className="tfoot-total">€{total.toFixed(2)}</td>
                            </tr>
                            </tfoot>
                        </table>
                    </div>

                )}
            </div>

            {showSplit && (
                <BeautyOrderSplitDialog
                    fromOrderId={order.orderId}
                    lines={order.lines}
                    onClose={() => setShowSplit(false)}
                    onMoved={() => {
                        setShowSplit(false);
                        // Refresh source order after successful move
                        load();
                        // For staff UX: optionally open target order if they want
                        if (role === "Owner" || role === "Manager") {
                            // managers typically want to keep context; do nothing
                            return;
                        }
                        // Staff often wants to proceed with the split ticket
                        // (If you prefer always staying, remove this.)
                        // props.onPay(targetId); // do NOT auto-pay, just keep on source
                    }}
                />
            )}
        </div>
    );
}



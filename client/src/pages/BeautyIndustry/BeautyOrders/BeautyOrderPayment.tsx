import { useEffect, useMemo, useState } from "react";
import { createPayment, listPaymentsForOrder, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { getOrder, type OrderDetail } from "../../../frontapi/orderApi";

export default function BeautyOrderPayment(props?: {
    orderId: number;
    onBack?: () => void;
}) {
    const orderIdFromProps = props?.orderId;
    const params = new URLSearchParams(window.location.search);
    const orderIdFromQuery = Number(params.get("orderId"));
    const orderId = Number(orderIdFromProps ?? orderIdFromQuery);

    const [giftCardCode, setGiftCardCode] = useState("");
    const [giftCardAmount, setGiftCardAmount] = useState<string>("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [order, setOrder] = useState<OrderDetail | null>(null);
    const [history, setHistory] = useState<PaymentHistoryItem[]>([]);

    if (!orderId) {
        return <div className="page">No order found</div>;
    }

    const load = async () => {
        setError(null);
        try {
            const [o, h] = await Promise.all([getOrder(orderId), listPaymentsForOrder(orderId)]);
            setOrder(o);
            setHistory(Array.isArray(h) ? h : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load order/payment info");
        }
    };

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [orderId]);

    const formatCents = (cents: number, currency: string = "EUR") => {
        const amount = (Number(cents) || 0) / 100;
        try {
            return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
        } catch {
            return `${amount.toFixed(2)} ${currency}`;
        }
    };

    const orderSubtotalCents = useMemo(() => {
        if (!order) return 0;
        return order.lines.reduce((sum, l) => {
            const line = Number(l.unitPriceSnapshot) * Number(l.qty);
            return sum + Math.round(line * 100);
        }, 0);
    }, [order]);

    const estimatedTaxCents = useMemo(() => {
        if (!order) return 0;
        // NOTE: UI-only estimate. Backend currently charges without tax/discount.
        return order.lines.reduce((sum, l) => {
            const base = Number(l.unitPriceSnapshot) * Number(l.qty);
            const tax = base * (Number(l.taxRateSnapshotPct) / 100);
            return sum + Math.round(tax * 100);
        }, 0);
    }, [order]);

    const hasAnyDiscount = useMemo(() => {
        if (!order) return false;
        return Boolean(order.discountId || order.orderDiscountSnapshot || order.lines.some((l) => l.discountId || l.unitDiscountSnapshot));
    }, [order]);

    const pay = async () => {
        try {
            setLoading(true);
            setError(null);

            const amt = giftCardAmount.trim() ? Number(giftCardAmount) : null;
            const giftCardAmountCents =
                amt === null
                    ? null
                    : Number.isFinite(amt) && amt > 0
                        ? Math.round(amt * 100)
                        : null;

            const result = await createPayment(orderId, {
                currency: "EUR",
                giftCardCode: giftCardCode.trim() || undefined,
                giftCardAmountCents,
            });

            if (result.stripeUrl) {
                window.location.href = result.stripeUrl;
            } else {
                window.location.href = "/payment-success";
            }
        } catch (e) {
            console.error(e);
            setError((e as any)?.message || "Payment failed");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="page">
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12 }}>
                <h2 style={{ margin: 0 }}>Payment</h2>
                <div style={{ display: "flex", gap: 10 }}>
                    {props?.onBack && (
                        <button className="btn" onClick={props.onBack} disabled={loading}>
                            ← Back
                        </button>
                    )}
                    <button className="btn" onClick={load} disabled={loading}>
                        Refresh
                    </button>
                </div>
            </div>

            {order && (
                <div style={{ marginTop: 12, background: "#fff", borderRadius: 14, padding: 14, boxShadow: "0 4px 12px rgba(0,0,0,0.08)" }}>
                    <div style={{ fontWeight: 800, color: "#023047" }}>Order #{order.orderId}</div>
                    <div style={{ display: "grid", gap: 6, marginTop: 10, fontSize: 13 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                            <span className="muted">Subtotal (backend charge)</span>
                            <span>{formatCents(orderSubtotalCents, "EUR")}</span>
                        </div>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                            <span className="muted">Estimated tax (UI-only)</span>
                            <span>{formatCents(estimatedTaxCents, "EUR")}</span>
                        </div>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                            <span className="muted">Discounts</span>
                            <span>{hasAnyDiscount ? "Applied" : "—"}</span>
                        </div>
                        {hasAnyDiscount && (
                            <div className="muted" style={{ fontSize: 12 }}>
                                Note: backend payment currently does not apply discounts/tax to the charged total.
                            </div>
                        )}
                    </div>
                </div>
            )}

            {error && (
                <div
                    style={{
                        background: "rgba(214, 40, 40, 0.1)",
                        border: "1px solid rgba(214, 40, 40, 0.3)",
                        color: "#b01d1d",
                        borderRadius: 12,
                        padding: "10px 12px",
                        marginTop: 10,
                        marginBottom: 10,
                    }}
                >
                    {error}
                </div>
            )}

            <div style={{ marginTop: 12, background: "#fff", borderRadius: 14, padding: 14, boxShadow: "0 4px 12px rgba(0,0,0,0.08)", display: "grid", gap: 10 }}>
                <div style={{ fontWeight: 800, color: "#023047" }}>Pay (Stripe + optional Gift Card)</div>

                <input
                    className="dropdown"
                    placeholder="Gift card code (optional)"
                    value={giftCardCode}
                    onChange={(e) => setGiftCardCode(e.target.value)}
                    disabled={loading}
                />

                <input
                    className="dropdown"
                    placeholder="Gift card amount (EUR, optional) — leave blank to use max"
                    inputMode="decimal"
                    value={giftCardAmount}
                    onChange={(e) => setGiftCardAmount(e.target.value)}
                    disabled={loading}
                />

                <div className="muted" style={{ fontSize: 12 }}>
                    Cash payments / split cash+giftcard are not available because the backend payment API currently supports only Stripe and GiftCard.
                </div>

                <button className="btn btn-primary" onClick={pay} disabled={loading}>
                    {loading ? "Processing…" : "Create Payment"}
                </button>
            </div>

            <div style={{ marginTop: 12, background: "#fff", borderRadius: 14, padding: 14, boxShadow: "0 4px 12px rgba(0,0,0,0.08)" }}>
                <div style={{ fontWeight: 800, color: "#023047" }}>Payments for this order</div>
                {history.length === 0 ? (
                    <div className="muted" style={{ marginTop: 8 }}>
                        No payments yet.
                    </div>
                ) : (
                    <div style={{ display: "grid", gap: 10, marginTop: 10 }}>
                        {history
                            .slice()
                            .sort((a, b) => b.paymentId - a.paymentId)
                            .map((p) => (
                                <div key={p.paymentId} style={{ border: "1px solid #eee", borderRadius: 12, padding: 10 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                        <div style={{ fontWeight: 700 }}>Payment #{p.paymentId}</div>
                                        <div className="muted">{p.status}</div>
                                    </div>
                                    <div className="muted" style={{ fontSize: 13, marginTop: 6 }}>
                                        {p.method} • {formatCents(p.amountCents, p.currency)} • giftcard planned {formatCents(p.giftCardPlannedCents, p.currency)}
                                    </div>
                                </div>
                            ))}
                    </div>
                )}
            </div>
        </div>
    );
}

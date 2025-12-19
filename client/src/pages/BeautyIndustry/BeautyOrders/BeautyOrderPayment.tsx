import { useEffect, useMemo, useState } from "react";
import "./BeautyOrderPayment.css";
import { createPayment, listPaymentsForOrder, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { getOrder, updateOrder, type OrderDetail } from "../../../frontapi/orderApi";
import { listDiscounts, type DiscountSummary } from "../../../frontapi/discountsApi";

type DiscountSnapshot = {
    version?: number;
    discountId?: number;
    code?: string;
    type?: string; // "Percent" | "Amount"
    scope?: string; // "Order" | "Line"
    value?: number;
    capturedAtUtc?: string;
};

function tryParseDiscountSnapshot(json: string | null | undefined): DiscountSnapshot | null {
    if (!json || !json.trim()) return null;
    try {
        const obj = JSON.parse(json);
        if (!obj || typeof obj !== "object") return null;
        return obj as DiscountSnapshot;
    } catch {
        return null;
    }
}

function clampNonNeg(n: number) {
    return Number.isFinite(n) && n > 0 ? n : 0;
}

function toCents(amount: number) {
    return Math.round((Number(amount) || 0) * 100);
}

function roundDiv(numerator: number, denominator: number) {
    if (!denominator) return 0;
    return Math.round(numerator / denominator);
}

function applyDiscountToCents(amountCents: number, snap: DiscountSnapshot | null): number {
    const amount = Number(amountCents) || 0;
    if (!snap) return amount;
    const type = String(snap.type ?? "").trim();
    const value = Number(snap.value) || 0;
    if (type === "Percent" && value > 0) {
        return Math.round(amount * (1 - value / 100));
    }
    if (type === "Amount" && value > 0) {
        return amount - Math.round(value * 100);
    }
    return amount;
}

type LinePricing = {
    orderLineId: number;
    name: string;
    qty: number;
    unitPriceCents: number;
    grossCents: number;
    lineDiscountCents: number;
    netAfterItemDiscountCents: number;
    taxCents: number;
    totalCents: number;
    lineDiscountCode?: string | null;
};

function calculatePricing(order: OrderDetail | null) {
    if (!order) {
        return {
            orderDiscountSnap: null as DiscountSnapshot | null,
            lines: [] as LinePricing[],
            totals: {
                grossSubtotalCents: 0,
                lineDiscountTotalCents: 0,
                orderDiscountTotalCents: 0,
                netSubtotalCents: 0,
                taxTotalCents: 0,
                grandTotalCents: 0,
            },
        };
    }

    const orderDiscountSnap = tryParseDiscountSnapshot(order.orderDiscountSnapshot);

    const baseLines = order.lines
        .filter((l) => (Number(l.qty) || 0) > 0 && (Number(l.unitPriceSnapshot) || 0) >= 0)
        .map((l) => {
            const qty = Number(l.qty) || 0;
            const unitPriceCents = toCents(Number(l.unitPriceSnapshot) || 0);
            const grossCents = Math.round(unitPriceCents * qty);

            const lineSnap = tryParseDiscountSnapshot(l.unitDiscountSnapshot);
            let afterLine = applyDiscountToCents(grossCents, lineSnap);
            if (afterLine < 0) afterLine = 0;

            const lineDiscountCents = Math.max(0, grossCents - afterLine);

            return {
                raw: l,
                qty,
                unitPriceCents,
                grossCents,
                netAfterLineCents: afterLine,
                lineDiscountCents,
                lineDiscountCode: (lineSnap?.code ?? null) as string | null,
                taxRatePct: clampNonNeg(Number(l.taxRateSnapshotPct) || 0),
            };
        });

    const netSubtotalAfterLine = baseLines.reduce((s, x) => s + x.netAfterLineCents, 0);

    const lines: LinePricing[] = baseLines.map((x) => {
        // IMPORTANT: item cards should NOT be reduced by order-level discount.
        // They show only item-level discount + tax (based on item-discounted net).
        const netAfterItemDiscount = Math.max(0, x.netAfterLineCents);
        const taxCents = x.taxRatePct * 100;
        const totalCents = netAfterItemDiscount + taxCents;

        return {
            orderLineId: x.raw.orderLineId,
            name: x.raw.itemNameSnapshot,
            qty: x.qty,
            unitPriceCents: x.unitPriceCents,
            grossCents: x.grossCents,
            lineDiscountCents: x.lineDiscountCents,
            netAfterItemDiscountCents: netAfterItemDiscount,
            taxCents,
            totalCents,
            lineDiscountCode: x.lineDiscountCode,
        };
    });

    const grossSubtotalCents = lines.reduce((s, l) => s + l.grossCents, 0);
    const lineDiscountTotalCents = lines.reduce((s, l) => s + l.lineDiscountCents, 0);

    // Summary totals: apply order-level discount at the order level (without changing item cards).
    // We still compute tax AFTER order discount to match backend total behavior as closely as possible.
    const orderType = String(orderDiscountSnap?.type ?? "").trim();
    const orderValue = Number(orderDiscountSnap?.value) || 0;

    // Recompute "after order discount" by distributing across lines (for tax correctness), but do not show it per line.
    const discountedNets: number[] = new Array(baseLines.length).fill(0);

    if (baseLines.length > 0 && netSubtotalAfterLine > 0) {
        if (orderType === "Percent" && orderValue > 0) {
            const factor = Math.max(0, 1 - orderValue / 100);
            for (let i = 0; i < baseLines.length; i++) discountedNets[i] = Math.round(baseLines[i].netAfterLineCents * factor);
        } else if (orderType === "Amount" && orderValue > 0) {
            const orderValueCents = Math.round(orderValue * 100);
            let allocated = 0;
            for (let i = 0; i < baseLines.length; i++) {
                const before = baseLines[i].netAfterLineCents;
                const share = Math.min(before, roundDiv(orderValueCents * before, netSubtotalAfterLine));
                discountedNets[i] = Math.max(0, before - share);
                allocated += share;
            }
            const remainder = orderValueCents - allocated;
            if (baseLines.length > 0 && remainder !== 0) {
                const last = baseLines.length - 1;
                discountedNets[last] = Math.max(0, discountedNets[last] - remainder);
            }
        } else {
            for (let i = 0; i < baseLines.length; i++) discountedNets[i] = baseLines[i].netAfterLineCents;
        }
    }

    let finalNetAfterOrderCents = 0;
    let taxTotalAfterOrderCents = 0;

    for (let i = 0; i < baseLines.length; i++) {
        const net = Math.max(0, discountedNets[i] || 0);
        finalNetAfterOrderCents += net;
        taxTotalAfterOrderCents += Math.round((net * baseLines[i].taxRatePct) / 100);
    }

    const orderDiscountTotalCents = Math.max(0, netSubtotalAfterLine - finalNetAfterOrderCents);
    const netSubtotalCents = finalNetAfterOrderCents;
    const taxTotalCents = taxTotalAfterOrderCents;
    const grandTotalCents = netSubtotalCents + taxTotalCents;

    return {
        orderDiscountSnap,
        lines,
        totals: {
            grossSubtotalCents,
            lineDiscountTotalCents,
            orderDiscountTotalCents,
            netSubtotalCents,
            taxTotalCents,
            grandTotalCents,
        },
    };
}

export default function BeautyOrderPayment(props?: {
    orderId: number;
    onBack?: () => void;
}) {
    const orderIdFromProps = props?.orderId;
    const params = new URLSearchParams(window.location.search);
    const orderIdFromQuery = Number(params.get("orderId"));
    const orderId = Number(orderIdFromProps ?? orderIdFromQuery);

    const [discountCode, setDiscountCode] = useState("");
    const [discountBusy, setDiscountBusy] = useState(false);
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

    const pricing = useMemo(() => calculatePricing(order), [order]);

    const applyOrderDiscountCode = async () => {
        if (!order) return;
        const code = discountCode.trim();
        if (!code) {
            setError("Please enter a discount code.");
            return;
        }

        try {
            setDiscountBusy(true);
            setError(null);

            const discounts = await listDiscounts();
            const match = (discounts || []).find((d: DiscountSummary) => String(d.code).toLowerCase() === code.toLowerCase());

            if (!match) {
                setError("Discount code not found.");
                return;
            }
            if (String(match.scope).toLowerCase() !== "order") {
                setError("This discount code is not an order-level discount.");
                return;
            }
            if (String(match.status).toLowerCase() !== "active") {
                setError("This discount code is not active.");
                return;
            }

            const updated = await updateOrder(order.orderId, {
                employeeId: order.employeeId,
                discountId: match.discountId,
            });
            setOrder(updated);
        } catch (e: any) {
            setError(e?.message || "Failed to apply discount code.");
        } finally {
            setDiscountBusy(false);
        }
    };

    const clearOrderDiscount = async () => {
        if (!order) return;
        try {
            setDiscountBusy(true);
            setError(null);
            const updated = await updateOrder(order.orderId, { employeeId: order.employeeId, discountId: null });
            setOrder(updated);
            setDiscountCode("");
        } catch (e: any) {
            setError(e?.message || "Failed to clear discount.");
        } finally {
            setDiscountBusy(false);
        }
    };

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
                <div
                    style={{
                        marginTop: 12,
                        alignItems: "start",
                    }}
                    className="beauty-checkout-grid"
                >
                    {/* LEFT: Items */}
                    <div className="beauty-card">
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "baseline" }}>
                            <div style={{ fontWeight: 800, color: "#023047" }}>Order #{order.orderId}</div>
                            <div className="muted" style={{ fontSize: 12 }}>
                                {pricing.lines.length} item{pricing.lines.length === 1 ? "" : "s"}
                            </div>
                        </div>

                        <div style={{ marginTop: 12, display: "grid", gap: 10 }}>
                            {pricing.lines.length === 0 ? (
                                <div className="muted">No items in this order.</div>
                            ) : (
                                pricing.lines.map((l) => (
                                    <div key={l.orderLineId} className="beauty-checkout-item">
                                        <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                            <div style={{ fontWeight: 800 }}>{l.name}</div>
                                            <div style={{ fontWeight: 800 }}>{formatCents(l.totalCents, "EUR")}</div>
                                        </div>

                                        <div className="muted" style={{ marginTop: 4, fontSize: 12 }}>
                                            Qty {l.qty} • Unit {formatCents(l.unitPriceCents, "EUR")}
                                        </div>

                                        <div style={{ display: "grid", gap: 4, marginTop: 8, fontSize: 13 }}>
                                            <div className="beauty-checkout-line">
                                                <span className="muted">Line price</span>
                                                <span>{formatCents(l.grossCents, "EUR")}</span>
                                            </div>

                                            <div className="beauty-checkout-line">
                                                <span className="muted">Item discount{l.lineDiscountCode ? ` (${l.lineDiscountCode})` : ""}</span>
                                                <span>{l.lineDiscountCents ? `- ${formatCents(l.lineDiscountCents, "EUR")}` : "—"}</span>
                                            </div>

                                            <div className="beauty-checkout-line">
                                                <span className="muted">Tax</span>
                                                <span>{l.taxCents ? formatCents(l.taxCents, "%") : "—"}</span>
                                            </div>
                                        </div>
                                    </div>
                                ))
                            )}
                        </div>
                    </div>

                    {/* RIGHT: Summary + Discounts + Giftcard + Pay */}
                    <div className="beauty-checkout-right">
                        <div className="beauty-card">
                            <div style={{ fontWeight: 800, color: "#023047" }}>Summary</div>
                            <div style={{ display: "grid", gap: 6, marginTop: 10, fontSize: 13 }}>
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                    <span className="muted">Subtotal</span>
                                    <span>{formatCents(pricing.totals.grossSubtotalCents, "EUR")}</span>
                                </div>
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                    <span className="muted">Item discounts</span>
                                    <span>{pricing.totals.lineDiscountTotalCents ? `- ${formatCents(pricing.totals.lineDiscountTotalCents, "EUR")}` : "—"}</span>
                                </div>
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                    <span className="muted">Order discount</span>
                                    <span>{pricing.totals.orderDiscountTotalCents ? `- ${formatCents(pricing.totals.orderDiscountTotalCents, "EUR")}` : "—"}</span>
                                </div>
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                    <span className="muted">Tax</span>
                                    <span>{pricing.totals.taxTotalCents ? formatCents(pricing.totals.taxTotalCents, "EUR") : "—"}</span>
                                </div>
                                <div className="beauty-checkout-sep" />
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10, fontSize: 15 }}>
                                    <span style={{ fontWeight: 900 }}>Total</span>
                                    <span style={{ fontWeight: 900 }}>{formatCents(pricing.totals.grandTotalCents, "EUR")}</span>
                                </div>
                            </div>

                            {pricing.totals.orderDiscountTotalCents > 0 && (
                                <div className="muted" style={{ marginTop: 10, fontSize: 12 }}>
                                    Order discount is applied in the total; item cards show only item discounts.
                                </div>
                            )}
                        </div>

                        <div className="beauty-card" style={{ display: "grid", gap: 10 }}>
                            <div style={{ fontWeight: 800, color: "#023047" }}>Discount code</div>

                            <div style={{ display: "flex", gap: 8 }}>
                                <input
                                    className="dropdown beauty-checkout-input"
                                    placeholder="Type discount code (order-level)"
                                    value={discountCode}
                                    onChange={(e) => setDiscountCode(e.target.value)}
                                    disabled={loading || discountBusy}
                                    style={{ flex: 1 }}
                                />
                                <button className="btn btn-primary" onClick={applyOrderDiscountCode} disabled={loading || discountBusy}>
                                    {discountBusy ? "Applying…" : "Apply"}
                                </button>
                            </div>

                            {pricing.orderDiscountSnap?.code ? (
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10, fontSize: 13 }}>
                                    <div className="muted">
                                        Applied: <b>{pricing.orderDiscountSnap.code}</b> ({pricing.orderDiscountSnap.type} {pricing.orderDiscountSnap.value})
                                    </div>
                                    <button className="btn" onClick={clearOrderDiscount} disabled={loading || discountBusy}>
                                        Clear
                                    </button>
                                </div>
                            ) : (
                                <div className="muted" style={{ fontSize: 12 }}>
                                    No order discount applied.
                                </div>
                            )}
                        </div>

                        <div className="beauty-card" style={{ display: "grid", gap: 10 }}>
                            <div style={{ fontWeight: 800, color: "#023047" }}>Pay (Stripe + optional Gift Card)</div>

                            <input
                                className="dropdown beauty-checkout-input"
                                placeholder="Giftcard (optional)"
                                value={giftCardCode}
                                onChange={(e) => setGiftCardCode(e.target.value)}
                                disabled={loading}
                            />

                            <input
                                className="dropdown beauty-checkout-input"
                                placeholder="Giftcard amount (EUR, optional) — leave blank to use max"
                                inputMode="decimal"
                                value={giftCardAmount}
                                onChange={(e) => setGiftCardAmount(e.target.value)}
                                disabled={loading}
                            />

                            <button className="btn btn-primary" onClick={pay} disabled={loading}>
                                {loading ? "Processing…" : "Create Payment"}
                            </button>
                        </div>
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

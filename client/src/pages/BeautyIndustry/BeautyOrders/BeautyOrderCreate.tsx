import { useEffect, useState } from "react";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { addOrderLine, createOrder } from "../../../frontapi/orderApi";

import "../../../App.css";
import "./BeautyOrderCreate.css";

interface OrderLine {
    serviceId: number;
    name: string;
    price: number;
    quantity: number;
}

interface BeautyOrderCreateProps {
    goBack: () => void;
    onCreated: (orderId: number) => void;
}

export default function BeautyOrderCreate({
    goBack,
    onCreated,
}: BeautyOrderCreateProps) {
    const businessId = Number(localStorage.getItem("businessId"));
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [lines, setLines] = useState<OrderLine[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    /* ---------------- LOAD SERVICES ---------------- */
    useEffect(() => {
        if (!businessId) return;

        getActiveServices(businessId)
            .then(setServices)
            .catch((e) => {
                console.error(e);
                setError(e?.message || "Failed to load services");
            })
            .finally(() => setLoading(false));
    }, [businessId]);

    /* ---------------- ADD SERVICE ---------------- */
    const addService = (service: CatalogItem) => {
        setLines(prev => {
            const existing = prev.find(
                l => l.serviceId === service.catalogItemId
            );

            if (existing) {
                return prev.map(l =>
                    l.serviceId === service.catalogItemId
                        ? { ...l, quantity: l.quantity + 1 }
                        : l
                );
            }

            return [
                ...prev,
                {
                    serviceId: service.catalogItemId,
                    name: service.name,
                    price: service.basePrice,
                    quantity: 1,
                },
            ];
        });
    };

    /* ---------------- QUANTITY ---------------- */
    const increaseQty = (id: number) => {
        setLines(prev =>
            prev.map(l =>
                l.serviceId === id
                    ? { ...l, quantity: l.quantity + 1 }
                    : l
            )
        );
    };

    const decreaseQty = (id: number) => {
        setLines(prev =>
            prev
                .map(l =>
                    l.serviceId === id
                        ? { ...l, quantity: l.quantity - 1 }
                        : l
                )
                .filter(l => l.quantity > 0)
        );
    };

    /* ---------------- REMOVE LINE (✕) ---------------- */
    const removeLine = (id: number) => {
        setLines(prev => prev.filter(l => l.serviceId !== id));
    };

    const total = lines.reduce(
        (sum, l) => sum + l.price * l.quantity,
        0
    );

    /* ---------------- PROCEED ---------------- */
    const proceed = async () => {
        if (!employeeId || lines.length === 0 || saving) return;

        try {
            setSaving(true);
            setError(null);

            const order = await createOrder(employeeId);
            const orderId = Number(order?.orderId);
            if (!orderId) throw new Error("Backend returned invalid orderId");

            const results = await Promise.allSettled(
                lines.map((l) => addOrderLine(orderId, l.serviceId, l.quantity))
            );

            const failed = results.filter((r) => r.status === "rejected");
            if (failed.length > 0) {
                setError(
                    `Order created, but ${failed.length} line(s) failed to add. Open the order to review and retry.`
                );
            }

            onCreated(orderId);
        } catch (err: any) {
            console.error(err);
            setError(err?.message || "Failed to create order");
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return <div className="page">Loading services…</div>;
    }

    return (
        <div className="page order-create">
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12 }}>
                <h2 style={{ margin: 0 }}>New Walk-in Order</h2>
                <button className="btn" onClick={goBack} disabled={saving}>
                    ← Back
                </button>
            </div>

            {error && (
                <div
                    style={{
                        background: "rgba(214, 40, 40, 0.1)",
                        border: "1px solid rgba(214, 40, 40, 0.3)",
                        color: "#b01d1d",
                        borderRadius: 12,
                        padding: "10px 12px",
                    }}
                >
                    {error}
                </div>
            )}

            {/* SERVICES */}
            <div className="service-grid">
                {services.map(service => (
                    <button
                        key={service.catalogItemId}
                        className="service-card"
                        onClick={() => addService(service)}
                    >
                        <div className="service-name">{service.name}</div>
                        <div className="service-price">
                            €{service.basePrice.toFixed(2)}
                        </div>
                    </button>
                ))}
            </div>

            {/* ORDER SUMMARY */}
            <div className="order-summary">
                <h3>Order</h3>

                {lines.length === 0 && (
                    <div className="muted">No services added</div>
                )}

                {lines.map(line => (
                    <div key={line.serviceId} className="order-line">
                        <span className="order-line-name">
                            {line.name}
                        </span>

                        <div className="order-line-right">
                            <button
                                onClick={() =>
                                    decreaseQty(line.serviceId)
                                }
                                disabled={line.quantity === 1}
                            >
                                −
                            </button>

                            <span>{line.quantity}</span>

                            <button
                                onClick={() =>
                                    increaseQty(line.serviceId)
                                }
                            >
                                +
                            </button>

                            <span className="order-line-price">
                                €{(line.price * line.quantity).toFixed(2)}
                            </span>

                            {/* ✕ REMOVE */}
                            <button
                                className="remove-line-btn"
                                onClick={() =>
                                    removeLine(line.serviceId)
                                }
                                title="Remove service"
                            >
                                ✕
                            </button>
                        </div>
                    </div>
                ))}

                <div className="order-total">
                    <strong>Total</strong>
                    <strong>€{total.toFixed(2)}</strong>
                </div>

                <button
                    className="btn btn-primary"
                    disabled={lines.length === 0 || saving}
                    onClick={proceed}
                >
                    {saving ? "Creating order…" : "Create order"}
                </button>
            </div>
        </div>
    );
}

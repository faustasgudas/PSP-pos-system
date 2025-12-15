import { useEffect, useState } from "react";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { createOrder } from "../../../frontapi/orderApi";

import "../../../App.css";
import "./BeautyOrderCreate.css";

interface OrderLine {
    serviceId: number;
    name: string;
    price: number;
    quantity: number;
}

interface BeautyOrderCreateProps {
    onProceedToPayment: (orderId: number) => void;
}

export default function BeautyOrderCreate({
                                              onProceedToPayment,
                                          }: BeautyOrderCreateProps) {
    const businessId = Number(localStorage.getItem("businessId"));
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [lines, setLines] = useState<OrderLine[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    /* ---------------- LOAD SERVICES ---------------- */
    useEffect(() => {
        if (!businessId) return;

        getActiveServices(businessId)
            .then(setServices)
            .catch(console.error)
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

            const order = await createOrder(employeeId);

            sessionStorage.setItem(
                "selectedServices",
                JSON.stringify(lines)
            );

            onProceedToPayment(order.orderId);
        } catch (err) {
            console.error(err);
            alert("Failed to create order");
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return <div className="page">Loading services…</div>;
    }

    return (
        <div className="page order-create">
            <h2>New Walk-in Order</h2>

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
                    {saving ? "Creating order…" : "Proceed to Payment"}
                </button>
            </div>
        </div>
    );
}

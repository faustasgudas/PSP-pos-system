import { useEffect, useState } from "react";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { addOrderLine, createOrder } from "../../../frontapi/orderApi";
import { listReservations, type ReservationSummary } from "../../../frontapi/reservationsApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";

import "../../../App.css";
import "./BeautyOrderCreate.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

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

    const [reservationsLoading, setReservationsLoading] = useState(true);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);
    const [selectedReservationId, setSelectedReservationId] = useState<string>("");

    const reservationSelectOptions = useMemo(() => {
        if (reservationsLoading) {
            return [{ value: "", label: "Loading reservations…", disabled: true }];
        }
        const opts = reservations
            .slice()
            .sort((a, b) => new Date(a.appointmentStart).getTime() - new Date(b.appointmentStart).getTime())
            .map((r) => {
                const svcName =
                    services.find((s) => s.catalogItemId === r.catalogItemId)?.name ??
                    `Service ${r.catalogItemId}`;
                const empName =
                    employees.find((e: any) => Number(e.employeeId ?? e.id) === r.employeeId)?.name ??
                    `Employee ${r.employeeId}`;
                const when = new Date(r.appointmentStart).toLocaleString();
                return {
                    value: String(r.reservationId),
                    label: when,
                    subLabel: `${svcName} • ${empName}`,
                };
            });
        return [{ value: "", label: "No reservation (walk-in)", subLabel: "Optional link" }, ...opts];
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [reservationsLoading, reservations, services, employees]);

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

    useEffect(() => {
        if (!businessId) return;

        setReservationsLoading(true);
        setError(null);

        const now = new Date();
        const from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
        const to = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);

        Promise.all([
            listReservations(businessId, {
                status: "Booked",
                dateFrom: from.toISOString(),
                dateTo: to.toISOString(),
            }),
            fetchEmployees(businessId),
        ])
            .then(([resList, empList]) => {
                setReservations(Array.isArray(resList) ? resList : []);
                setEmployees(Array.isArray(empList) ? empList : []);
            })
            .catch((e) => {
                console.error(e);
                setReservations([]);
                // keep UI usable for walk-in orders even if reservations fail
            })
            .finally(() => setReservationsLoading(false));
    }, [businessId]);

    // If user picks a reservation, prefill its service line (qty=1) if not present yet.
    useEffect(() => {
        const rid = selectedReservationId ? Number(selectedReservationId) : null;
        if (!rid) return;

        const r = reservations.find((x) => x.reservationId === rid);
        if (!r) return;

        const svc = services.find((s) => s.catalogItemId === r.catalogItemId);
        if (!svc) return;

        setLines((prev) => {
            const existing = prev.find((l) => l.serviceId === svc.catalogItemId);
            if (existing) return prev;
            return [
                {
                    serviceId: svc.catalogItemId,
                    name: svc.name,
                    price: svc.basePrice,
                    quantity: 1,
                },
                ...prev,
            ];
        });
    }, [selectedReservationId, reservations, services]);

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

            const rid = selectedReservationId ? Number(selectedReservationId) : null;
            if (selectedReservationId && (!Number.isFinite(rid) || !rid || rid <= 0)) {
                throw new Error("Invalid reservation selection");
            }

            const order = await createOrder(employeeId, { reservationId: rid });
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
                <h2 style={{ margin: 0 }}>New Order</h2>
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

            <div style={{ display: "flex", gap: 12, alignItems: "center", flexWrap: "wrap" }}>
                <div style={{ width: "min(520px, 100%)" }} title="Optional: link order to an existing reservation">
                    <BeautySelect
                        value={selectedReservationId}
                        onChange={setSelectedReservationId}
                        disabled={saving || reservationsLoading}
                        placeholder="No reservation (walk-in)"
                        options={reservationSelectOptions}
                    />
                </div>

                <div className="muted">
                    Pick a reservation to link it; we’ll auto-add the reserved service (qty 1).
                </div>
            </div>

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

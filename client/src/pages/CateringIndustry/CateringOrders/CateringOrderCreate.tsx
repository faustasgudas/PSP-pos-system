import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import { addOrderLine, createOrder } from "../../../frontapi/orderApi";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import { listReservations, type ReservationSummary } from "../../../frontapi/reservationsApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";

type DraftLine = {
    catalogItemId: number;
    name: string;
    price: number;
    qty: number;
};

export default function CateringOrderCreate(props: {
    goBack: () => void;
    onCreated: (orderId: number) => void;
}) {
    const businessId = Number(localStorage.getItem("businessId"));
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [items, setItems] = useState<CatalogItem[]>([]);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);

    const [selectedReservationId, setSelectedReservationId] = useState<string>("");
    const [query, setQuery] = useState("");
    const [lines, setLines] = useState<DraftLine[]>([]);

    useEffect(() => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);

        const now = new Date();
        const from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
        const to = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);

        Promise.all([
            listCatalogItems(businessId, { status: "Active" }),
            listReservations(businessId, { status: "Booked", dateFrom: from.toISOString(), dateTo: to.toISOString() }).catch(() => []),
            fetchEmployees(businessId).catch(() => []),
        ])
            .then(([catalog, resv, emps]) => {
                setItems(Array.isArray(catalog) ? catalog : []);
                setReservations(Array.isArray(resv) ? resv : []);
                setEmployees(Array.isArray(emps) ? emps : []);
            })
            .catch((e: any) => setError(e?.message || "Failed to load order data"))
            .finally(() => setLoading(false));
    }, [businessId]);

    const reservationOptions = useMemo(() => {
        const byEmp = new Map<number, string>();
        employees.forEach((e: any) => byEmp.set(Number(e.employeeId ?? e.id), e.name));

        const byItem = new Map<number, string>();
        items.forEach((i) => byItem.set(i.catalogItemId, i.name));

        return reservations
            .slice()
            .sort((a, b) => new Date(a.appointmentStart).getTime() - new Date(b.appointmentStart).getTime())
            .map((r) => ({
                id: r.reservationId,
                label: `${new Date(r.appointmentStart).toLocaleString()} — ${byItem.get(r.catalogItemId) ?? `Item ${r.catalogItemId}`} (${byEmp.get(r.employeeId) ?? `Employee ${r.employeeId}`})`,
                catalogItemId: r.catalogItemId,
            }));
    }, [reservations, employees, items]);

    useEffect(() => {
        const rid = selectedReservationId ? Number(selectedReservationId) : null;
        if (!rid) return;

        const opt = reservationOptions.find((o) => o.id === rid);
        if (!opt) return;

        const item = items.find((i) => i.catalogItemId === opt.catalogItemId);
        if (!item) return;

        setLines((prev) => {
            if (prev.some((l) => l.catalogItemId === item.catalogItemId)) return prev;
            return [
                {
                    catalogItemId: item.catalogItemId,
                    name: item.name,
                    price: Number(item.basePrice) || 0,
                    qty: 1,
                },
                ...prev,
            ];
        });
    }, [selectedReservationId, reservationOptions, items]);

    const filteredItems = useMemo(() => {
        const q = query.trim().toLowerCase();
        let list = items.filter((i) => String(i.status).toLowerCase() === "active");
        if (q) list = list.filter((i) => i.name.toLowerCase().includes(q) || i.code.toLowerCase().includes(q));
        return list.sort((a, b) => String(a.type).localeCompare(String(b.type)) || a.name.localeCompare(b.name));
    }, [items, query]);

    const addItem = (item: CatalogItem) => {
        setLines((prev) => {
            const existing = prev.find((l) => l.catalogItemId === item.catalogItemId);
            if (existing) return prev.map((l) => (l.catalogItemId === item.catalogItemId ? { ...l, qty: l.qty + 1 } : l));
            return [...prev, { catalogItemId: item.catalogItemId, name: item.name, price: Number(item.basePrice) || 0, qty: 1 }];
        });
    };

    const inc = (id: number) => setLines((prev) => prev.map((l) => (l.catalogItemId === id ? { ...l, qty: l.qty + 1 } : l)));
    const dec = (id: number) =>
        setLines((prev) =>
            prev
                .map((l) => (l.catalogItemId === id ? { ...l, qty: l.qty - 1 } : l))
                .filter((l) => l.qty > 0)
        );
    const remove = (id: number) => setLines((prev) => prev.filter((l) => l.catalogItemId !== id));

    const total = useMemo(() => lines.reduce((sum, l) => sum + l.price * l.qty, 0), [lines]);

    const create = async () => {
        if (!employeeId) return setError("Missing employeeId");
        if (lines.length === 0) return setError("Add at least one item");
        if (saving) return;

        const rid = selectedReservationId ? Number(selectedReservationId) : null;
        if (selectedReservationId && (!Number.isFinite(rid) || !rid || rid <= 0)) {
            setError("Invalid reservation selection");
            return;
        }

        setSaving(true);
        setError(null);
        try {
            const order = await createOrder(employeeId, { reservationId: rid });
            const orderId = Number(order?.orderId);
            if (!orderId) throw new Error("Backend returned invalid orderId");

            const results = await Promise.allSettled(lines.map((l) => addOrderLine(orderId, l.catalogItemId, l.qty)));
            const failed = results.filter((r) => r.status === "rejected");
            if (failed.length > 0) {
                setError(`Order created, but ${failed.length} line(s) failed to add. Open the order to review.`);
            }

            props.onCreated(orderId);
        } catch (e: any) {
            setError(e?.message || "Failed to create order");
        } finally {
            setSaving(false);
        }
    };

    if (loading) return <div className="page"><div className="card">Loading…</div></div>;

    return (
        <div className="page">
            <div className="action-bar">
                <h2 className="section-title">New Order</h2>
                <button className="btn" onClick={props.goBack} disabled={saving}>
                    ← Back
                </button>
            </div>

            {error && (
                <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                    {error}
                </div>
            )}

            <div className="card" style={{ marginBottom: 12 }}>
                <div className="muted" style={{ marginBottom: 6 }}>Reservation (optional)</div>
                <select className="dropdown" value={selectedReservationId} onChange={(e) => setSelectedReservationId(e.target.value)} disabled={saving}>
                    <option value="">No reservation (walk-in)</option>
                    {reservationOptions.map((o) => (
                        <option key={o.id} value={o.id}>
                            {o.label}
                        </option>
                    ))}
                </select>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 12 }}>
                <div className="card">
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                        <h3 style={{ margin: 0 }}>Catalog items</h3>
                        <input className="dropdown" value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search name/code…" style={{ maxWidth: 260 }} />
                    </div>
                    <div style={{ marginTop: 12, display: "grid", gap: 8 }}>
                        {filteredItems.slice(0, 60).map((i) => (
                            <button key={i.catalogItemId} className="btn" onClick={() => addItem(i)} disabled={saving} style={{ textAlign: "left" }}>
                                <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                                    <div>
                                        <strong>{i.name}</strong> <span className="muted">({i.type})</span>
                                    </div>
                                    <div className="muted">€{Number(i.basePrice).toFixed(2)}</div>
                                </div>
                            </button>
                        ))}
                        {filteredItems.length === 0 && <div className="muted">No items found.</div>}
                    </div>
                </div>

                <div className="card">
                    <h3 style={{ marginTop: 0 }}>Order</h3>
                    {lines.length === 0 ? (
                        <div className="muted">No items added.</div>
                    ) : (
                        <div style={{ display: "grid", gap: 10 }}>
                            {lines.map((l) => (
                                <div key={l.catalogItemId} style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                    <div style={{ minWidth: 180 }}>
                                        <strong>{l.name}</strong>
                                        <div className="muted">€{l.price.toFixed(2)} each</div>
                                    </div>
                                    <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                                        <button className="btn" onClick={() => dec(l.catalogItemId)} disabled={saving}>−</button>
                                        <div className="card" style={{ padding: "8px 10px" }}>{l.qty}</div>
                                        <button className="btn" onClick={() => inc(l.catalogItemId)} disabled={saving}>+</button>
                                        <div style={{ width: 90, textAlign: "right", fontWeight: 800 }}>€{(l.price * l.qty).toFixed(2)}</div>
                                        <button className="btn btn-danger" onClick={() => remove(l.catalogItemId)} disabled={saving}>✕</button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    <div style={{ marginTop: 14, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                        <strong>Total</strong>
                        <strong>€{total.toFixed(2)}</strong>
                    </div>

                    <button className="btn btn-primary" onClick={create} disabled={saving} style={{ marginTop: 12, width: "100%" }}>
                        {saving ? "Creating…" : "Create order"}
                    </button>
                </div>
            </div>
        </div>
    );
}



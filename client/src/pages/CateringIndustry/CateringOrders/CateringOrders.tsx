import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "../../BeautyIndustry/BeautyOrders/BeautyOrders.css";
import { getUserFromToken } from "../../../utils/auth";
import { listAllOrders, listMyOrders, type OrderSummary } from "../../../frontapi/orderApi";
import { BeautySelect } from "../../../components/ui/BeautySelect";
import { fetchEmployees } from "../../../frontapi/employeesApi";

type Scope = "mine" | "all";

export default function CateringOrders(props: {
    onNewOrder: () => void;
    onOpenOrder: (orderId: number) => void;
}) {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const employeeId = Number(localStorage.getItem("employeeId"));
    const businessId = Number(localStorage.getItem("businessId"));

    const canListAll = role === "Owner" || role === "Manager";

    const [scope, setScope] = useState<Scope>("mine");
    const [status, setStatus] = useState<string>("");
    const [openId, setOpenId] = useState<string>("");

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [orders, setOrders] = useState<OrderSummary[]>([]);

    const [employees, setEmployees] = useState<any[]>([]);

    useEffect(() => {
        if (!businessId) {
            setEmployees([]);
            return;
        }
        fetchEmployees(businessId)
            .then((emps) => setEmployees(Array.isArray(emps) ? emps : []))
            .catch(() => setEmployees([]));
    }, [businessId]);

    const employeeEmailById = useMemo(() => {
        const m = new Map<number, string>();
        employees.forEach((e: any) => {
            const id = Number(e.employeeId ?? e.id);
            if (!id) return;
            const email = String(e.email ?? e.userEmail ?? "").trim();
            if (email) m.set(id, email);
        });
        return m;
    }, [employees]);

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            let data: OrderSummary[] = [];

            if (scope === "all") {
                data = await listAllOrders({ status: status || undefined });
            } else {
                // backend /orders/mine returns Open-only; emulate for managers/owners when filtering other statuses
                if (canListAll && employeeId && status) {
                    const all = await listAllOrders({ status });
                    data = (Array.isArray(all) ? all : []).filter((o) => o.employeeId === employeeId);
                } else {
                    data = await listMyOrders();
                }
            }

            setOrders(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setOrders([]);
            setError(e?.message || "Failed to load orders");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (!canListAll && scope === "all") setScope("mine");
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [canListAll]);

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [scope, status]);

    const filtered = useMemo(() => {
        const sf = status ? status.toLowerCase() : "";
        let data = orders;
        if (sf) data = data.filter((o) => String(o.status).toLowerCase() === sf);
        return [...data].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    }, [orders, status]);

    const openById = () => {
        const id = Number(openId);
        if (!Number.isFinite(id) || id <= 0) {
            setError("Enter a valid Order ID");
            return;
        }
        props.onOpenOrder(id);
    };

    return (
        <div className="page">
            <div className="action-bar">
                <h2 className="section-title">Orders</h2>
                <div className="action-buttons">
                    <button className="btn" onClick={load} disabled={loading}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                    <button className="btn btn-primary" onClick={props.onNewOrder}>
                        ➕ New Order
                    </button>
                </div>
            </div>

            <div className="card" style={{ marginBottom: 12 }}>
                <div style={{ display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
                    <div className="muted">Scope</div>
                    <button className={`btn ${scope === "mine" ? "btn-primary" : ""}`} onClick={() => setScope("mine")} disabled={loading}>
                        My orders
                    </button>
                    <button
                        className={`btn ${scope === "all" ? "btn-primary" : ""}`}
                        onClick={() => setScope("all")}
                        disabled={loading || !canListAll}
                        title={!canListAll ? "Manager/Owner only" : ""}
                    >
                        All orders
                    </button>

                    <div style={{ width: 16 }} />
                    <div className="muted">Status</div>
                    <div style={{ width: 220 }}>
                        <BeautySelect
                            value={status}
                            onChange={setStatus}
                            disabled={loading}
                            placeholder="All"
                            options={[
                                { value: "", label: "All" },
                                { value: "Open", label: "Open" },
                                { value: "Closed", label: "Closed" },
                                { value: "Cancelled", label: "Cancelled" },
                            ]}
                        />
                    </div>

                    <div style={{ width: 16 }} />
                    <div className="muted">Open</div>
                    <input
                        className="dropdown"
                        value={openId}
                        onChange={(e) => setOpenId(e.target.value)}
                        placeholder="Order ID (e.g. 123)"
                        style={{ maxWidth: 200 }}
                    />
                    <button className="btn" onClick={openById}>
                        Open
                    </button>
                </div>
                {scope === "mine" && !canListAll && (
                    <div className="muted" style={{ marginTop: 8 }}>
                        Note: backend “My orders” returns only <strong>Open</strong> orders for staff.
                    </div>
                )}
            </div>

            {error && (
                <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                    {error}
                </div>
            )}

            {loading ? (
                <div className="card">Loading…</div>
            ) : filtered.length === 0 ? (
                <div className="card">No orders found.</div>
            ) : (
                <div className="orders-table-wrap">
                    <table className="orders-table">
                        <thead>
                        <tr>
                            <th>Order</th>
                            <th>Created</th>
                            <th>Table</th>
                            <th>Reservation</th>
                            <th>Employee</th>
                            <th>Status</th>
                            <th className="right">Actions</th>
                        </tr>
                        </thead>

                        <tbody>
                        {filtered.map((o) => (
                            <tr
                                key={o.orderId}
                                className="orders-row"
                                onClick={() => props.onOpenOrder(o.orderId)}
                            >
                                <td className="order-id">#{o.orderId}</td>
                                <td>{new Date(o.createdAt).toLocaleString()}</td>
                                <td className="muted">{o.tableOrArea ? String(o.tableOrArea) : "—"}</td>
                                <td className="muted">{o.reservationId ? `#${o.reservationId}` : "—"}</td>
                                <td className="muted">{employeeEmailById.get(o.employeeId) ?? `#${o.employeeId}`}</td>
                                <td>
                                    <span className={`status-pill status-${String(o.status).toLowerCase()}`}>
                                        {o.status}
                                    </span>
                                </td>
                                <td className="right">
                                    <button
                                        className="btn btn-ghost"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            props.onOpenOrder(o.orderId);
                                        }}
                                    >
                                        Open →
                                    </button>
                                </td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}



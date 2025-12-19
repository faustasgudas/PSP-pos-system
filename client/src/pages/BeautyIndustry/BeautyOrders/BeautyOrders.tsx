import { useEffect, useMemo, useState } from "react";
import {
    listAllOrders,
    listMyOrders,
    type OrderSummary,
} from "../../../frontapi/orderApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";

import "../../../App.css";
import "./BeautyOrders.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

type OrdersScope = "mine" | "all";

export default function BeautyOrders(props: {
    onOpenOrder: (orderId: number) => void;
    onNewOrder: () => void;
}) {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const employeeId = Number(localStorage.getItem("employeeId"));
    const businessId = Number(localStorage.getItem("businessId"));

    const [scope, setScope] = useState<OrdersScope>("mine");
    const [statusFilter, setStatusFilter] = useState<string>("");
    const [queryId, setQueryId] = useState<string>("");

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [orders, setOrders] = useState<OrderSummary[]>([]);

    const [employees, setEmployees] = useState<any[]>([]);

    const canListAll = role === "Owner" || role === "Manager";
    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

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
                data = await listAllOrders({ status: statusFilter || undefined });
            } else {
                // Backend /orders/mine intentionally returns only OPEN orders.
                // For managers/owners, emulate "mine" for other statuses using listAllOrders + filter by employeeId.
                if (canListAll && employeeId) {
                    const all = await listAllOrders({ status: statusFilter || undefined });
                    data = (Array.isArray(all) ? all : []).filter((o) => o.employeeId === employeeId);
                } else {
                    data = await listMyOrders();
                }
            }

            setOrders(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load orders");
            setOrders([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        // If user isn't allowed to list all, force "mine".
        if (!canListAll && scope === "all") setScope("mine");
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [canListAll]);

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [scope, statusFilter]);

    const filtered = useMemo(() => {
        let data = orders;

        // Client-side filter (case-insensitive). For managers/owners scope=mine this is redundant,
        // but helpful for staff (mine endpoint is Open-only anyway).
        if (statusFilter) {
            const sf = statusFilter.toLowerCase();
            data = data.filter((o) => String(o.status).toLowerCase() === sf);
        }

        return [...data].sort(
            (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
    }, [orders, scope, statusFilter]);

    const openById = () => {
        const id = Number(queryId);
        if (!Number.isFinite(id) || id <= 0) {
            setError("Enter a valid Order ID");
            return;
        }
        props.onOpenOrder(id);
    };

    return (
        <div className="page beauty-orders">
            <div className="beauty-orders__header">
                <div>
                    <h2>Orders</h2>
                    <div className="muted">
                        View and manage orders (create, edit lines, split).
                    </div>
                </div>

               
            </div>

            <div className="orders-toolbar">
                <div className="orders-toolbar__left">
                    <div className="segmented">
                        <button className={scope === "mine" ? "active" : ""} onClick={() => setScope("mine")}>
                            My orders
                        </button>
                        <button className={scope === "all" ? "active" : ""} onClick={() => setScope("all")} disabled={!canListAll}>
                            All orders
                        </button>
                    </div>

                    <div style={{ minWidth: 200 }}>
                        <BeautySelect
                            value={statusFilter}
                            onChange={setStatusFilter}
                            placeholder="All statuses"
                            options={[
                                { value: "", label: "All statuses", subLabel: "Show everything" },
                                { value: "Open", label: "Open", subLabel: "Active orders" },
                                { value: "Closed", label: "Closed", subLabel: "Paid / finished" },
                                { value: "Cancelled", label: "Cancelled", subLabel: "Cancelled orders" },
                            ]}
                        />
                    </div>

                    <input
                        className="search-input"
                        placeholder="Search by order ID"
                        value={queryId}
                        onChange={(e) => setQueryId(e.target.value)}
                        onKeyDown={(e) => e.key === "Enter" && openById()}
                    />
                </div>

                <div className="orders-toolbar__right">
                    <button className="btn btn-ghost" onClick={load}>Refresh</button>
                    <button className="btn btn-primary" onClick={props.onNewOrder}>+ New order</button>
                </div>
            </div>


            {error && (
                <div className="beauty-orders__error">
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
            )}

            {loading ? (
                <div className="page">Loading orders…</div>
            ) : filtered.length === 0 ? (
                <div className="beauty-orders__empty">
                    No orders found.
                </div>
            ) : (
                <div className="orders-table-wrap">
                    <table className="orders-table">
                        <thead>
                        <tr>
                            <th>Order</th>
                            <th>Date</th>
                            <th>Employee</th>
                            <th>Status</th>
                            <th className="right">Actions</th>
                        </tr>
                        </thead>

                        <tbody>
                        {filtered.map((o) => (
                            <tr
                                key={o.orderId}
                                onClick={() => props.onOpenOrder(o.orderId)}
                                className="orders-row"
                            >
                                <td className="order-id">
                                    #{o.orderId}
                                </td>

                                <td>
                                    {new Date(o.createdAt).toLocaleString()}
                                </td>

                                <td>
                                    {employeeEmailById.get(o.employeeId) ?? `#${o.employeeId}`}
                                </td>

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



import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import { createOrder, listMyOrders, moveOrderLines, type OrderLine, type OrderSummary } from "../../../frontapi/orderApi";

type Draft = { orderLineId: number; maxQty: number; qty: string; checked: boolean };

export default function CateringOrderSplitDialog(props: {
    fromOrderId: number;
    lines: OrderLine[];
    onClose: () => void;
    onMoved: () => void;
}) {
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [targets, setTargets] = useState<OrderSummary[]>([]);
    const [targetOrderId, setTargetOrderId] = useState<number | null>(null);
    const [creating, setCreating] = useState(false);
    const [moving, setMoving] = useState(false);

    const [draft, setDraft] = useState<Draft[]>(
        props.lines.map((l) => ({
            orderLineId: l.orderLineId,
            maxQty: Number(l.qty),
            qty: String(l.qty),
            checked: false,
        }))
    );

    useEffect(() => {
        setDraft(
            props.lines.map((l) => ({
                orderLineId: l.orderLineId,
                maxQty: Number(l.qty),
                qty: String(l.qty),
                checked: false,
            }))
        );
    }, [props.lines]);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            setError(null);
            try {
                const mine = await listMyOrders();
                const open = mine.filter((o) => String(o.status) === "Open" && o.orderId !== props.fromOrderId);
                setTargets(open);
                if (open.length > 0) setTargetOrderId(open[0].orderId);
            } catch (e: any) {
                setError(e?.message || "Failed to load target orders");
                setTargets([]);
                setTargetOrderId(null);
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [props.fromOrderId]);

    const selected = useMemo(() => draft.filter((d) => d.checked), [draft]);

    const validate = () => {
        const target = targetOrderId;
        if (!target) return { ok: false as const, msg: "Pick a target order (or create one)." };
        if (target === props.fromOrderId) return { ok: false as const, msg: "Target must be different from source." };
        if (selected.length === 0) return { ok: false as const, msg: "Select at least one line." };

        const ids = selected.map((s) => s.orderLineId);
        if (new Set(ids).size !== ids.length) return { ok: false as const, msg: "Duplicate line selected." };

        const lines = selected.map((s) => ({ orderLineId: s.orderLineId, qty: Number(s.qty) }));
        for (const l of lines) {
            if (!Number.isFinite(l.qty) || l.qty <= 0) return { ok: false as const, msg: "Qty must be positive." };
            const src = draft.find((d) => d.orderLineId === l.orderLineId);
            if (!src) return { ok: false as const, msg: "Line not found. Refresh and try again." };
            if (l.qty > src.maxQty) return { ok: false as const, msg: "Qty exceeds available." };
        }

        return { ok: true as const, target, lines };
    };

    const createTarget = async () => {
        if (!employeeId || creating) return;
        setError(null);
        setCreating(true);
        try {
            const created = await createOrder(employeeId);
            const id = Number(created?.orderId);
            if (!id) throw new Error("Invalid orderId");
            setTargets((prev) => [{ ...(created as any), orderId: id, status: "Open" } as OrderSummary, ...prev]);
            setTargetOrderId(id);
        } catch (e: any) {
            setError(e?.message || "Failed to create target order");
        } finally {
            setCreating(false);
        }
    };

    const move = async () => {
        if (moving) return;
        const v = validate();
        if (!v.ok) {
            setError(v.msg);
            return;
        }
        setMoving(true);
        setError(null);
        try {
            await moveOrderLines(props.fromOrderId, { targetOrderId: v.target, lines: v.lines });
            props.onMoved();
        } catch (e: any) {
            setError(e?.message || "Move failed");
        } finally {
            setMoving(false);
        }
    };

    return (
        <div
            style={{
                position: "fixed",
                inset: 0,
                background: "rgba(0,0,0,0.35)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                padding: 16,
                zIndex: 999,
            }}
        >
            <div className="card" style={{ width: "min(860px, 100%)", textAlign: "left" }}>
                <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "center" }}>
                    <div>
                        <div style={{ fontWeight: 800, fontSize: 18 }}>Split order</div>
                        <div className="muted">Move selected lines to another open order.</div>
                    </div>
                    <button className="btn" onClick={props.onClose} disabled={moving}>
                        Close
                    </button>
                </div>

                {error && (
                    <div className="card" style={{ marginTop: 12, borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d" }}>
                        {error}
                    </div>
                )}

                <div style={{ marginTop: 12, display: "grid", gap: 12 }}>
                    <div>
                        <div className="muted" style={{ marginBottom: 6 }}>Target order</div>
                        <div style={{ display: "flex", gap: 10, alignItems: "center", flexWrap: "wrap" }}>
                            <select
                                className="dropdown"
                                value={targetOrderId ?? ""}
                                onChange={(e) => setTargetOrderId(Number(e.target.value))}
                                disabled={loading || moving}
                                style={{ maxWidth: 420 }}
                            >
                                {loading && <option value="">Loading…</option>}
                                {!loading && targets.length === 0 && <option value="">No other open orders</option>}
                                {targets.map((t) => (
                                    <option key={t.orderId} value={t.orderId}>
                                        #{t.orderId}
                                    </option>
                                ))}
                            </select>
                            <button className="btn" onClick={createTarget} disabled={creating || moving || !employeeId}>
                                {creating ? "Creating…" : "Create new order"}
                            </button>
                        </div>
                    </div>

                    <div>
                        <div className="muted" style={{ marginBottom: 6 }}>Lines to move</div>
                        <div style={{ display: "grid", gap: 8 }}>
                            {props.lines.map((l) => {
                                const d = draft.find((x) => x.orderLineId === l.orderLineId);
                                if (!d) return null;
                                return (
                                    <div key={l.orderLineId} className="card" style={{ padding: 12 }}>
                                        <div style={{ display: "flex", justifyContent: "space-between", gap: 10, alignItems: "center" }}>
                                            <label style={{ display: "flex", gap: 10, alignItems: "center" }}>
                                                <input
                                                    type="checkbox"
                                                    checked={d.checked}
                                                    onChange={(e) =>
                                                        setDraft((prev) =>
                                                            prev.map((x) =>
                                                                x.orderLineId === l.orderLineId ? { ...x, checked: e.target.checked } : x
                                                            )
                                                        )
                                                    }
                                                    disabled={moving}
                                                />
                                                <div>
                                                    <strong>{l.itemNameSnapshot}</strong>{" "}
                                                    <span className="muted">Line #{l.orderLineId} • Available: {l.qty}</span>
                                                </div>
                                            </label>

                                            <input
                                                className="dropdown"
                                                style={{ maxWidth: 140 }}
                                                type="number"
                                                step={0.01}
                                                value={d.qty}
                                                onChange={(e) =>
                                                    setDraft((prev) =>
                                                        prev.map((x) =>
                                                            x.orderLineId === l.orderLineId ? { ...x, qty: e.target.value } : x
                                                        )
                                                    )
                                                }
                                                disabled={moving || !d.checked}
                                            />
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                </div>

                <div style={{ marginTop: 12, display: "flex", justifyContent: "flex-end", gap: 10 }}>
                    <button className="btn" onClick={props.onClose} disabled={moving}>
                        Cancel
                    </button>
                    <button className="btn btn-primary" onClick={move} disabled={moving}>
                        {moving ? "Moving…" : "Move selected"}
                    </button>
                </div>
            </div>
        </div>
    );
}



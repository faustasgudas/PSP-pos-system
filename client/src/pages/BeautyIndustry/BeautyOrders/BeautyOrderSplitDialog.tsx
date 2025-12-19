import { useEffect, useMemo, useState } from "react";
import {
    createOrder,
    listMyOrders,
    moveOrderLines,
    type OrderLine,
    type OrderSummary,
} from "../../../frontapi/orderApi";

import "./BeautyOrderSplitDialog.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

type MoveDraft = {
    orderLineId: number;
    maxQty: number;
    qty: string; // keep as string for safe input
    checked: boolean;
};

export default function BeautyOrderSplitDialog(props: {
    fromOrderId: number;
    lines: OrderLine[];
    onClose: () => void;
    onMoved: (targetOrderId: number) => void;
}) {
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [loadingTargets, setLoadingTargets] = useState(true);
    const [targetsError, setTargetsError] = useState<string | null>(null);
    const [targets, setTargets] = useState<OrderSummary[]>([]);

    const [targetOrderId, setTargetOrderId] = useState<number | null>(null);
    const [creatingTarget, setCreatingTarget] = useState(false);
    const [moving, setMoving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [draft, setDraft] = useState<MoveDraft[]>(
        props.lines.map((l) => ({
            orderLineId: l.orderLineId,
            maxQty: Number(l.qty),
            qty: String(l.qty),
            checked: false,
        }))
    );

    useEffect(() => {
        // refresh draft when lines change
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
        const loadTargets = async () => {
            setLoadingTargets(true);
            setTargetsError(null);
            try {
                const mine = await listMyOrders();
                const open = mine.filter(
                    (o) => o.status === "Open" && o.orderId !== props.fromOrderId
                );
                setTargets(open);
                if (open.length > 0) setTargetOrderId(open[0].orderId);
            } catch (e: any) {
                setTargetsError(e?.message || "Failed to load target orders");
                setTargets([]);
                setTargetOrderId(null);
            } finally {
                setLoadingTargets(false);
            }
        };

        loadTargets();
    }, [props.fromOrderId]);

    const selectedCount = useMemo(
        () => draft.filter((d) => d.checked).length,
        [draft]
    );

    const targetOptions = useMemo(() => {
        if (loadingTargets) return [{ value: "", label: "Loading…", disabled: true }];
        if (!targets.length) return [{ value: "", label: "No other open orders", disabled: true }];
        const opts = targets.map((t) => ({
            value: String(t.orderId),
            label: `#${t.orderId}`,
            subLabel: new Date(t.createdAt).toLocaleTimeString(),
        }));
        // include current value if not in targets (edge)
        if (targetOrderId && targets.find((t) => t.orderId === targetOrderId) == null) {
            opts.unshift({ value: String(targetOrderId), label: `#${targetOrderId}`, subLabel: "" });
        }
        return opts;
    }, [loadingTargets, targets, targetOrderId]);

    const validate = (): { ok: true; target: number; lines: { orderLineId: number; qty: number }[] } | { ok: false; message: string } => {
        const target = targetOrderId;
        if (!target || target <= 0) return { ok: false, message: "Pick a target order (or create a new one)." };
        if (target === props.fromOrderId) return { ok: false, message: "Target order must be different from source." };

        const picked = draft.filter((d) => d.checked);
        if (picked.length === 0) return { ok: false, message: "Select at least one line to move." };

        // backend rejects duplicates; ensure unique
        const ids = picked.map((p) => p.orderLineId);
        const unique = new Set(ids);
        if (unique.size !== ids.length) return { ok: false, message: "Duplicate line selected (unexpected)." };

        const lines = picked.map((p) => {
            const qty = Number(p.qty);
            return { orderLineId: p.orderLineId, qty };
        });

        for (const l of lines) {
            if (!Number.isFinite(l.qty) || l.qty <= 0) return { ok: false, message: "Move qty must be a positive number." };

            const src = draft.find((d) => d.orderLineId === l.orderLineId);
            if (!src) return { ok: false, message: "Line not found in source (refresh and try again)." };
            if (l.qty > src.maxQty) return { ok: false, message: "Move qty cannot exceed available qty." };
        }

        return { ok: true, target, lines };
    };

    const createNewTarget = async () => {
        if (!employeeId || creatingTarget) return;
        setError(null);
        setCreatingTarget(true);
        try {
            const created = await createOrder(employeeId);
            const newId = Number(created?.orderId);
            if (!newId) throw new Error("Backend returned invalid orderId");

            setTargets((prev) => [{ ...(created as any), orderId: newId, status: "Open" } as OrderSummary, ...prev]);
            setTargetOrderId(newId);
        } catch (e: any) {
            setError(e?.message || "Failed to create target order");
        } finally {
            setCreatingTarget(false);
        }
    };

    const move = async () => {
        if (moving) return;
        setError(null);

        const v = validate();
        if (!v.ok) {
            setError(v.message);
            return;
        }

        setMoving(true);
        try {
            await moveOrderLines(props.fromOrderId, {
                targetOrderId: v.target,
                lines: v.lines,
            });
            props.onMoved(v.target);
        } catch (e: any) {
            setError(e?.message || "Failed to move lines");
        } finally {
            setMoving(false);
        }
    };

    return (
        <div className="split-overlay" role="dialog" aria-modal="true">
            <div className="split-dialog">
                <div className="split-header">
                    <div>
                        <div className="split-title">Split order</div>
                        <div className="muted">
                            Move selected order lines to another open order.
                        </div>
                    </div>
                    <button className="btn" onClick={props.onClose} disabled={moving}>
                        Close
                    </button>
                </div>

                <div className="split-body">
                    <div className="split-section">
                        <div className="split-section-title">Target order</div>

                        {targetsError && <div className="split-error">{targetsError}</div>}

                        <div className="split-target-row">
                            <div style={{ minWidth: 260, flex: 1 }}>
                                <BeautySelect
                                    value={targetOrderId ? String(targetOrderId) : ""}
                                    onChange={(v) => setTargetOrderId(v ? Number(v) : null)}
                                    disabled={loadingTargets || moving}
                                    placeholder="Select target order"
                                    options={targetOptions}
                                />
                            </div>

                            <button
                                className="btn"
                                onClick={createNewTarget}
                                disabled={creatingTarget || moving || !employeeId}
                                title={!employeeId ? "Missing employeeId in localStorage" : ""}
                            >
                                {creatingTarget ? "Creating…" : "Create new order"}
                            </button>
                        </div>
                    </div>

                    <div className="split-section">
                        <div className="split-section-title">
                            Lines to move ({selectedCount} selected)
                        </div>

                        <div className="split-lines">
                            {props.lines.map((l) => {
                                const d = draft.find((x) => x.orderLineId === l.orderLineId);
                                if (!d) return null;

                                return (
                                    <div key={l.orderLineId} className="split-line">
                                        <label className="split-line-left">
                                            <input
                                                type="checkbox"
                                                checked={d.checked}
                                                onChange={(e) =>
                                                    setDraft((prev) =>
                                                        prev.map((x) =>
                                                            x.orderLineId === l.orderLineId
                                                                ? { ...x, checked: e.target.checked }
                                                                : x
                                                        )
                                                    )
                                                }
                                                disabled={moving}
                                            />
                                            <div className="split-line-name">
                                                {l.itemNameSnapshot}
                                                <div className="muted small">
                                                    Line #{l.orderLineId} • Available: {l.qty}
                                                </div>
                                            </div>
                                        </label>

                                        <div className="split-line-right">
                                            <input
                                                className="dropdown qty"
                                                type="number"
                                                min={0}
                                                step={0.01}
                                                value={d.qty}
                                                onChange={(e) =>
                                                    setDraft((prev) =>
                                                        prev.map((x) =>
                                                            x.orderLineId === l.orderLineId
                                                                ? { ...x, qty: e.target.value }
                                                                : x
                                                        )
                                                    )
                                                }
                                                disabled={moving || !d.checked}
                                                title={!d.checked ? "Select the line first" : ""}
                                            />
                                            <div className="muted small">
                                                €{Number(l.unitPriceSnapshot).toFixed(2)}
                                            </div>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>

                    {error && <div className="split-error">{error}</div>}
                </div>

                <div className="split-footer">
                    <button className="btn" onClick={props.onClose} disabled={moving}>
                        Cancel
                    </button>
                    <button
                        className="btn btn-primary"
                        onClick={move}
                        disabled={moving}
                    >
                        {moving ? "Moving…" : "Move selected lines"}
                    </button>
                </div>
            </div>
        </div>
    );
}



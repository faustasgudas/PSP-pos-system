import { useEffect, useMemo, useState } from "react";
import "./BeautyGiftCards.css";

import {
    createGiftCard,
    deactivateGiftCard,
    getGiftCardByCode,
    getGiftCardById,
    listGiftCards,
    redeemGiftCard,
    topUpGiftCard,
    type GiftCardResponse,
} from "../../../frontapi/giftCardApi";

function formatMoneyFromMinorUnits(value: number, currency: string = "EUR") {
    const major = value / 100;
    try {
        return new Intl.NumberFormat(undefined, {
            style: "currency",
            currency,
        }).format(major);
    } catch {
        return `${major.toFixed(2)} ${currency}`;
    }
}

function parseMoneyToMinorUnits(input: string): number | null {
    const n = Number(input);
    if (!Number.isFinite(n)) return null;
    if (n < 0) return null;
    return Math.round(n * 100);
}

export default function BeautyGiftCards() {
    const businessId = Number(localStorage.getItem("businessId"));
    const isBusinessReady = Number.isFinite(businessId) && businessId > 0;

    const [giftCards, setGiftCards] = useState<GiftCardResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [statusFilter, setStatusFilter] = useState<string>("");
    const [codeFilter, setCodeFilter] = useState<string>("");

    const [showCreate, setShowCreate] = useState(false);
    const [newCode, setNewCode] = useState("");
    const [newInitialAmount, setNewInitialAmount] = useState("");
    const [newExpiresAt, setNewExpiresAt] = useState<string>("");
    const [creating, setCreating] = useState(false);

    const [selected, setSelected] = useState<GiftCardResponse | null>(null);
    const [manageTopUpAmount, setManageTopUpAmount] = useState("");
    const [manageRedeemAmount, setManageRedeemAmount] = useState("");
    const [managing, setManaging] = useState(false);

    const [lookupCode, setLookupCode] = useState("");
    const [lookupResult, setLookupResult] = useState<GiftCardResponse | null>(null);
    const [lookingUp, setLookingUp] = useState(false);

    const load = async () => {
        if (!isBusinessReady) return;
        setLoading(true);
        setError(null);
        try {
            const data = await listGiftCards(businessId, {
                status: statusFilter || undefined,
                code: codeFilter || undefined,
            });
            setGiftCards(data);
        } catch (e: any) {
            setError(e?.message ?? "Failed to load gift cards");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    const visibleGiftCards = useMemo(() => giftCards, [giftCards]);

    const openCreate = () => {
        setNewCode("");
        setNewInitialAmount("");
        setNewExpiresAt("");
        setError(null);
        setShowCreate(true);
    };

    const handleCreate = async () => {
        if (!isBusinessReady) {
            setError("Missing businessId (re-login?)");
            return;
        }

        const code = newCode.trim();
        if (!code) {
            setError("Code is required");
            return;
        }

        const balance = parseMoneyToMinorUnits(newInitialAmount.trim());
        if (balance === null) {
            setError("Initial amount must be a valid number");
            return;
        }

        setCreating(true);
        setError(null);
        try {
            const created = await createGiftCard(businessId, {
                code,
                balance,
                expiresAt: newExpiresAt ? new Date(newExpiresAt).toISOString() : null,
            });
            setShowCreate(false);
            setGiftCards((prev) => [created, ...prev]);
        } catch (e: any) {
            setError(e?.message ?? "Failed to create gift card");
        } finally {
            setCreating(false);
        }
    };

    const openManage = (card: GiftCardResponse) => {
        setSelected(card);
        setManageTopUpAmount("");
        setManageRedeemAmount("");
        setError(null);
    };

    const refreshSelected = async () => {
        if (!isBusinessReady || !selected) return;
        const fresh = await getGiftCardById(businessId, selected.giftCardId);
        setSelected(fresh);
        setGiftCards((prev) => prev.map((c) => (c.giftCardId === fresh.giftCardId ? fresh : c)));
    };

    const handleTopUp = async () => {
        if (!isBusinessReady || !selected) return;
        const amount = parseMoneyToMinorUnits(manageTopUpAmount.trim());
        if (amount === null || amount <= 0) {
            setError("Top-up amount must be greater than 0");
            return;
        }

        setManaging(true);
        setError(null);
        try {
            await topUpGiftCard(businessId, selected.giftCardId, amount);
            await refreshSelected();
            setManageTopUpAmount("");
        } catch (e: any) {
            setError(e?.message ?? "Top up failed");
        } finally {
            setManaging(false);
        }
    };

    const handleRedeem = async () => {
        if (!isBusinessReady || !selected) return;
        const amount = parseMoneyToMinorUnits(manageRedeemAmount.trim());
        if (amount === null || amount <= 0) {
            setError("Redeem amount must be greater than 0");
            return;
        }

        setManaging(true);
        setError(null);
        try {
            const res = await redeemGiftCard(businessId, selected.giftCardId, amount);
            const updated: GiftCardResponse = { ...selected, balance: res.remainingBalance };
            setSelected(updated);
            setGiftCards((prev) => prev.map((c) => (c.giftCardId === updated.giftCardId ? updated : c)));
            setManageRedeemAmount("");
        } catch (e: any) {
            setError(e?.message ?? "Redeem failed");
        } finally {
            setManaging(false);
        }
    };

    const handleDeactivate = async () => {
        if (!isBusinessReady || !selected) return;
        setManaging(true);
        setError(null);
        try {
            await deactivateGiftCard(businessId, selected.giftCardId);
            await refreshSelected();
        } catch (e: any) {
            setError(e?.message ?? "Deactivate failed");
        } finally {
            setManaging(false);
        }
    };

    const handleLookup = async () => {
        if (!isBusinessReady) return;
        const code = lookupCode.trim();
        if (!code) return;

        setLookingUp(true);
        setError(null);
        try {
            const card = await getGiftCardByCode(businessId, code);
            setLookupResult(card);
        } catch (e: any) {
            setLookupResult(null);
            setError(e?.message ?? "Lookup failed");
        } finally {
            setLookingUp(false);
        }
    };

    return (
        <div className="giftcards-container">
            <div className="action-bar">
                <h2 className="section-title">Gift Cards</h2>
                <div className="giftcard-actions">
                    <button className="btn btn-secondary" onClick={load} disabled={loading || !isBusinessReady}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                    <button className="btn btn-primary" onClick={openCreate} disabled={!isBusinessReady}>
                        ➕ New Gift Card
                    </button>
                </div>
            </div>

            {!isBusinessReady && (
                <div className="no-giftcards">Missing business context (try logging in again).</div>
            )}

            {error && <div className="no-giftcards">{error}</div>}

            <div className="giftcards-filters">
                <input
                    className="giftcards-filter-input"
                    placeholder="Filter by code…"
                    value={codeFilter}
                    onChange={(e) => setCodeFilter(e.target.value)}
                />
                <select
                    className="giftcards-filter-input"
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                >
                    <option value="">All statuses</option>
                    <option value="Active">Active</option>
                    <option value="Inactive">Inactive</option>
                </select>
                <button className="btn btn-secondary" onClick={load} disabled={loading || !isBusinessReady}>
                    Apply
                </button>
            </div>

            <div className="giftcards-lookup">
                <input
                    className="giftcards-filter-input"
                    placeholder="Lookup by exact code…"
                    value={lookupCode}
                    onChange={(e) => setLookupCode(e.target.value)}
                />
                <button className="btn btn-secondary" onClick={handleLookup} disabled={lookingUp || !isBusinessReady}>
                    {lookingUp ? "Looking up…" : "Lookup"}
                </button>
                {lookupResult && (
                    <button className="btn btn-primary" onClick={() => openManage(lookupResult)}>
                        Open
                    </button>
                )}
            </div>

            <div className="giftcards-list">
                {visibleGiftCards.length > 0 ? (
                    visibleGiftCards.map((card) => (
                        <div
                            key={card.giftCardId}
                            className="giftcard-card"
                            role="button"
                            tabIndex={0}
                            onClick={() => openManage(card)}
                            onKeyDown={(e) => (e.key === "Enter" ? openManage(card) : null)}
                        >
                            <div>
                                <div className="giftcard-code">{card.code}</div>
                                <div className="giftcard-balance">
                                    {formatMoneyFromMinorUnits(card.balance, "EUR")}
                                </div>
                                <div className={`giftcard-status ${card.status === "Inactive" ? "blocked" : ""}`}>
                                    {card.status}
                                </div>
                            </div>
                            <div className="giftcard-actions">
                                <button
                                    className="btn-small"
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        openManage(card);
                                    }}
                                >
                                    Manage
                                </button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-giftcards">{loading ? "Loading…" : "No gift cards found"}</div>
                )}
            </div>

            {showCreate && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">New Gift Card</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input
                                    type="text"
                                    placeholder="e.g. GC-ABCD-1234"
                                    value={newCode}
                                    onChange={(e) => setNewCode(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Initial amount (EUR)</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    placeholder="e.g. 25.00"
                                    value={newInitialAmount}
                                    onChange={(e) => setNewInitialAmount(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Expires at (optional)</label>
                                <input
                                    type="date"
                                    value={newExpiresAt}
                                    onChange={(e) => setNewExpiresAt(e.target.value)}
                                />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowCreate(false)} disabled={creating}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={handleCreate} disabled={creating}>
                                {creating ? "Creating…" : "Create Gift Card"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {selected && (
                <div className="modal-overlay" onClick={() => setSelected(null)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">Manage Gift Card</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input type="text" value={selected.code} readOnly />
                            </div>
                            <div className="modal-field">
                                <label>Balance</label>
                                <input
                                    type="text"
                                    value={formatMoneyFromMinorUnits(selected.balance, "EUR")}
                                    readOnly
                                />
                            </div>

                            <div className="modal-field">
                                <label>Top up (EUR)</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    value={manageTopUpAmount}
                                    onChange={(e) => setManageTopUpAmount(e.target.value)}
                                />
                                <button className="btn btn-success" onClick={handleTopUp} disabled={managing}>
                                    Top up
                                </button>
                            </div>

                            <div className="modal-field">
                                <label>Redeem (EUR)</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    value={manageRedeemAmount}
                                    onChange={(e) => setManageRedeemAmount(e.target.value)}
                                />
                                <button className="btn btn-primary" onClick={handleRedeem} disabled={managing}>
                                    Redeem
                                </button>
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setSelected(null)} disabled={managing}>
                                Close
                            </button>
                            <button className="btn btn-danger" onClick={handleDeactivate} disabled={managing}>
                                Deactivate
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

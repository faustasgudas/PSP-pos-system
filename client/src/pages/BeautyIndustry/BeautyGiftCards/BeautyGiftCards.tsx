import { useState } from "react";
import "./BeautyGiftCards.css";
import type { GiftCardResponse } from "../../../types/api";
import * as giftCardService from "../../../services/giftCardService";

interface BeautyGiftCardsProps {
    giftCards: GiftCardResponse[];
    onRefresh: () => void;
}

export default function BeautyGiftCards({ giftCards, onRefresh }: BeautyGiftCardsProps) {
    const [showModal, setShowModal] = useState(false);
    const [balance, setBalance] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    return (
        <div className="giftcards-container">
            <div className="action-bar">
                <h2 className="section-title">Gift Cards</h2>
                <button className="btn btn-primary" onClick={() => setShowModal(true)}>
                    ➕ New Gift Card
                </button>
            </div>

            <div className="giftcards-list">
                {giftCards.length > 0 ? (
                    giftCards.map(card => (
                        <div key={card.giftCardId} className="giftcard-card">
                            <div className="giftcard-code">Code: {card.code}</div>
                            <div className="giftcard-balance">Balance: €{(card.balance / 100).toFixed(2)}</div>
                            <div className="giftcard-status">Status: {card.status}</div>
                            {card.expiresAt && (
                                <div className="giftcard-expires">
                                    Expires: {new Date(card.expiresAt).toLocaleDateString()}
                                </div>
                            )}
                        </div>
                    ))
                ) : (
                    <div className="no-giftcards">No gift cards found</div>
                )}
            </div>

            {/* ✅ NEW GIFT CARD MODAL */}
            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">New Gift Card</h3>

                        {error && (
                            <div style={{ color: "red", marginBottom: "1rem" }}>
                                {error}
                            </div>
                        )}

                        <form
                            onSubmit={async (e) => {
                                e.preventDefault();
                                setIsSubmitting(true);
                                setError(null);
                                try {
                                    // Balance is in cents, so multiply by 100
                                    await giftCardService.createGiftCard({
                                        balance: Math.round(parseFloat(balance) * 100),
                                    });
                                    setBalance("");
                                    setShowModal(false);
                                    onRefresh();
                                } catch (err) {
                                    setError(err instanceof Error ? err.message : "Failed to create gift card");
                                } finally {
                                    setIsSubmitting(false);
                                }
                            }}
                        >
                            <div className="modal-form">
                                <div className="modal-field">
                                    <label>Initial Balance (EUR) *</label>
                                    <input
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        value={balance}
                                        onChange={(e) => setBalance(e.target.value)}
                                        required
                                    />
                                </div>
                            </div>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={() => setShowModal(false)}
                                    disabled={isSubmitting}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="btn btn-success"
                                    disabled={isSubmitting}
                                >
                                    {isSubmitting ? "Creating..." : "Create Gift Card"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}

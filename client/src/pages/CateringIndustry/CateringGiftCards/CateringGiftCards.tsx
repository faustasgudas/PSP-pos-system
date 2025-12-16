import { useState } from 'react';
import "./CateringGiftCards.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

interface GiftCard {
    id: number;
    code: string;
    balance: { amount: number; currency: string };
    status: string;
}

interface CateringGiftCardsProps {
    giftCards: GiftCard[];
}

export default function CateringGiftCards({ giftCards }: CateringGiftCardsProps) {
    const [showModal, setShowModal] = useState(false);
    const [currency, setCurrency] = useState("EUR");

    return (
        <div className="giftcards-container">
            <div className="action-bar">
                <h2 className="section-title">Gift Card Management</h2>
                <button className="btn btn-primary" onClick={() => setShowModal(true)}>
                    <span>➕</span> New Gift Card
                </button>
            </div>
            <div className="giftcards-list">
                {giftCards.length > 0 ? (
                    giftCards.map(card => (
                        <div key={card.id} className="giftcard-card">
                            <div className="giftcard-code">{card.code}</div>
                        </div>
                    ))
                ) : (
                    <div className="no-giftcards">No gift cards found</div>
                )}
            </div>

            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">New Gift Card</h3>
                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Initial Balance</label>
                                <input type="number" />
                            </div>
                            <div className="modal-field">
                                <label>Currency</label>
                                <BeautySelect
                                    value={currency}
                                    onChange={setCurrency}
                                    options={[
                                        { value: "EUR", label: "EUR" },
                                        { value: "USD", label: "USD" },
                                        { value: "GBP", label: "GBP" },
                                    ]}
                                />
                            </div>
                        </div>
                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowModal(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-success">
                                Create Gift Card
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
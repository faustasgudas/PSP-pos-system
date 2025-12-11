import { useState } from 'react';
import "../../../App.css";
import "./CateringGiftCards.css";

interface GiftCard {
    id: number;
    code: string;
    balance: { amount: number; currency: string };
    status: string;
}

function CateringGiftCards() {
    const [showModal, setShowModal] = useState(false);

    return (
        <div className="content-box" id="gift-cards">
            <div className="action-bar">
                <h2 className="section-title">Gift Card Management</h2>
                <button className="btn btn-primary">
                    <span>➕</span> New Gift Card
                </button>
            </div>
            <div className="gift-cards-grid" id="gift-cards-grid">
                {/* todo - add gift cards grid */}
            </div>
        </div>
    )
}

export default CateringGiftCards;
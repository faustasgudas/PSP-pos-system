import { useState } from "react";
import "./BeautyInventory.css";

interface StockItem {
    id: number;
    name: string;
    qtyOnHand: number;
    unit: string;
}

interface BeautyInventoryProps {
    stockItems: StockItem[];
}

export default function BeautyInventory({ stockItems }: BeautyInventoryProps) {
    const [showModal, setShowModal] = useState(false);

    return (
        <div className="inventory-container">
            <div className="action-bar">
                <h2 className="section-title">Inventory</h2>
                <button className="btn btn-primary" onClick={() => setShowModal(true)}>
                    ➕ Add Item
                </button>
            </div>

            <div className="inventory-list">
                {stockItems.length > 0 ? (
                    stockItems.map(item => (
                        <div key={item.id} className="inventory-card">
                            <div>
                                <div className="inventory-name">{item.name}</div>
                                <div className="inventory-qty">
                                    {item.qtyOnHand} {item.unit}
                                </div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-inventory">No inventory items found</div>
                )}
            </div>

            {/* ✅ ADD ITEM MODAL */}
            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">Add Inventory Item</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Item Name</label>
                                <input type="text" />
                            </div>

                            <div className="modal-field">
                                <label>Quantity</label>
                                <input type="number" />
                            </div>

                            <div className="modal-field">
                                <label>Unit</label>
                                <input type="text" />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowModal(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-success">
                                Save Item
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

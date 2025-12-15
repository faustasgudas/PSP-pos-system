import { useState } from 'react';
import "./CateringProducts.css";

interface Product {
    id: number;
    name: string;
    basePrice: {amount: number; currency: string};
}

interface CateringProductsProps {
    products: Product[];
}

export default function CateringProducts({ products }: CateringProductsProps) {
    const [showModal, setShowModal] = useState(false);
    
    return (
        <div className="products-container">
            <div className="action-bar">
                <h2 className="section-title">Products</h2>
                <button 
                    className="btn btn-primary"
                    onClick={() => setShowModal(true)}
                >
                    <span>➕</span> Add Product
                </button>
            </div>
            <div className="products-list">
                {products.length > 0 ? (
                    products.map((product) => (
                        <div key={product.id} className="product-card">
                            <div>   
                                <div className="product-name">{product.name}</div>
                                <div className="product-price">
                                    {product.basePrice.amount} {product.basePrice.currency}
                                </div>
                            </div>
                            <div className="product-actions">
                                <button className="btn-small">Edit</button>
                                <button className="btn-small btn-danger">Delete</button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-products">No products found</div>
                )}
            </div>

            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">Add Product</h3>
                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Product Name</label>
                                <input type="text" />
                            </div>
                            <div className="modal-field">
                                <label>Price</label>
                                <input type="number" />
                            </div>
                            <div className="modal-field">
                                <label>Currency</label>
                                <select>
                                    <option value="EUR">EUR</option>
                                    <option value="USD">USD</option>
                                    <option value="GBP">GBP</option>
                                </select>
                            </div>
                        </div>
                        <div className="modal-actions">
                            <button
                                className="btn btn-secondary"
                                onClick={() => setShowModal(false)}
                            >
                                Cancel
                            </button>
                            <button className="btn btn-success">
                                Save Product
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
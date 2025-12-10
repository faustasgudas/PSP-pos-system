import { useState } from 'react';
import "../../../App.css";
import "./CateringProducts.css";

function CateringProducts() {
    return (
        <div className="content-box" id="products">
            <div className="action-bar">
                <h2 className="section-title">Product Catalog</h2>
                <button className="btn btn-primary">
                    <span>➕</span> Add Product
                </button>
            </div>
            <div className="product-grid" id="product-grid">
                {/* todo - add product grid */}
            </div>
        </div>
    )
}

export default CateringProducts;
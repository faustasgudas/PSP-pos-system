import { useEffect, useState } from 'react';
import "./CateringProducts.css";
import { getUserFromToken } from "../../../utils/auth";

interface Product {
    catalogItemId: number;
    name: string;
    basePrice: number;
    status: "Active" | "Archived";
}

export default function CateringProducts() {
    const user = getUserFromToken();
    const role = user?.role;
    
    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");
    
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    
    const [showAddModal, setShowAddModal] = useState(false);
    const [showEditModal, setShowEditModal] = useState<Product | null>(null);
    
    const [name, setName] = useState("");
    const [price, setPrice] = useState("");
    
    const fetchProducts = async () => {
        try {
            const res = await fetch(
                `https://localhost:44317/api/businesses/${businessId}/catalog-items?type=Product`,
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                }
            );
            
            if (!res.ok) throw new Error();
            const data = await res.json();
            setProducts(data);
        } catch {
            setProducts([]);
        } finally {
            setLoading(false);
        }
    };
    
    useEffect(() => {
        fetchProducts();
    }, []);
    
    const activeProducts = products.filter(p => p.status === "Active");
    const archivedProducts = products.filter(s => s.status === "Archived");
    
    const handleAddProduct = async () => {
        await fetch(
            `https://localhost:44317/api/businesses/${businessId}/catalog-items`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({
                    name,
                    code: name.toUpperCase().replace(/\s+/g, "_"),
                    type: "Service",
                    basePrice: Number(price),
                    taxClass: "STANDARD",
                }),
            }
        );
        setShowAddModal(false);
        setName("");
        setPrice("");
        fetchProducts();
    };
    
    const handleDeactivate = async (id: number) => {
        await fetch(
            `https://localhost:44317/api/businesses/${businessId}/catalog-items/${id}/archive`,
            {
                method: "POST",
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );
        fetchProducts();
    };
    
    const handleReactivate = async (id: number) => {
        await fetch(
            `https://localhost:44317/api/businesses/${businessId}/catalog-items/${id}`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({
                    status: "Active"
                }),
            }
        );
        fetchProducts();
    };
    
    const handleEditPrice = async () => {
        if (!showEditModal) return;
        await fetch(
            `https://localhost:44317/api/businesses/${businessId}/catalog-items/${showEditModal.catalogItemId}`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({
                    basePrice: Number(price)
                }),
            }
        );
        setShowEditModal(null);
        setPrice("");
        fetchProducts();
    };
    
    if (loading) return <div>Loading products…</div>;
    
    return (
        <div className="products page">
            <div className="products-header">
                <h2>Products</h2>

                {(role === "Owner" || role === "Manager") && (
                    <button className="btn btn-primary" onClick={() => setShowAddModal(true)}>
                        ➕ Add Product
                    </button>
                )}
            </div>

            <h3>Active Services</h3>
            <div className="products-grid">
                {activeProducts.map(product => (
                    <div key={product.catalogItemId} className="product-card">
                        <div className="product-name">{product.name}</div>
                        <div className="product-price">€{product.basePrice}</div>

                        {(role === "Owner" || role === "Manager") && (
                            <div className="card-actions">
                                <button
                                    className="btn"
                                    onClick={() => {
                                        setShowEditModal(product);
                                        setPrice(String(product.basePrice));
                                    }}
                                >
                                    ✏️ Edit
                                </button>
                                <button
                                    className="btn btn-danger"
                                    onClick={() => handleDeactivate(product.catalogItemId)}
                                >
                                    Deactivate
                                </button>
                            </div>
                        )}
                    </div>
                ))}
            </div>

            <hr className="products-divider" />

            <h3 className="muted">Deactivated Products</h3>
            <div className="products-grid archived">
                {archivedProducts.map(product => (
                    <div key={product.catalogItemId} className="product-card archived">
                        <div className="product-name">{product.name}</div>
                        <div className="product-price">€{product.basePrice}</div>

                        {(role === "Owner" || role === "Manager") && (
                            <button
                                className="btn btn-success"
                                onClick={() => handleReactivate(product.catalogItemId)}
                            >
                                ♻️ Reactivate
                            </button>
                        )}
                    </div>
                ))}
            </div>

            {showAddModal && (
                <div className="modal-backdrop">
                    <div className="modal">
                        <h3>Add Product</h3>
                        <input
                            placeholder="Product name"
                            value={name}
                            onChange={e => setName(e.target.value)}
                        />

                        <input
                            placeholder="Price"
                            type="number"
                            value={price}
                            onChange={e => setPrice(e.target.value)}
                        />

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setShowAddModal(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={handleAddProduct}>
                                Add
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {showEditModal && (
                <div className="modal-backdrop">
                    <div className="modal">
                        <h3>Edit Price</h3>

                        <input
                            type="number"
                            value={price}
                            onChange={e => setPrice(e.target.value)}
                        />

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setShowEditModal(null)}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={handleEditPrice}>
                                Save
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
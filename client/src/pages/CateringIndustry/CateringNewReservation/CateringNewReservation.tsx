import { useState } from 'react';
import "../../../App.css";
import "./CateringNewReservation.css";

function CateringNewReservation() {
    return (
        <div className="content-box" id="new-reservation">
            <div className="action-bar">
                <h2 className="section-title">New Table Reservation</h2>
                <button className="btn btn-secondary">Back to Reservations</button>
            </div>
            <div className="card">
                <h3>Select Table</h3>
                <div className="table-selection" id="table-selection">
                    {/* todo - add table selection */}
                </div>
                <h3>Reservation Information</h3>
                <div className="form-grid">
                    <div className="form-group">
                        <label className="form-label">Customer Name</label>
                        <input type="text" className="form-input" id="customer-name" placeholder="Enter customer name"></input>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Customer Phone</label>
                        <input type="tel" className="form-input" id="customer-phone" placeholder="+370 600 00000"></input>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Customer Email</label>
                        <input type="email" className="form-input" id="customer-email" placeholder="customer@example.com"></input>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Number of Guests</label>
                        <select className="form-select" id="guest-count">
                            <option>1</option>
                            <option>2</option>
                            <option>3</option>
                            <option>4</option>
                            <option>5</option>
                            <option>6</option>
                            <option>7</option>
                            <option>8+</option>
                        </select>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Date</label>
                        <input type="date" className="form-input" id="reservation-date" placeholder="2025-12-17"></input>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Time</label>
                        <select className="form-select" id="reservation-time">
                            <option>09:00</option>
                            <option>10:00</option>
                            <option>11:00</option>
                            <option>12:00</option>
                            <option>13:00</option>
                            <option>14:00</option>
                            <option>15:00</option>
                            <option>16:00</option>
                            <option>17:00</option>
                            <option>18:00</option>
                            <option>19:00</option>
                            <option>20:00</option>
                        </select>
                    </div>
                    <div className="form-group full-width">
                        <label className="form-label">Special Requests (optional)</label>
                        <textarea className="form-textarea" id="reservation-notes" rows="3" placeholder="Allergies, celebrations, etc."></textarea>
                    </div>
                </div>
                <div>
                    <button className="btn btn-secondary">Cancel</button>
                    <button className="btn btn-success">
                        <span>✓</span> Create Reservation
                    </button>
                </div>
            </div>
        </div>
    )
}

export default CateringNewReservation;
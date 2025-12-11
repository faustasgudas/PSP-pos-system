import { useState } from 'react';
import "../../../App.css";
import "./CateringReservations.css";

interface Reservation {
    id: number;
    customerName: string;
    customerPhone: string;
    customerEmail: string;
    reservationStart: string;
    reservationEnd: string;
    status: string;
    employeeId: number;
    notes?: string;
}

interface Table {
    id: number;
    seats: number;
    status: string;
}

interface Employee {
    id: number;
    name: string;
    role: string;
    status: string;
}

function CateringReservations(){
    return(
        <div className="content-box" id="reservations">
            <div className="action-bar">
                <h2 className="section-title">Reservation Management</h2>
                <button className="btn btn-primary">
                    <span>➕</span> New Reservation
                </button>
            </div>
            <div className="calendar-container">
                <div className="calendar-header">
                    <div className="calendar-title" id="calendar-title">December 2025</div>
                    <div className="calendar-nav">
                        <button className="btn btn-secondary btn-sm">
                            ◀ Prev
                        </button>
                        <button className="btn btn-secondary btn-sm">
                            Next ▶
                        </button>
                    </div>
                </div>
                <div className="calendar-grid" id="calendar-grid">
                    {/* todo - add calendar here */}
                </div>
            </div>
            <div className="time-slots-container">
                <h3>Available Time Slots for
                <span id="selected-date">December 17, 2025</span></h3>
                <div className="time-slots-grid" id="time-slots-grid">
                    {/* todo - add time slot grid here */}
                </div>
            </div>
        </div>
    )
}

export default CateringReservations;
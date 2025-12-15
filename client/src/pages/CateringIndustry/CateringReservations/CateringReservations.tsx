import { useState, useMemo } from 'react';
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

interface CateringReservationsProps {
    reservations: Reservation[];
    tables: Table[];
    employees: Employee[];
}

{/* todo - finish this */}
export default function CateringReservations({reservations, tables, employees}: CateringReservationsProps) {
    const [selectedDate, setSelectedDate] = useState(new Date());
    const [currentMonth, setCurrentMonth] = useState(new Date());

    const year = currentMonth.getFullYear();
    const month = currentMonth.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const isSameDay = (a: Date, b: Date) =>
        a.getFullYear() === b.getFullYear() && 
        a.getMonth() === b.getMonth() &&
        a.getDate() === b.getDate();
    
    const monthLabel = currentMonth.toLocaleString("default", {
        month: "long",
        year: "numeric",
    });
    
    const reservedDates = useMemo(() => {
        const days = new Set<number>();
        reservations.forEach(r => {
            const d = new Date(r.reservationStart);
            if (d.getFullYear() === year && d.getMonth() === month) {
                days.add(d.getDate());
            }
        });
        return Array.from(days);
    }, [reservations, year, month]);
    
    const reservationsForSelectedDate = useMemo(() => {
        return reservations.filter(r =>
            isSameDay(new Date(r.reservationStart), selectedDate)    
        );
    }, [reservations, selectedDate]);
    
    return (
        <div className="reservations-container">
            <div className="action-bar">
                <h2 className="section-title">Reservation Management</h2>
                
                {/* todo - add on click open new reservation modal */}
                <button
                    className="btn btn-primary"
                >
                    <span>➕</span> New Reservation
                </button>
            </div>
            <div className="calendar-wrapper">
                <div className="calendar-header">
                    <button
                        className="cal-nav-btn"
                        onClick={() => setCurrentMonth(new Date(year, month - 1, 1 ))}
                    >
                        ◀ Prev
                    </button>
                    <div className="calendar-title">{monthLabel}</div>
                    <button 
                        className="cal-nav-btn"
                        onClick={() => setCurrentMonth(new Date(year, month + 1, 1 ))}
                    >
                        Next ▶
                    </button>
                </div>
                <div className="calendar-grid">
                    {Array.from({ length: daysInMonth }, (_, i) => {
                        const day = i + 1;
                        const thisDate = new Date(year, month, day);

                        const isSelected = isSameDay(thisDate, selectedDate);
                        const isReserved = reservedDates.includes(day);

                        return (
                            <div
                                key={day}
                                className={`calendar-day ${isSelected ? "selected" : ""}`}
                                onClick={() => setSelectedDate(thisDate)}
                            >
                                {day}
                                {isReserved && <span className="dot" />}
                            </div>
                        );
                    })}
                </div>
            </div>
            <h3 className="sub-title">
                Reservations for{" "}
                {selectedDate.toLocaleDateString([], {
                    year: "numeric",
                    month: "short",
                    day: "numeric",
                })}
            </h3>
            <div className="reservation-list">
                {reservationsForSelectedDate.length > 0 ? (
                    reservationsForSelectedDate.map(r => (
                        <div key={r.id} className="reservation-item">
                            <div className="reservation-details">
                                <div className="detail-value">
                                    {r.customerName}
                                </div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="reservation-item">
                        <div className="reservation-details">
                            <div className="detail-value">
                                No reservations for this day
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
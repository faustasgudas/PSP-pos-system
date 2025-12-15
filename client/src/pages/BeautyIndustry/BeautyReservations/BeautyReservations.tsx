import { useState, useMemo } from "react";
import "./BeautyReservations.css";

interface Booking {
    id: number;
    customerName: string;
    customerPhone: string;
    customerEmail: string;
    appointmentStart: string;
    appointmentEnd: string;
    status: string;
    services: number[];
    employeeId: number;
    notes?: string;
}

interface Service {
    id: number;
    name: string;
    basePrice: { amount: number; currency: string };
}

interface Employee {
    id: number;
    name: string;
    role: string;
}

interface ReservationsProps {
    reservations: Booking[];
    services: Service[];
    employees: Employee[];
    goToNewBooking: () => void;
}

export default function BeautyReservations({
                                               reservations,
                                               services,
                                               employees,
                                               goToNewBooking,
                                           }: ReservationsProps) {
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

    const bookedDates = useMemo(() => {
        const days = new Set<number>();
        reservations.forEach(r => {
            const d = new Date(r.appointmentStart);
            if (d.getFullYear() === year && d.getMonth() === month) {
                days.add(d.getDate());
            }
        });
        return Array.from(days);
    }, [reservations, year, month]);

    const bookingsForSelectedDate = useMemo(() => {
        return reservations.filter(r =>
            isSameDay(new Date(r.appointmentStart), selectedDate)
        );
    }, [reservations, selectedDate]);

    return (
        <div className="reservations-container">

            <div className="action-bar">
                <h2 className="section-title">Reservation Management</h2>

                <button
                    className="btn btn-primary"
                    onClick={() => {
                        console.log("✅ NEW BOOKING BUTTON CLICKED");
                        goToNewBooking(); // 🔥 THIS MUST FIRE
                    }}
                >
                    <span>➕</span> New Booking
                </button>
            </div>

            <div className="calendar-wrapper">
                <div className="calendar-header">
                    <button
                        className="cal-nav-btn"
                        onClick={() =>
                            setCurrentMonth(new Date(year, month - 1, 1))
                        }
                    >
                        ◀ Prev
                    </button>

                    <div className="calendar-title">{monthLabel}</div>

                    <button
                        className="cal-nav-btn"
                        onClick={() =>
                            setCurrentMonth(new Date(year, month + 1, 1))
                        }
                    >
                        Next ▶
                    </button>
                </div>

                <div className="calendar-grid">
                    {Array.from({ length: daysInMonth }, (_, i) => {
                        const day = i + 1;
                        const thisDate = new Date(year, month, day);

                        const isSelected = isSameDay(thisDate, selectedDate);
                        const isBooked = bookedDates.includes(day);

                        return (
                            <div
                                key={day}
                                className={`calendar-day ${isSelected ? "selected" : ""}`}
                                onClick={() => setSelectedDate(thisDate)}
                            >
                                {day}
                                {isBooked && <span className="dot" />}
                            </div>
                        );
                    })}
                </div>
            </div>

            <h3 className="sub-title">
                Bookings for{" "}
                {selectedDate.toLocaleDateString([], {
                    year: "numeric",
                    month: "short",
                    day: "numeric",
                })}
            </h3>

            <div className="booking-list">
                {bookingsForSelectedDate.length > 0 ? (
                    bookingsForSelectedDate.map(b => (
                        <div key={b.id} className="booking-item">
                            <div className="booking-details">
                                <div className="detail-value">
                                    {b.customerName}
                                </div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="booking-item">
                        <div className="booking-details">
                            <div className="detail-value">
                                No bookings for this day
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

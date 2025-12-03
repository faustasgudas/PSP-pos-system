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
}

export default function BeautyReservations({
                                               reservations,
                                               services,
                                               employees,
                                           }: ReservationsProps) {
    const [selectedDate, setSelectedDate] = useState(new Date());
    const [currentMonth, setCurrentMonth] = useState(new Date());

    // Helpers
    const getServiceNames = (ids: number[]) =>
        ids.map(id => services.find(s => s.id === id)?.name || "Unknown").join(", ");

    const getEmployeeName = (id: number) =>
        employees.find(e => e.id === id)?.name || "Unknown";

    const formatTime = (dateString: string) =>
        new Date(dateString).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
        });

    const startOfMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1);
    const endOfMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 0);
    const monthDays = endOfMonth.getDate();

    const prevMonth = () =>
        setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));

    const nextMonth = () =>
        setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));

    const isSameDay = (a: Date, b: Date) =>
        a.getFullYear() === b.getFullYear() &&
        a.getMonth() === b.getMonth() &&
        a.getDate() === b.getDate();

    const bookedDates = useMemo(() => {
        return reservations.map(r => new Date(r.appointmentStart).getDate());
    }, [reservations]);

    // Filter reservations for selected date
    const reservationsForDay = reservations.filter(r =>
        isSameDay(new Date(r.appointmentStart), selectedDate)
    );

    return (
        <div className="reservations-container">
            <div className="action-bar">
                <h2 className="section-title">Reservations</h2>
                <button className="btn btn-primary">
                    <span>➕</span> New Booking
                </button>
            </div>

            {/* Calendar */}
            <div className="calendar-wrapper">
                <div className="calendar-header">
                    <button onClick={prevMonth} className="cal-nav-btn">◀</button>
                    <h3>
                        {currentMonth.toLocaleString("default", { month: "long" })}{" "}
                        {currentMonth.getFullYear()}
                    </h3>
                    <button onClick={nextMonth} className="cal-nav-btn">▶</button>
                </div>

                <div className="calendar-grid">
                    {Array.from({ length: monthDays }, (_, i) => {
                        const day = i + 1;
                        const thisDate = new Date(
                            currentMonth.getFullYear(),
                            currentMonth.getMonth(),
                            day
                        );

                        const isToday = isSameDay(thisDate, new Date());
                        const isSelected = isSameDay(thisDate, selectedDate);
                        const isBooked = bookedDates.includes(day);

                        return (
                            <div
                                key={day}
                                className={`calendar-day 
                                    ${isToday ? "today" : ""} 
                                    ${isSelected ? "selected" : ""}`}
                                onClick={() => setSelectedDate(thisDate)}
                            >
                                {day}
                                {isBooked && <div className="dot"></div>}
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Bookings for selected date */}
            <h3 className="sub-title">
                Bookings for {selectedDate.toLocaleDateString()}
            </h3>

            <div className="booking-list">
                {reservationsForDay.length > 0 ? (
                    reservationsForDay.map(booking => (
                        <div key={booking.id} className="booking-item">
                            <div className="booking-header">
                                <div className="booking-time">
                                    {formatTime(booking.appointmentStart)}
                                </div>
                                <div className="booking-status status-confirmed">
                                    {booking.status}
                                </div>
                            </div>

                            <div className="booking-details">
                                <div className="detail-item">
                                    <div className="detail-label">Client</div>
                                    <div className="detail-value">{booking.customerName}</div>
                                </div>

                                <div className="detail-item">
                                    <div className="detail-label">Service</div>
                                    <div className="detail-value">
                                        {getServiceNames(booking.services)}
                                    </div>
                                </div>

                                <div className="detail-item">
                                    <div className="detail-label">Employee</div>
                                    <div className="detail-value">
                                        {getEmployeeName(booking.employeeId)}
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="booking-item">
                        <div className="booking-details">
                            <div className="detail-value">No bookings for this day</div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

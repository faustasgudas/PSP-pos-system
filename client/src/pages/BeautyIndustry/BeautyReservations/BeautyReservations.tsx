import { useEffect, useMemo, useState } from "react";
import "./BeautyReservations.css";
import {
    cancelReservation,
    getReservation,
    listReservations,
    updateReservation,
    type ReservationSummary,
    type ReservationDetail,
} from "../../../frontapi/reservationsApi";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

interface ReservationsProps {
    goToNewBooking: () => void;
}

// Parse client info from notes (format: "Client: Name\nPhone: 123...")
function parseClientInfo(notes: string | null): { clientName: string; phone: string; otherNotes: string } {
    if (!notes) return { clientName: "", phone: "", otherNotes: "" };
    
    const lines = notes.split("\n");
    let clientName = "";
    let phone = "";
    const otherLines: string[] = [];
    
    for (const line of lines) {
        if (line.startsWith("Client:")) {
            clientName = line.replace("Client:", "").trim();
        } else if (line.startsWith("Phone:")) {
            phone = line.replace("Phone:", "").trim();
        } else if (line.trim()) {
            otherLines.push(line);
        }
    }
    
    return { clientName, phone, otherNotes: otherLines.join("\n") };
}

export default function BeautyReservations({
    goToNewBooking,
}: ReservationsProps) {
    const user = getUserFromToken();
    const role = user?.role ?? "";

    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);
    
    // Store detailed info for selected date bookings
    const [detailedBookings, setDetailedBookings] = useState<Map<number, ReservationDetail>>(new Map());
    const [loadingDetails, setLoadingDetails] = useState(false);

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);

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

    const serviceNameById = useMemo(() => {
        const m = new Map<number, string>();
        services.forEach((s) => m.set(s.catalogItemId, s.name));
        return m;
    }, [services]);

    const servicePriceById = useMemo(() => {
        const m = new Map<number, number>();
        services.forEach((s) => m.set(s.catalogItemId, s.basePrice));
        return m;
    }, [services]);

    const employeeNameById = useMemo(() => {
        const m = new Map<number, string>();
        employees.forEach((e: any) => {
            const id = Number(e.employeeId ?? e.id);
            if (id) m.set(id, e.name ?? `Employee ${id}`);
        });
        return m;
    }, [employees]);

    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const load = async (monthDate: Date) => {
        if (!businessId) {
            setError("Missing businessId");
            setReservations([]);
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);
        try {
            const from = new Date(monthDate.getFullYear(), monthDate.getMonth(), 1, 0, 0, 0);
            const to = new Date(monthDate.getFullYear(), monthDate.getMonth() + 1, 1, 0, 0, 0);

            const [resList, svcList, empList] = await Promise.all([
                listReservations(businessId, {
                    dateFrom: from.toISOString(),
                    dateTo: to.toISOString(),
                }),
                getActiveServices(businessId),
                fetchEmployees(businessId),
            ]);

            setReservations(Array.isArray(resList) ? resList : []);
            setServices(Array.isArray(svcList) ? svcList : []);
            setEmployees(Array.isArray(empList) ? empList : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load reservations");
            setReservations([]);
        } finally {
            setLoading(false);
        }
    };

    // Load detailed info for bookings on selected date
    const loadBookingDetails = async (bookings: ReservationSummary[]) => {
        if (!businessId || bookings.length === 0) return;
        
        setLoadingDetails(true);
        try {
            const details = await Promise.all(
                bookings.map(b => getReservation(businessId, b.reservationId))
            );
            
            const newMap = new Map<number, ReservationDetail>();
            details.forEach(d => newMap.set(d.reservationId, d));
            setDetailedBookings(newMap);
        } catch (e) {
            console.error("Failed to load booking details", e);
        } finally {
            setLoadingDetails(false);
        }
    };

    useEffect(() => {
        load(currentMonth);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    useEffect(() => {
        load(currentMonth);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentMonth]);

    // Load details when selected date changes
    useEffect(() => {
        loadBookingDetails(bookingsForSelectedDate);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedDate, reservations]);

    const canManage = role === "Owner" || role === "Manager";

    const doCancel = async (reservationId: number) => {
        if (!businessId) return;
        const ok = window.confirm(`Cancel reservation #${reservationId}?`);
        if (!ok) return;

        setError(null);
        try {
            const detail = await getReservation(businessId, reservationId);
            if (detail.orderId) {
                setError(
                    `Reservation #${reservationId} already has order #${detail.orderId}. Cancel the order to auto-cancel the reservation.`
                );
                return;
            }

            await cancelReservation(businessId, reservationId);
            await load(currentMonth);
        } catch (e: any) {
            setError(e?.message || "Cancel failed");
        }
    };

    const markCompleted = async (reservationId: number) => {
        if (!businessId) return;
        setError(null);
        try {
            await updateReservation(businessId, reservationId, { status: "Completed" });
            await load(currentMonth);
        } catch (e: any) {
            setError(e?.message || "Update failed");
        }
    };

    const getStatusClass = (status: string) => {
        switch (status) {
            case "Booked": return "status-booked";
            case "Completed": return "status-completed";
            case "Cancelled": return "status-cancelled";
            default: return "";
        }
    };

    return (
        <div className="reservations-container">

            <div className="action-bar">
                <h2 className="section-title">Reservation Management</h2>

                <button
                    className="btn btn-primary"
                    onClick={goToNewBooking}
                >
                    ➕ New Booking
                </button>
            </div>

            {error && (
                <div className="error-box">
                    <div>{error}</div>
                    {authProblem && (
                        <button
                            className="btn"
                            onClick={() => {
                                logout();
                                window.location.reload();
                            }}
                            style={{ marginTop: 10 }}
                        >
                            Log out
                        </button>
                    )}
                </div>
            )}

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
                        const isToday = isSameDay(thisDate, new Date());

                        return (
                            <div
                                key={day}
                                className={`calendar-day ${isSelected ? "selected" : ""} ${isToday ? "today" : ""}`}
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
                    weekday: "long",
                    year: "numeric",
                    month: "long",
                    day: "numeric",
                })}
                <span className="booking-count">({bookingsForSelectedDate.length} bookings)</span>
            </h3>

            {loading || loadingDetails ? (
                <div className="loading-message">Loading bookings...</div>
            ) : bookingsForSelectedDate.length > 0 ? (
                <div className="bookings-table-wrap">
                    <table className="bookings-table">
                        <thead>
                            <tr>
                                <th>Time</th>
                                <th>Client</th>
                                <th>Phone</th>
                                <th>Service</th>
                                <th>Employee</th>
                                <th>Duration</th>
                                <th>Price</th>
                                <th>Status</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {bookingsForSelectedDate
                                .slice()
                                .sort(
                                    (a, b) =>
                                        new Date(a.appointmentStart).getTime() -
                                        new Date(b.appointmentStart).getTime()
                                )
                                .map((b) => {
                                    const detail = detailedBookings.get(b.reservationId);
                                    const clientInfo = parseClientInfo(detail?.notes ?? null);
                                    
                                    return (
                                        <tr key={b.reservationId}>
                                            <td className="time-cell">
                                                {new Date(b.appointmentStart).toLocaleTimeString([], {
                                                    hour: "2-digit",
                                                    minute: "2-digit",
                                                })}
                                            </td>
                                            <td className="client-cell">
                                                {clientInfo.clientName || <span className="muted">—</span>}
                                            </td>
                                            <td>
                                                {clientInfo.phone || <span className="muted">—</span>}
                                            </td>
                                            <td className="service-cell">
                                                {serviceNameById.get(b.catalogItemId) ?? `Service ${b.catalogItemId}`}
                                            </td>
                                            <td>
                                                {employeeNameById.get(b.employeeId) ?? <span className="muted">Not assigned</span>}
                                            </td>
                                            <td>
                                                {b.plannedDurationMin} min
                                            </td>
                                            <td>
                                                €{(servicePriceById.get(b.catalogItemId) ?? 0).toFixed(2)}
                                            </td>
                                            <td>
                                                <span className={`status-badge ${getStatusClass(b.status)}`}>
                                                    {b.status}
                                                </span>
                                            </td>
                                            <td className="actions-cell">
                                                {canManage && b.status === "Booked" && (
                                                    <>
                                                        <button 
                                                            className="btn btn-sm btn-success" 
                                                            onClick={() => markCompleted(b.reservationId)}
                                                        >
                                                            Complete
                                                        </button>
                                                        <button 
                                                            className="btn btn-sm btn-danger" 
                                                            onClick={() => doCancel(b.reservationId)}
                                                        >
                                                            Cancel
                                                        </button>
                                                    </>
                                                )}
                                                {b.status !== "Booked" && (
                                                    <span className="muted">—</span>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })}
                        </tbody>
                    </table>
                </div>
            ) : (
                <div className="no-bookings">
                    No bookings for this day
                </div>
            )}
        </div>
    );
}

import { Routes, Route, Navigate } from "react-router-dom";
import PaymentSuccess from "./pages/BeautyIndustry/BeautyPayments/PaymentSuccess";
import PaymentCancel from "./pages/BeautyIndustry/BeautyPayments/PaymentCancel";
import MainApp from "./MainApp";

export default function App() {
    return (
        <Routes>
            {/* Stripe return pages */}
            <Route path="/payments/success" element={<PaymentSuccess />} />
            <Route path="/payments/cancel" element={<PaymentCancel />} />

            {/* visa tavo aplikacija */}
            <Route path="/*" element={<MainApp />} />

            {/* fallback */}
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
}

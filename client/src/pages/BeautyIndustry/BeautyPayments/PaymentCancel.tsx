import { useNavigate } from "react-router-dom";

export default function PaymentCancel() {
    const navigate = useNavigate();

    return (
        <div style={styles.box}>
            <h2>❌ Payment cancelled</h2>
            <p>The payment was cancelled.</p>

            <button style={styles.btn} onClick={() => navigate("/", { replace: true })}>
                Back to home
            </button>
        </div>
    );
}

const styles: Record<string, React.CSSProperties> = {
    box: {
        maxWidth: 420,
        margin: "80px auto",
        padding: 24,
        borderRadius: 12,
        background: "#fff",
        boxShadow: "0 10px 30px rgba(0,0,0,.15)",
        textAlign: "center",
    },
    btn: {
        marginTop: 16,
        padding: "10px 14px",
        borderRadius: 10,
        border: "1px solid rgba(0,0,0,0.12)",
        background: "#111827",
        color: "#fff",
        cursor: "pointer",
    },
};

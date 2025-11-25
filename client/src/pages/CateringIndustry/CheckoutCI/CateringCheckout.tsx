import "../../../App.css"
import { useState } from "react";

function CateringCheckout() {
    // @ts-ignore
    const [apiMessage, setApiMessage] = useState("Loading from API...");
    const [selectedOption, setSelectedOption] = useState("");
    // @ts-ignore
    const [amount, setAmount] = useState(0.00);
    // @ts-ignore
    const [totalAmount, setTotalAmount] = useState(0.00);

    //todo: add split payments
    return (
        <div className="content-box">
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
            </div>
            <div className="checkout">
                <h2 className="checkout-text">Checkout</h2>
                <p>Current total: {amount}€</p>
                <p>Enter gift card code (optional):</p>
                <input type="text"></input>
                <p>Add tip (optional):</p>
                <input type="number">€</input>
                <p>Final calculated amount: {totalAmount}</p>
                <p>Select payment method:</p>
                <select
                    id="payment-method"
                    value={selectedOption}
                    onChange={(e) => setSelectedOption(e.currentTarget.value)}
                    className="payment-method-dropdown"
                >
                    <option value="">Select payment method</option>
                    <option value="cash">Cash</option>
                    <option value="card">Card</option>
                </select>
                <button>Cancel</button>
                <button>Continue</button>
            </div>
        </div>    
    )
}

export default CateringCheckout;
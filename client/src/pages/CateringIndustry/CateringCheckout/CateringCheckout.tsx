import "../../../App.css"
import "./CateringCheckout.css"
import { useState } from "react";
import PayCardCI from "../PayCardCI/CateringPayCard";
import PayCashCI from "../PayCashCI/CateringPayCash";

function CateringCheckout() {
    // @ts-ignore
    const [apiMessage, setApiMessage] = useState("Loading from API...");
    // @ts-ignore
    const [amount, setAmount] = useState(0.00);
    // @ts-ignore
    const [totalAmount, setTotalAmount] = useState(0.00);
    
    const goToCash = () => {
        return <PayCashCI />
    }
    
    const goToCard = () => {
        return <PayCardCI />
    }

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
                <p>Final calculated amount: {totalAmount}€</p>
                <p>Select payment method:</p>
                <button>Cancel</button>
                <button onClick={goToCard}>Pay by card</button>
                <button onClick={goToCash}>Pay by cash</button>
            </div>
        </div>    
    )
}

export default CateringCheckout;
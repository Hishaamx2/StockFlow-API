let authToken = null;
let conversationHistory = [];
let authMode = "login";

const loginTab = document.getElementById("login-tab");
const registerTab = document.getElementById("register-tab");
const usernameInput = document.getElementById("username-input");
const passwordInput = document.getElementById("password-input");
const authSubmitButton = document.getElementById("auth-submit-button");
const loginStatus = document.getElementById("login-status");

const askButton = document.getElementById("ask-button");
const questionInput = document.getElementById("question-input");
const chatLog = document.getElementById("chat-log");

function setAuthMode(mode) {
    authMode = mode;
    loginTab.classList.toggle("active", mode === "login");
    registerTab.classList.toggle("active", mode === "register");
    authSubmitButton.textContent = mode === "login" ? "Log In" : "Register";
}

loginTab.addEventListener("click", () => setAuthMode("login"));
registerTab.addEventListener("click", () => setAuthMode("register"));

async function authenticate() {
    const endpoint = authMode === "login" ? "/api/auth/login" : "/api/auth/register";
    const failMessage = authMode === "login"
        ? "Login failed. Check your username/password."
        : "Registration failed. Username may already be taken.";

    const response = await fetch(endpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            username: usernameInput.value,
            password: passwordInput.value
        })
    });

    if (!response.ok) {
        loginStatus.textContent = failMessage;
        return;
    }

    const data = await response.json();
    authToken = data.token;
    loginStatus.textContent = "Logged in!";
}

authSubmitButton.addEventListener("click", authenticate);

async function askQuestion(question) {
    if (question === "") return;

    addMessage(question, "user");
    questionInput.value = "";

    if (!authToken) {
        addMessage("Please log in first.", "ai");
        return;
    }

    const response = await fetch("/api/query", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + authToken
        },
        body: JSON.stringify({ question: question, history: conversationHistory })
    });

    if (!response.ok) {
        addMessage("Something went wrong asking that.", "ai");
        return;
    }

    const data = await response.json();
    addMessage(data.answer, "ai");

    conversationHistory.push({ question: question, answer: data.answer });
    if (conversationHistory.length > 10) {
        conversationHistory = conversationHistory.slice(-10);
    }
}

askButton.addEventListener("click", () => askQuestion(questionInput.value.trim()));

const inventoryContainer = document.getElementById("inventory-container");

async function loadInventory() {
    const [warehousesResponse, itemsResponse] = await Promise.all([
        fetch("/api/warehouses"),
        fetch("/api/items")
    ]);

    if (!warehousesResponse.ok || !itemsResponse.ok) {
        inventoryContainer.textContent = "Could not load inventory.";
        return;
    }

    const warehouses = await warehousesResponse.json();
    const items = await itemsResponse.json();

    inventoryContainer.innerHTML = "";

    warehouses.forEach(warehouse => {
        const warehouseItems = items.filter(item => item.warehouseId === warehouse.id);

        const group = document.createElement("div");
        group.className = "warehouse-group";

        const heading = document.createElement("h3");
        heading.textContent = `${warehouse.name} (${warehouse.location})`;
        group.appendChild(heading);

        if (warehouseItems.length === 0) {
            const empty = document.createElement("p");
            empty.className = "no-items";
            empty.textContent = "No items in this warehouse.";
            group.appendChild(empty);
        } else {
            const list = document.createElement("ul");
            warehouseItems.forEach(item => {
                const row = document.createElement("li");
                row.className = "item-row";

                if (item.quantity < item.reorderThreshold) {
                    row.classList.add("low-stock");
                }

                row.textContent = `${item.name} — SKU ${item.sku} — qty ${item.quantity}`;
                list.appendChild(row);
            });
            group.appendChild(list);
        }

        inventoryContainer.appendChild(group);
    });
}

loadInventory();

function addMessage(text, sender) {
    const messageDiv = document.createElement("div");
    messageDiv.className = "message " + sender;

    if (sender === "ai") {
        const icon = document.createElement("img");
        icon.src = "robot.svg";
        icon.alt = "StockBot";
        icon.className = "bot-icon-message";
        messageDiv.appendChild(icon);
    }

    messageDiv.appendChild(document.createTextNode(text));

    chatLog.appendChild(messageDiv);
    chatLog.scrollTop = chatLog.scrollHeight;
}

addMessage(
    "Hi, I'm StockBot. Ask me anything about your inventory, item counts, specific products, warehouses, low stock, whatever you're curious about.",
    "ai"
);

let authToken = null;

const loginButton = document.getElementById("login-button");
const registerButton = document.getElementById("register-button");
const usernameInput = document.getElementById("username-input");
const passwordInput = document.getElementById("password-input");
const loginStatus = document.getElementById("login-status");

const askButton = document.getElementById("ask-button");
const questionInput = document.getElementById("question-input");
const chatLog = document.getElementById("chat-log");

async function authenticate(endpoint, failMessage) {
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

loginButton.addEventListener("click", () =>
    authenticate("/api/auth/login", "Login failed. Check your username/password."));

registerButton.addEventListener("click", () =>
    authenticate("/api/auth/register", "Registration failed. Username may already be taken."));

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
        body: JSON.stringify({ question: question })
    });

    if (!response.ok) {
        addMessage("Something went wrong asking that.", "ai");
        return;
    }

    const data = await response.json();
    addMessage(formatResult(data), "ai");
}

askButton.addEventListener("click", () => askQuestion(questionInput.value.trim()));

document.querySelectorAll(".suggestion-chip").forEach(chip => {
    chip.addEventListener("click", () => askQuestion(chip.textContent));
});

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

function formatResult(data) {
    if (data.items.length === 0) {
        return "No items matched that.";
    }

    return data.items
        .map(item => `${item.name} (SKU ${item.sku}) — qty ${item.quantity}`)
        .join("\n");
}

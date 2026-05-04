import * as signalR from "@microsoft/signalr";

// Replace the port number with your actual C# running port
const HUB_URL = "https://localhost:7203/chatHub";

const connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL)
    .withAutomaticReconnect() // Auto-reconnect if the server drops
    .configureLogging(signalR.LogLevel.Information)
    .build();

export const startConnection = async () => {
    try {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            await connection.start();
            console.log("SignalR Connected Successfully!");
        }
    } catch (error) {
        console.error("SignalR Connection Error: ", error);
        setTimeout(startConnection, 5000); // Retry connection after 5 seconds
    }
};

export default connection;
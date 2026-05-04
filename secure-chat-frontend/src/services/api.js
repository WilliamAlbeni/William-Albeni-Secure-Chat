import axios from 'axios';

const API_BASE_URL = 'https://localhost:7203/api'; 

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json'
    }
});

// We can add interceptors here later when we implement JWT authentication
export const registerUser = async (userData) => {
    try {
        const response = await apiClient.post('/Auth/register', userData);
        return response.data;
    } catch (error) {
        console.error("Registration failed:", error.response?.data || error.message);
        throw error;
    }
};

export const loginUser = async (credentials) => {
    try {
        const response = await apiClient.post('/Auth/login', credentials);
        return response.data;
    } catch (error) {
        console.error("Login failed:", error.response?.data || error.message);
        throw error;
    }
};

export const searchUser = async (username) => {
    try {
        const response = await apiClient.get(`/Users/search/${username}`);
        return response.data;
    } catch (error) {
        console.error("User search failed:", error.response?.data || error.message);
        throw error;
    }
};

export const getChatHistory = async (user1Id, user2Id) => {
    try {
        const response = await apiClient.get(`Chat/history?user1Id=${user1Id}&user2Id=${user2Id}`);
        return response.data;
    } catch (error) {
        console.error("Failed to load history", error);
        return [];
    }
};

// Fetch the list of users I have chatted with before
export const getChatContacts = async (userId) => {
    try {
        const response = await apiClient.get(`/Chat/contacts?userId=${userId}`);
        return response.data;
    } catch (error) {
        console.error("Failed to load contacts from API", error);
        return [];
    }
};

// Function to tell the server we read the messages
export const markChatAsRead = async (senderId, myId) => {
    try {
        await apiClient.post(`/Chat/mark-read?senderId=${senderId}&myId=${myId}`);
    } catch (error) {
        console.error("Failed to mark messages as read", error);
    }
};

export default apiClient;
import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { searchUser, getChatHistory, getChatContacts } from '../services/api'; // تمت إزالة markChatAsRead لأننا نستخدم SignalR الآن
import connection, { startConnection } from '../services/signalrService';

export default function ChatRoom() {
    const navigate = useNavigate();
    
    // User session data
    const currentUsername = localStorage.getItem('currentUsername');
    const currentUserId = localStorage.getItem('currentUserId');

    // UI States
    const [searchQuery, setSearchQuery] = useState('');
    const [searchError, setSearchError] = useState('');
    const [activeContact, setActiveContact] = useState(null); 
    const [messages, setMessages] = useState([]); 
    const [messageInput, setMessageInput] = useState('');
    const [contactsList, setContactsList] = useState([]);

    // Ref to track the currently open chat for SignalR
    const activeContactRef = useRef(null);

    useEffect(() => {
        activeContactRef.current = activeContact;
    }, [activeContact]);

    // ---------------------------------------------------------
    // 1. SignalR Connection & Listening
    // ---------------------------------------------------------
    useEffect(() => {
        if (!currentUserId) {
            navigate('/'); 
            return;
        }

        const initChat = async () => {
            await startConnection();
            try {
                await connection.invoke("RegisterUser", currentUserId);
            } catch (err) {
                console.error("Failed to register user:", err);
            }
        };

        initChat();

        // Listen for new incoming messages
        connection.on("ReceiveMessage", (messageDto) => {
            const senderId = messageDto.senderId || messageDto.SenderId;
            const messageText = messageDto.text || messageDto.Text;
            
            const deliveryStatus = messageDto.deliveryStatus || messageDto.DeliveryStatus || 2; 

            if (activeContactRef.current && activeContactRef.current.id === senderId) {
                // If chat is open, show the message immediately
                setMessages(prev => [...prev, { 
                    senderId: senderId, 
                    content: messageText, 
                    isMine: false,
                    deliveryStatus: deliveryStatus
                }]);

                // Tell server immediately we read it via SignalR (Blue ticks)
                connection.invoke("NotifyMessagesRead", senderId, currentUserId)
                    .catch(err => console.error("Error notifying read:", err));
            } else {
                // If chat is closed, increment the unread badge
                setContactsList(prevContacts => {
                    const existingContact = prevContacts.find(c => c.id === senderId);
                    if (existingContact) {
                        return prevContacts.map(c => 
                            c.id === senderId ? { ...c, unreadCount: (c.unreadCount || 0) + 1 } : c
                        );
                    } else {
                        return [...prevContacts, { id: senderId, username: "New Message", unreadCount: 1 }];
                    }
                });
            }
        });

        // NEW: Listen for "Delivered" status (2 Gray Ticks) from the backend SendPrivateMessage
        connection.on("MessageDeliveredToDevice", (deliveredMessageId) => {
            setMessages(prev => prev.map(m => 
                (m.isMine && m.deliveryStatus === 1) ? { ...m, deliveryStatus: 2 } : m
            ));
        });

        // NEW: Listen for "Read" status (2 Blue Ticks) from the other user
        connection.on("MessagesReadByDevice", (readerId) => {
            if (activeContactRef.current && activeContactRef.current.id === readerId) {
                setMessages(prev => prev.map(m => 
                    (m.isMine && m.deliveryStatus < 3) ? { ...m, deliveryStatus: 3 } : m
                ));
            }
        });

        return () => {
            connection.off("ReceiveMessage");
            connection.off("MessageDeliveredToDevice");
            connection.off("MessagesReadByDevice");
        };
    }, [currentUserId, navigate]);

    // ---------------------------------------------------------
    // 2. Fetch Chat History & Contacts
    // ---------------------------------------------------------
    useEffect(() => {
        const loadHistory = async () => {
            if (activeContact && currentUserId) {
                const history = await getChatHistory(currentUserId, activeContact.id);
                const formattedHistory = history.map(msg => ({
                    senderId: msg.senderId || msg.SenderId,
                    content: msg.text || msg.Text || msg.OriginalPayload, 
                    isMine: (msg.senderId || msg.SenderId).toLowerCase() === currentUserId.toLowerCase(),
                    deliveryStatus: msg.deliveryStatus ?? msg.DeliveryStatus ?? 1 
                }));
                setMessages(formattedHistory);
            }
        };
        loadHistory();
    }, [activeContact, currentUserId]);

    useEffect(() => {
        const loadContactsFromDatabase = async () => {
            if (currentUserId) {
                const contacts = await getChatContacts(currentUserId);
                const formattedContacts = contacts.map(c => ({
                    id: c.id || c.Id,
                    username: c.username || c.Username,
                    publicKey: c.publicKey || c.PublicKey,
                    unreadCount: c.unreadCount || c.UnreadCount || 0
                }));
                setContactsList(formattedContacts);
            }
        };
        loadContactsFromDatabase();
    }, [currentUserId]);

    // ---------------------------------------------------------
    // 3. UI Interactions
    // ---------------------------------------------------------
    const handleSelectContact = (contact) => {
        setActiveContact(contact);
        setMessages([]); 
        
        // Reset local badge counter
        setContactsList(prev => prev.map(c => 
            c.id === contact.id ? { ...c, unreadCount: 0 } : c
        ));

        // Tell backend to change DeliveryStatus to 3 (Read) via SignalR
        // We check if there's an unread count OR if there are any unread messages loaded
        if (contact.unreadCount > 0) {
            connection.invoke("NotifyMessagesRead", contact.id, currentUserId)
                .catch(err => console.error("Error notifying read:", err));
        }
    };

    const handleSearch = async (e) => {
        e.preventDefault();
        setSearchError('');
        try {
            const user = await searchUser(searchQuery);
            if (user) {
                const newContact = { id: user.id || user.Id, username: user.username || user.Username, publicKey: user.publicKey, unreadCount: 0 };
                setContactsList(prev => {
                    if (!prev.find(c => c.id === newContact.id)) return [...prev, newContact];
                    return prev;
                });
                handleSelectContact(newContact);
                setSearchQuery('');
            }
        } catch (error) {
            setSearchError("User not found.");
        }
    };

    const handleSendMessage = async (e) => {
        e.preventDefault();
        if (!messageInput.trim() || !activeContact) return;

        try {
            // Optimistically show message on screen with status 1 (Sent / 1 tick)
            setMessages(prev => [...prev, { 
                senderId: currentUserId, 
                content: messageInput, 
                isMine: true, 
                deliveryStatus: 1 
            }]);
            
            await connection.invoke("SendPrivateMessage", currentUserId, activeContact.id, messageInput);
            setMessageInput('');
        } catch (error) {
            console.error("Failed to send message: ", error);
        }
    };

    const handleLogout = () => {
        localStorage.clear();
        navigate('/');
    };

    // ---------------------------------------------------------
    // Helper: Render WhatsApp style ticks based on Enum values
    // 1 = Sent, 2 = Delivered, 3 = Read
    // ---------------------------------------------------------
    const renderTicks = (status) => {
        if (status === 1) return <span style={styles.tickSent}>✓</span>;           // 1 gray tick
        if (status === 2) return <span style={styles.tickDelivered}>✓✓</span>;      // 2 gray ticks
        if (status === 3) return <span style={styles.tickRead}>✓✓</span>;           // 2 blue ticks
        return null;
    };

    return (
        <div style={styles.container}>
            <div style={styles.sidebar}>
                <div style={styles.sidebarHeader}>
                    <h3>Welcome, {currentUsername}</h3>
                    <button onClick={handleLogout} style={styles.logoutBtn}>Logout</button>
                </div>
                
                <form onSubmit={handleSearch} style={styles.searchForm}>
                    <input 
                        type="text" 
                        placeholder="Find user to chat..." 
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        style={styles.searchInput}
                    />
                    <button type="submit" style={styles.searchBtn}>Find</button>
                </form>
                {searchError && <p style={styles.errorText}>{searchError}</p>}

                <div style={styles.contactsContainer}>
                    {contactsList.map(contact => (
                        <div 
                            key={contact.id} 
                            onClick={() => handleSelectContact(contact)}
                            style={{
                                ...styles.contactItem, 
                                backgroundColor: activeContact?.id === contact.id ? '#ebebeb' : '#ffffff'
                            }}
                        >
                            <span style={styles.contactName}>{contact.username}</span>
                            {contact.unreadCount > 0 && (
                                <span style={styles.badge}>{contact.unreadCount}</span>
                            )}
                        </div>
                    ))}
                </div>
            </div>

            <div style={styles.chatArea}>
                {!activeContact ? (
                    <div style={styles.emptyChat}>
                        <h2>Select a chat to view messages</h2>
                    </div>
                ) : (
                    <>
                        <div style={styles.chatHeader}>
                            <strong>{activeContact.username}</strong>
                        </div>
                        
                        <div style={styles.messagesContainer}>
                            {messages.map((msg, index) => (
                                <div key={index} style={msg.isMine ? styles.myMessageWrapper : styles.theirMessageWrapper}>
                                    <div style={msg.isMine ? styles.myMessageBubble : styles.theirMessageBubble}>
                                        {msg.content}
                                        
                                        {/* Show delivery ticks ONLY if the message is mine */}
                                        {msg.isMine && (
                                            <div style={styles.ticksContainer}>
                                                {renderTicks(msg.deliveryStatus)}
                                            </div>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                        
                        <form onSubmit={handleSendMessage} style={styles.messageForm}>
                            <input 
                                type="text" 
                                placeholder="Type a message..." 
                                value={messageInput}
                                onChange={(e) => setMessageInput(e.target.value)}
                                style={styles.messageInput}
                            />
                            <button type="submit" style={styles.sendBtn}>Send</button>
                        </form>
                    </>
                )}
            </div>
        </div>
    );
}

// Inline Styles
const styles = {
    container: { display: 'flex', height: '100vh', backgroundColor: '#e5ddd5', fontFamily: 'Arial, sans-serif' },
    sidebar: { width: '320px', backgroundColor: '#ffffff', borderRight: '1px solid #ccc', display: 'flex', flexDirection: 'column' },
    sidebarHeader: { padding: '1rem', backgroundColor: '#f0f2f5', borderBottom: '1px solid #ccc', display: 'flex', justifyContent: 'space-between', alignItems: 'center' },
    logoutBtn: { backgroundColor: '#dc3545', color: 'white', border: 'none', padding: '5px 10px', borderRadius: '4px', cursor: 'pointer' },
    searchForm: { padding: '1rem', display: 'flex', gap: '5px', borderBottom: '1px solid #eee' },
    searchInput: { flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #ccc' },
    searchBtn: { padding: '8px', backgroundColor: '#28a745', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' },
    errorText: { color: 'red', fontSize: '0.8rem', paddingLeft: '1rem', marginTop: '-10px' },
    
    contactsContainer: { flex: 1, overflowY: 'auto' },
    contactItem: { padding: '15px', borderBottom: '1px solid #f0f0f0', display: 'flex', justifyContent: 'space-between', alignItems: 'center', cursor: 'pointer', transition: 'background-color 0.2s' },
    contactName: { fontWeight: 'bold', color: '#333' },
    badge: { backgroundColor: '#25D366', color: 'white', borderRadius: '50%', padding: '2px 8px', fontSize: '0.8rem', fontWeight: 'bold' },
    
    chatArea: { flex: 1, display: 'flex', flexDirection: 'column' },
    chatHeader: { padding: '15px 20px', backgroundColor: '#f0f2f5', borderBottom: '1px solid #ccc', fontSize: '1.2rem', color: '#333' },
    emptyChat: { flex: 1, display: 'flex', justifyContent: 'center', alignItems: 'center', color: '#888' },
    messagesContainer: { flex: 1, padding: '2rem', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '10px' },
    
    myMessageWrapper: { display: 'flex', justifyContent: 'flex-end' },
    theirMessageWrapper: { display: 'flex', justifyContent: 'flex-start' },
    myMessageBubble: { backgroundColor: '#dcf8c6', padding: '10px 15px', borderRadius: '15px 15px 0 15px', maxWidth: '60%', boxShadow: '0 1px 1px rgba(0,0,0,0.1)', display: 'flex', flexDirection: 'column' },
    theirMessageBubble: { backgroundColor: '#ffffff', padding: '10px 15px', borderRadius: '15px 15px 15px 0', maxWidth: '60%', boxShadow: '0 1px 1px rgba(0,0,0,0.1)' },
    
    // Ticks styling
    ticksContainer: { alignSelf: 'flex-end', fontSize: '0.7rem', marginTop: '4px', marginLeft: '10px' },
    tickSent: { color: '#999' },
    tickDelivered: { color: '#999', letterSpacing: '-2px' },
    tickRead: { color: '#53bdeb', letterSpacing: '-2px' }, // Blue color for read

    messageForm: { display: 'flex', padding: '1rem', backgroundColor: '#f0f2f5' },
    messageInput: { flex: 1, padding: '12px', borderRadius: '24px', border: 'none', outline: 'none', fontSize: '1rem' },
    sendBtn: { marginLeft: '10px', padding: '10px 20px', backgroundColor: '#007bff', color: 'white', border: 'none', borderRadius: '24px', cursor: 'pointer', fontWeight: 'bold' }
};
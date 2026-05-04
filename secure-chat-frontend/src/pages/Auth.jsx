import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { registerUser, loginUser } from '../services/api';
import { generateRSAKeyPair } from '../services/cryptoService';

export default function Auth() {
    const [isLogin, setIsLogin] = useState(true);
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [errorMessage, setErrorMessage] = useState('');
    
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setErrorMessage('');

        try {
            if (isLogin) {
                
                const response = await loginUser({ username, password });
                
                localStorage.setItem('currentUserId', response.userId);
                localStorage.setItem('currentUsername', username);
                
                navigate('/chat');
            } else {
                
                
                // key pair generation (RSA)
                const keys = await generateRSAKeyPair();

                
                const response = await registerUser({ 
                    username: username, 
                    password: password, 
                    publicKey: keys.publicKey 
                });

                // saving private key in user's device
                localStorage.setItem('userPrivateKey', keys.privateKey);
                
                // saving session data
                localStorage.setItem('currentUserId', response.userId);
                localStorage.setItem('currentUsername', username);
                
                navigate('/chat');
            }
        } catch (error) {
            
            if (error.response && error.response.status === 401) {
                setErrorMessage("Invalid username or password.");
            } else if (error.response && error.response.status === 400) {
                
                if (error.response.data.errors) {
                    const firstError = Object.values(error.response.data.errors)[0][0];
                    setErrorMessage(firstError);
                } else {
                    setErrorMessage(error.response.data.Message || "Invalid input.");
                }
            } else {
                setErrorMessage("Connection error. Is the server running?");
            }
        }
    };

    return (
        <div style={styles.container}>
            <div style={styles.card}>
                <h2 style={styles.title}>{isLogin ? 'Sign In' : 'Sign Up'}</h2>
                
                {errorMessage && <p style={styles.errorText}>{errorMessage}</p>}
                
                <form onSubmit={handleSubmit} style={styles.form}>
                    <input 
                        type="text" 
                        placeholder="Username" 
                        value={username} 
                        onChange={(e) => setUsername(e.target.value)} 
                        required 
                        style={styles.input}
                    />
                    <input 
                        type="password" 
                        placeholder="Password" 
                        value={password} 
                        onChange={(e) => setPassword(e.target.value)} 
                        required 
                        style={styles.input}
                    />
                    <button type="submit" style={styles.submitBtn}>
                        {isLogin ? 'Login' : 'Create Account'}
                    </button>
                </form>

                <button 
                    onClick={() => {
                        setIsLogin(!isLogin);
                        setErrorMessage('');
                    }} 
                    style={styles.toggleBtn}
                >
                    {isLogin ? "Don't have an account? Sign Up" : "Already have an account? Sign In"}
                </button>
            </div>
        </div>
    );
}

const styles = {
    container: { display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', backgroundColor: '#f0f2f5' },
    card: { padding: '2rem', backgroundColor: '#fff', borderRadius: '8px', boxShadow: '0 4px 12px rgba(0,0,0,0.1)', width: '350px', textAlign: 'center' },
    title: { marginBottom: '1.5rem', color: '#333', fontFamily: 'Arial, sans-serif' },
    form: { display: 'flex', flexDirection: 'column', gap: '1rem' },
    input: { padding: '0.8rem', borderRadius: '4px', border: '1px solid #ccc', fontSize: '1rem' },
    submitBtn: { padding: '0.8rem', backgroundColor: '#007bff', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '1rem', fontWeight: 'bold' },
    toggleBtn: { marginTop: '1rem', background: 'none', border: 'none', color: '#007bff', cursor: 'pointer', textDecoration: 'underline' },
    errorText: { color: '#d93025', backgroundColor: '#fde7e9', padding: '0.5rem', borderRadius: '4px', marginBottom: '1rem', fontSize: '0.9rem' }
};
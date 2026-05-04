import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Auth from './pages/Auth';
import ChatRoom from './pages/ChatRoom';

function App() {
  return (
    <Router>
      <Routes>
        {/* Default route points to the Auth page */}
        <Route path="/" element={<Auth />} />
        
        {/* Route for the real-time chat room */}
        <Route path="/chat" element={<ChatRoom />} />
        
        {/* Redirect any unknown path to the default route */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
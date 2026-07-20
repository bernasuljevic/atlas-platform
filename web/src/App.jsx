import { useState } from "react";
import Login from "./components/Login";
import WikiBoard from "./components/WikiBoard";

function App() {
  const [token, setToken] = useState(null);

  if (!token) {
    return <Login onLoginSuccess={setToken} />;
  }

  return <WikiBoard token={token} onLogout={() => setToken(null)} />;
}

export default App;
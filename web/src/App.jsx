import { useState } from "react";
import Login from "./components/Login";
import WikiBoard from "./components/WikiBoard";

function App() {
  // token null ise "giriş yapılmamış" demek - App, hangi ekranı göstereceğine
  // sadece bu tek değişkene bakarak karar veriyor. Bu, backend'deki
  // ICurrentUserAccessor.IsAuthenticated mantığının React karşılığı.
  const [token, setToken] = useState(null);

  if (!token) {
    return <Login onLoginSuccess={setToken} />;
  }

  return <WikiBoard token={token} />;
}

export default App;
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import Login from "./components/Login";
import WikiBoard from "./components/WikiBoard";

function App() {
  const [token, setToken] = useState(null);

  useEffect(() => {
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/hubs/notifications", {
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .build();
    // Backend'deki WikiPageCreatedEventHandler'ın gönderdiği "WikiPageCreated"
    // adlı mesajı dinliyoruz - isim BİREBİR eşleşmeli, aksi halde hiçbir şey olmaz
    // (sessizce yok sayılır, hata da vermez - bu yüzden isim tutarlılığı önemli).
    connection.on("WikiPageCreated", (data) => {
      console.log("[SignalR] Yeni bildirim:", data);
      alert(`Yeni wiki sayfası eklendi: "${data.title}" (${data.departmentName})`);
    });

    connection
      .start()
      .then(() => console.log("[SignalR] Bağlantı kuruldu:", connection.connectionId))
      .catch((err) => console.error("[SignalR] Bağlantı hatası:", err));

    return () => {
      connection.stop();
    };
  }, [token]);

  if (!token) {
    return <Login onLoginSuccess={setToken} />;
  }

  return <WikiBoard token={token} onLogout={() => setToken(null)} />;
}

export default App;
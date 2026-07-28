import { useEffect } from "react";
import { Navigate, Outlet } from "react-router";
import * as signalR from "@microsoft/signalr";
import { toast } from "sonner";
import { useAuth } from "../context/AuthContext";

// Bu component iki işi birden yapıyor:
// 1. Giriş yapılmamışsa /login'e yönlendirir (asıl "korumalı route" görevi).
// 2. SignalR bağlantısını kurar - bu bağlantı sadece giriş yapılmışken anlamlı
//    olduğu için, eskiden App.jsx'te "if (!token) return" ile korunan aynı
//    useEffect'i buraya taşıdık. Route'lara bölününce bu mantığın App.jsx'te
//    değil, "korumalı alan"ı temsil eden bu component'te yaşaması daha tutarlı.
function ProtectedRoute() {
  const { token } = useAuth();

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
    // alert() eskiden akışı tamamen kilitleyen (kullanıcı "Tamam"a basana kadar
    // sayfayla hiçbir etkileşim kurulamayan) çirkin bir tarayıcı popup'ıydı -
    // sonner'ın toast'u aynı bilgiyi kesintisiz, kendiliğinden kapanan bir
    // bildirimle veriyor.
    connection.on("WikiPageCreated", (data) => {
      console.log("[SignalR] Yeni bildirim:", data);
      toast(`Yeni wiki sayfası eklendi: "${data.title}"`, {
        description: data.departmentName,
      });
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
    return <Navigate to="/login" replace />;
  }

  // Outlet: bu route'un altındaki gerçek sayfayı (örn. /wiki) render eder.
  return <Outlet />;
}

export default ProtectedRoute;

import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import { Share2, Wand2, X } from "lucide-react";
import {
  createPasswordEntry,
  getPasswordEntryById,
  getVaultEntryShares,
  removeVaultEntryShare,
  revealPasswordEntry,
  shareVaultEntry,
  updatePasswordEntry,
} from "../api";
import { generatePassword } from "../passwordGenerator";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { Button } from "@atlas/ui/button";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Textarea } from "@atlas/ui/textarea";

// WikiEditorPage'deki AYNI "tek bileşen hem yeni hem düzenle" deseni. Fark:
// düzenleme modunda parola alanı BOŞ başlıyor (var olan şifreyi göstermeden) -
// kullanıcı ya yeni bir parola yazar (değiştirilir) ya da boş bırakır
// (UpdatePasswordEntryCommandHandler'daki kural: boşsa mevcut şifrelenmiş
// değer AYNEN korunur, yeniden şifrelenmez). Mevcut parolayı görmek isteyen
// "Göster" değil, listedeki reveal akışını kullanmalı - burada göstermemek
// bilinçli, "düzenleme formu açıkken omuzun üstünden bakan biri" riskini
// azaltıyor.
function VaultEntryFormPage({ token }) {
  const { id: entryId } = useParams();
  const isEditMode = Boolean(entryId);
  const navigate = useNavigate();
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [title, setTitle] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [url, setUrl] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState("");
  const [notes, setNotes] = useState("");
  // Vault paylaşım modeli (D grubu, Gün 3) - sadece "Paylaş" panelini
  // göstermek/gizlemek İÇİN DEĞİL, formun KENDİSİNİ salt-okunur yapmak için
  // de gerekiyor: VaultPage.jsx'in satır tıklaması artık owner-or-Admin
  // olmayan bir kullanıcıyı buraya HİÇ getirmiyor (bkz. oradaki düzeltme),
  // ama bu sayfaya doğrudan URL ile gelinmesi hâlâ mümkün - burada da AYNI
  // kuralı uygulamak "PUT'u dene, 403 al" gibi kötü bir UX'ten kaçınıyor.
  const [ownerUserId, setOwnerUserId] = useState(null);

  const [isLoadingExisting, setIsLoadingExisting] = useState(isEditMode);
  const [isSaving, setIsSaving] = useState(false);
  const [isRevealingCurrent, setIsRevealingCurrent] = useState(false);
  const [error, setError] = useState(null);

  const canManage = !isEditMode || isAdmin || ownerUserId === userId;

  const [shares, setShares] = useState([]);
  const [isLoadingShares, setIsLoadingShares] = useState(false);
  const [newShareEmail, setNewShareEmail] = useState("");
  const [isSharing, setIsSharing] = useState(false);
  const [shareError, setShareError] = useState(null);
  const [removingShareUserId, setRemovingShareUserId] = useState(null);

  useEffect(() => {
    if (!isEditMode) return;
    let cancelled = false;

    getPasswordEntryById(token, entryId)
      .then((entry) => {
        if (cancelled) return;
        setTitle(entry.title);
        setUsername(entry.username ?? "");
        setUrl(entry.url ?? "");
        setDescription(entry.description ?? "");
        setCategory(entry.category ?? "");
        setNotes(entry.notes ?? "");
        setOwnerUserId(entry.createdByUserId);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setIsLoadingExisting(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEditMode, entryId, token]);

  // Paylaşım listesi AYRI bir effect - "entry yüklenemese/görülemeşe bile
  // (404/403) paylaşım isteği hiç atılmasın" (WikiArticlePage'in favori/pin
  // effect'indeki AYNI desen), VE sadece owner-or-Admin biliniyor olduktan
  // SONRA çalışsın diye `ownerUserId` state'ine bağlı.
  useEffect(() => {
    if (!isEditMode || ownerUserId === null) return;
    if (!isAdmin && ownerUserId !== userId) return; // paylaşılan bir kullanıcı bu listeyi göremez (backend zaten 404 döner)

    let cancelled = false;
    setIsLoadingShares(true);
    getVaultEntryShares(token, entryId)
      .then((result) => {
        if (!cancelled) setShares(result);
      })
      .catch(() => {
        // Sessizce boş bırak - paylaşım listesi görüntülenemezse kaydın
        // asıl formu (başlık/parola vb.) hiç engellenmemeli.
      })
      .finally(() => {
        if (!cancelled) setIsLoadingShares(false);
      });

    return () => {
      cancelled = true;
    };
  }, [isEditMode, entryId, token, ownerUserId, isAdmin, userId]);

  async function handleAddShare(e) {
    e.preventDefault();
    if (!newShareEmail.trim()) return;

    setIsSharing(true);
    setShareError(null);
    try {
      await shareVaultEntry(token, entryId, newShareEmail.trim());
      setNewShareEmail("");
      const refreshed = await getVaultEntryShares(token, entryId);
      setShares(refreshed);
      toast("Kayıt paylaşıldı");
    } catch (err) {
      setShareError(err.message);
    } finally {
      setIsSharing(false);
    }
  }

  async function handleRemoveShare(sharedWithUserId) {
    setRemovingShareUserId(sharedWithUserId);
    try {
      await removeVaultEntryShare(token, entryId, sharedWithUserId);
      setShares((prev) => prev.filter((s) => s.sharedWithUserId !== sharedWithUserId));
    } catch (err) {
      toast("Paylaşım kaldırılamadı", { description: err.message });
    } finally {
      setRemovingShareUserId(null);
    }
  }

  function handleGeneratePassword() {
    setPassword(generatePassword(16));
  }

  // Düzenleme formunda mevcut parolayı doğrudan göstermek yerine, kullanıcı
  // isterse buradan da reveal edip alana yükleyebilir (ör. "biraz değiştirip
  // güncellemek" istediğinde baştan yazmak zorunda kalmasın) - bu da GetById
  // gibi sessiz değil, reveal endpoint'i üzerinden geçtiği için audit'e
  // "Revealed" olarak düşüyor, listedeki "Göster"le birebir aynı sonuç.
  async function handleLoadCurrentPassword() {
    setIsRevealingCurrent(true);
    try {
      const current = await revealPasswordEntry(token, entryId);
      setPassword(current);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsRevealingCurrent(false);
    }
  }

  async function handleSave(e) {
    e.preventDefault();
    if (!canManage) return; // savunma amaçlı - Kaydet düğmesi zaten disabled

    setError(null);
    setIsSaving(true);

    try {
      const payload = {
        title,
        username: username.trim() || null,
        password: password || null,
        url: url.trim() || null,
        description: description.trim() || null,
        category: category.trim() || null,
        notes: notes.trim() || null,
      };

      if (isEditMode) {
        await updatePasswordEntry(token, entryId, payload);
        navigate("/vault");
      } else {
        if (!password) {
          setError("Yeni bir kayıt için parola gerekli.");
          setIsSaving(false);
          return;
        }
        await createPasswordEntry(token, payload);
        navigate("/vault");
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoadingExisting) {
    return <p style={{ color: "var(--text)" }}>Yükleniyor...</p>;
  }

  return (
    <div className="mx-auto max-w-xl text-left">
      <h1 className="mb-6 text-2xl font-medium" style={{ color: "var(--text-h)" }}>
        {isEditMode ? "Kaydı Düzenle" : "Yeni Kasa Kaydı"}
      </h1>

      {/* VaultPage.jsx'in satır tıklaması artık owner-or-Admin olmayanı buraya
          hiç GETİRMİYOR (bkz. oradaki düzeltme) - bu banner SADECE doğrudan
          URL ile gelinen (ör. paylaşım e-postasındaki bir link, ya da eski
          bir sekme) durumlar için bir güvenlik ağı. */}
      {isEditMode && !canManage && (
        <p
          className="mb-4 rounded-lg border px-3 py-2 text-sm"
          style={{ borderColor: "var(--border)", color: "var(--text)", background: "var(--code-bg)" }}
        >
          Bu kayıt seninle paylaşıldı - görüntüleyip parolasını açabilirsin, ama düzenleyemez/silemezsin.
        </p>
      )}

      <form onSubmit={handleSave} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-title">Başlık</Label>
          <Input
            id="vault-title"
            placeholder="ör. Şirket GitHub Organizasyonu"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            disabled={isSaving || !canManage}
            required
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-username">Kullanıcı Adı</Label>
          <Input
            id="vault-username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            disabled={isSaving || !canManage}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-password">
            Parola {isEditMode && <span className="font-normal opacity-60">(boş bırakırsan değişmez)</span>}
          </Label>
          <div className="flex gap-2">
            <Input
              id="vault-password"
              type="text"
              className="font-mono"
              placeholder={isEditMode ? "••••••••" : ""}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isSaving || !canManage}
            />
            <Button type="button" variant="outline" onClick={handleGeneratePassword} disabled={isSaving} title="Rastgele parola üret">
              <Wand2 size={15} /> Üret
            </Button>
            {isEditMode && (
              <Button
                type="button"
                variant="outline"
                onClick={handleLoadCurrentPassword}
                disabled={isSaving || isRevealingCurrent}
              >
                {isRevealingCurrent ? "..." : "Mevcudu Göster"}
              </Button>
            )}
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-url">URL</Label>
          <Input
            id="vault-url"
            placeholder="https://..."
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            disabled={isSaving || !canManage}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-category">Kategori</Label>
          <Input
            id="vault-category"
            placeholder="ör. Sunucular, SaaS, Veritabanı"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            disabled={isSaving || !canManage}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-description">Açıklama</Label>
          <Input
            id="vault-description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={isSaving || !canManage}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-notes">Notlar</Label>
          <Textarea
            id="vault-notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={isSaving || !canManage}
            className="min-h-24"
          />
        </div>

        {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}

        <div className="flex gap-2">
          <Button
            type="submit"
            disabled={isSaving || !canManage}
            className="text-white hover:opacity-90"
            style={{ background: "var(--brand-accent)" }}
          >
            {isSaving ? "Kaydediliyor..." : isEditMode ? "Kaydet" : "Oluştur"}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate("/vault")}>
            Vazgeç
          </Button>
        </div>
      </form>

      {/* Vault paylaşım modeli (D grubu, Gün 3) - SADECE owner-or-Admin
          görüyor (backend zaten GetPasswordEntrySharesQuery'de aynı kuralı
          uyguluyor, burada AYRICA gizlemek gereksiz bir isteği önlüyor). */}
      {isEditMode && canManage && (
        <div className="mt-6 flex flex-col gap-3 rounded-lg border p-4" style={{ borderColor: "var(--border)" }}>
          <h2 className="flex items-center gap-1.5 text-sm font-semibold" style={{ color: "var(--text-h)" }}>
            <Share2 size={15} /> Paylaşılanlar
          </h2>

          <form onSubmit={handleAddShare} className="flex gap-2">
            <Input
              type="email"
              placeholder="ör. arkadas@atlas.local"
              value={newShareEmail}
              onChange={(e) => setNewShareEmail(e.target.value)}
              disabled={isSharing}
              className="flex-1"
            />
            <Button type="submit" variant="outline" disabled={isSharing || !newShareEmail.trim()}>
              {isSharing ? "Paylaşılıyor..." : "Paylaş"}
            </Button>
          </form>
          {shareError && <p style={{ color: "red" }} className="text-xs">{shareError}</p>}

          {isLoadingShares ? (
            <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
              Yükleniyor...
            </p>
          ) : shares.length === 0 ? (
            <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
              Bu kayıt henüz kimseyle paylaşılmadı.
            </p>
          ) : (
            <ul className="flex flex-col gap-1.5">
              {shares.map((s) => (
                <li
                  key={s.sharedWithUserId}
                  className="flex items-center justify-between gap-2 rounded border px-2.5 py-1.5 text-sm"
                  style={{ borderColor: "var(--border)" }}
                >
                  <span className="min-w-0 truncate" style={{ color: "var(--text-h)" }}>
                    {s.sharedWithEmail ?? "Bilinmiyor"}
                    <span className="ml-1.5 text-xs font-normal" style={{ color: "var(--text)", opacity: 0.6 }}>
                      · {formatUtcTimestamp(s.sharedAtUtc)}
                    </span>
                  </span>
                  <button
                    type="button"
                    onClick={() => handleRemoveShare(s.sharedWithUserId)}
                    disabled={removingShareUserId === s.sharedWithUserId}
                    title="Paylaşımı kaldır"
                    aria-label="Paylaşımı kaldır"
                    className="shrink-0 rounded p-1 hover:bg-[var(--brand-accent)]/10"
                  >
                    <X size={13} style={{ color: "var(--text)", opacity: 0.6 }} />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

export default VaultEntryFormPage;

import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { Wand2 } from "lucide-react";
import {
  createPasswordEntry,
  getPasswordEntryById,
  revealPasswordEntry,
  updatePasswordEntry,
} from "../api";
import { generatePassword } from "../passwordGenerator";
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

  const [title, setTitle] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [url, setUrl] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState("");
  const [notes, setNotes] = useState("");

  const [isLoadingExisting, setIsLoadingExisting] = useState(isEditMode);
  const [isSaving, setIsSaving] = useState(false);
  const [isRevealingCurrent, setIsRevealingCurrent] = useState(false);
  const [error, setError] = useState(null);

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

      <form onSubmit={handleSave} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-title">Başlık</Label>
          <Input
            id="vault-title"
            placeholder="ör. Şirket GitHub Organizasyonu"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            disabled={isSaving}
            required
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-username">Kullanıcı Adı</Label>
          <Input
            id="vault-username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            disabled={isSaving}
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
              disabled={isSaving}
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
            disabled={isSaving}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-category">Kategori</Label>
          <Input
            id="vault-category"
            placeholder="ör. Sunucular, SaaS, Veritabanı"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            disabled={isSaving}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-description">Açıklama</Label>
          <Input
            id="vault-description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={isSaving}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vault-notes">Notlar</Label>
          <Textarea
            id="vault-notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={isSaving}
            className="min-h-24"
          />
        </div>

        {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}

        <div className="flex gap-2">
          <Button
            type="submit"
            disabled={isSaving}
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
    </div>
  );
}

export default VaultEntryFormPage;

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import WikiSearch from "./WikiSearch";
import { searchByMeaning } from "../api";

// Bu dosya, saf-mantık testlerinden (dateUtils/jwt/passwordGenerator) FARKLI
// bir şeyi kanıtlamak için var: Vitest+RTL kurulumunun gerçek bir React
// component'ini jsdom'da RENDER edip, kullanıcı etkileşimini (yazma + submit)
// simüle edip DOM'u DOĞRU güncellediğini gösteriyor - "test altyapısı" görevi
// sadece pure-function testleriyle kanıtlanmış sayılmazdı.
vi.mock("../api", () => ({
  searchByMeaning: vi.fn(),
}));

// react-router'ın useNavigate'i gerçek bir <Router> context'i olmadan
// çöker - WikiSearch'ü her testte MemoryRouter ile sarmalıyoruz.
function renderWithRouter(ui) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe("WikiSearch", () => {
  beforeEach(() => {
    searchByMeaning.mockReset();
  });

  it("bir wiki sayfası ve bir belge sonucunu kaynak tipine göre AYRIŞTIRIP gösterir", async () => {
    // Backend'in SearchByMeaningQueryHandler'ının döndürdüğü
    // SemanticSearchResultDto şekliyle birebir - sourceType/resourceId
    // ayrımının component tarafında doğru okunduğunu kanıtlıyoruz.
    searchByMeaning.mockResolvedValue([
      {
        sourceType: "WikiPage",
        resourceId: "wiki-1",
        title: "Sunucu Bakım Prosedürü",
        departmentName: "IT",
        chunkText: "Sunucular her ayın ilk pazartesi günü bakıma alınır.",
        score: 0.87,
        createdAtUtc: "2026-08-01T10:00:00Z",
      },
      {
        sourceType: "Document",
        resourceId: "doc-1",
        title: "Bakım Kılavuzu.pdf",
        departmentName: "IT",
        chunkText: "Bakım öncesi yedekleme adımları...",
        score: 0.52,
        createdAtUtc: "2026-08-02T10:00:00Z",
      },
    ]);

    const user = userEvent.setup();
    renderWithRouter(<WikiSearch token="fake-token" />);

    await user.type(screen.getByPlaceholderText(/anlamına göre ara/i), "sunucu bakımı");
    await user.click(screen.getByRole("button", { name: "Ara" }));

    await waitFor(() => expect(searchByMeaning).toHaveBeenCalledTimes(1));
    expect(searchByMeaning).toHaveBeenCalledWith(
      "fake-token",
      "sunucu bakımı",
      5,
      { fromUtc: "", toUtc: "" },
    );

    expect(await screen.findByText("Sunucu Bakım Prosedürü")).toBeInTheDocument();
    expect(screen.getByText("Bakım Kılavuzu.pdf")).toBeInTheDocument();
    // Skor 0-1 aralığından yüzdeye çevriliyor (bkz. WikiSearch.jsx'teki not).
    expect(screen.getByText("%87")).toBeInTheDocument();
    expect(screen.getByText("%52")).toBeInTheDocument();
  });

  it("sonuç yoksa boş-durum mesajını gösterir", async () => {
    searchByMeaning.mockResolvedValue([]);

    const user = userEvent.setup();
    renderWithRouter(<WikiSearch token="fake-token" />);

    await user.type(screen.getByPlaceholderText(/anlamına göre ara/i), "alakasız bir sorgu");
    await user.click(screen.getByRole("button", { name: "Ara" }));

    expect(
      await screen.findByText(/eşleşen \(ve görebileceğin\) bir sayfa ya da belge bulunamadı/i),
    ).toBeInTheDocument();
  });

  it("API hata döndürürse hata mesajını gösterir, sonuç listesi render ETMEZ", async () => {
    searchByMeaning.mockRejectedValue(new Error("Sunucu hatası"));

    const user = userEvent.setup();
    renderWithRouter(<WikiSearch token="fake-token" />);

    await user.type(screen.getByPlaceholderText(/anlamına göre ara/i), "her şey");
    await user.click(screen.getByRole("button", { name: "Ara" }));

    expect(await screen.findByText("Sunucu hatası")).toBeInTheDocument();
    expect(
      screen.queryByText(/eşleşen \(ve görebileceğin\)/i),
    ).not.toBeInTheDocument();
  });

  it("boş sorgu ile submit edilirse aramayı hiç TETİKLEMEZ (backend'e boş istek gitmesin diye)", () => {
    renderWithRouter(<WikiSearch token="fake-token" />);

    // Buton zaten disabled (queryText boş) - form submit'i de tetiklenmemeli.
    expect(screen.getByRole("button", { name: "Ara" })).toBeDisabled();
    expect(searchByMeaning).not.toHaveBeenCalled();
  });
});

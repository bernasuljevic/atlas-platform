// Her test dosyasından ÖNCE (vite.config.js'teki test.setupFiles) çalışıyor -
// jest-dom'un toBeInTheDocument()/toHaveTextContent() gibi ek matcher'larını
// Vitest'in expect()'ine ekliyor.
import "@testing-library/jest-dom";

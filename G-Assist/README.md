# G-Assist — AI Gaming Assistant
> Audi RS tarzı siyah/kırmızı/beyaz overlay asistan

## Kurulum

### Gereksinimler
- Visual Studio 2022
- .NET 8.0 SDK
- Windows 10/11

### Adımlar

1. `GAssist.sln` dosyasını Visual Studio 2022 ile aç
2. Build → Restore NuGet Packages
3. `Release` modunda Build et (F5 veya Ctrl+F5)
4. `bin/Release/net8.0-windows/` klasörüne git
5. `apikey.txt` adında bir dosya oluştur, içine sadece Anthropic API anahtarını yaz:
   ```
   sk-ant-xxxxxxxxxxxxxxxx
   ```
6. `GAssist.exe` çalıştır

## Kullanım

| Kısayol | Açıklama |
|---------|----------|
| `Ctrl+Shift+G` | Overlay'i aç/gizle |
| `Enter` | Mesaj gönder |
| `Shift+Enter` | Yeni satır |

### Butonlar
- **➤ GÖNDER** — Metin mesajı gönder
- **📷 EKRAN** — Ekran görüntüsü al ve Claude'a analiz ettir

## Özellikler
- ✅ Sağ alt köşede overlay (TopMost)
- ✅ Ctrl+Shift+G global kısayol
- ✅ Ekran görüntüsü → Claude Vision analizi
- ✅ Sohbet geçmişi
- ✅ Sistem tepsisi ikonu
- ✅ Sürükleyerek taşıma
- ✅ Audi RS tema (siyah/kırmızı/beyaz)

## API Anahtarı
https://console.anthropic.com/account/keys adresinden ücretsiz alabilirsin.

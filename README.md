# Template2D

Unity 6.2 (6000.2.6f1) · 2D · Built-in Render Pipeline · dikey ekran mobil oyun şablonu.

## Başlarken
1. Projeyi aç. Paketler kendiliğinden iner (Spine ilk açılışta birkaç dakika sürebilir).
2. `Tools > Project > Build Sample Content` çalıştır. İlk seferde TMP kaynakları içe aktarılır, komutu bir kez daha çalıştır.
3. Herhangi bir sahnede Play'e bas. Oyun her zaman Boot'tan başlar.

## Klasörler
```
Assets/
  Art/{Sprites,Atlases,Spine,Animations,Materials,Particles,Fonts}
  Audio/  Configs/  Prefabs/  Scenes/{Boot,Home,Game}
  Scripts/{Core,Common,Contexts}   bağımlılık yönü: Contexts → Common → Core
  Editor/
```

## Sistemler
| Sistem | Yer |
|---|---|
| Boot ve servis kurulumu | `Contexts/Application/Bootstrapper`, `Core/Services` |
| Sahne geçişi (additive + yükleme ekranı) | `Core/SceneLoading`, `Contexts/Application/GameFlow` |
| Event bus | `Core/Events` |
| Pooling (config'ten warm/max) | `Core/Pooling` |
| Kayıt ve ayarlar | `Core/Save`, `Core/Settings` |
| Ses | `Core/Audio` |
| State machine | `Core/StateMachine` |
| Yerelleştirme | `Core/Localization`, `Common/UI/LocalizedText` |
| Haptik | `Core/Haptics` |
| Kamera uyumu, safe area | `Core/CameraFit`, `Common/UI/SafeArea` |
| Popup ve UI yardımcıları | `Common/UI` |
| Tween kalıpları (PrimeTween) | `Common/Tween` |
| Debug paneli (F1 / üç parmak) | `Core/Debugging` |

## Editor araçları (`Tools > Project`)
- **Always Start From Boot**: Play hangi sahnede basılırsa basılsın oyun Boot'tan başlar.
- **Pool Monitor**: Play sırasında havuz doluluğunu gösterir.
- **Build Sample Content**: Örnek Boot → Home → Game akışını yeniden oluşturur.
- **Project Setup / Apply 2D Mobile Defaults**: Gamma, dikey ekran, IL2CPP, ARM64, sorting layer'lar.

## Paketler
- **PrimeTween 1.5.2**: npm registry üzerinden kurulur.
- **Spine 4.3** (spine-csharp 4.3.42, spine-unity 4.3.120): Git commit'ine sabitli.
  - Export Spine Editor **4.3** ile yapılır. Dosyalar `Assets/Art/Spine/<Ad>/` altına konur.
  - Her kullanıcının kendi Spine lisansı olmalı.
- **Unity MCP 10.3.0**: Sadece Editor'da çalışan geliştirme aracı, build'e girmez.

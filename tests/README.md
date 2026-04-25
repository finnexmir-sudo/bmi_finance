# FinNex Test Layihələri

Bu qovluq avtomatlaşdırılmış testləri saxlayır.

## Layihələr

### `FinNex.Application.Tests`
Application qatının (servis-lər, biznes məntiqi) unit və inteqrasiya testləri.

**Qovluq quruluşu:**
```
FinNex.Application.Tests/
├── Infrastructure/      ← test infrastrukturu (smoke, helper-lər)
├── Maas/                ← MaasHesablamaService testləri (məzuniyyət, vergi, HYS, xəstəlik)
├── Workflow/            ← İcazə + Məzuniyyət təsdiq axını testləri (5 qayda)
└── Helpers/             ← test data builder-ləri, fake repo-lar
```

## Necə işlədilir

### Visual Studio
- `Test → Run All Tests` (Ctrl+R, A)
- Yan paneldə `Test Explorer` açılır
- Yaşıl ✓ = uğurlu, qırmızı ✗ = uğursuz

### Komanda satırı
```bash
# Bütün testlər
dotnet test

# Yalnız Application tests
dotnet test tests/FinNex.Application.Tests

# Müəyyən bir test sinfi
dotnet test --filter "FullyQualifiedName~SmokeTests"

# Coverage ilə
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## İstifadə olunan paketlər

| Paket | Niyə |
|-------|------|
| **xUnit** | Test framework, .NET sənaye standartı |
| **FluentAssertions** | `result.Should().Be(...)` oxunaqlı yoxlamalar |
| **NSubstitute** | Repo və servis interfeyslərinin "fake"-lənməsi |
| **EFCore.InMemory** | Real DB-yə dəymədən inteqrasiya testi |
| **coverlet** | Code coverage ölçümü |

## Test yazma konvensiyaları

### Adlandırma
Test metodları `Metod_Vəziyyət_Gözlənilən` formatında:
```csharp
[Fact]
public async Task FerdiHesabla_RehberMezuniyyetdedirse_KesintiTetbiqOlunmamalidir()
```

### Struktur (AAA pattern)
```csharp
[Fact]
public async Task ...()
{
    // Arrange — test data hazırla
    var input = ...;

    // Act — yoxlanılan metodu çağır
    var result = await _service.MetodAsync(input);

    // Assert — gözlənilən nəticəni yoxla
    result.Data.Should().NotBeNull();
}
```

### Real DB istifadə etmə
- **Unit test** — DB lazımdırsa, `Substitute.For<IRepository<T>>()` ilə fake.
- **Inteqrasiya testi** — real EF Core davranışı lazımdırsa, `EFCore.InMemory` ilə.
- **Heç vaxt** real SQL Server-ə qoşulma — testlər izolə olmalıdır.

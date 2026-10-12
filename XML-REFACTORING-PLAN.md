# XML Data Layer Refactoring Plan

## Table of Contents
1. [Problem Statement](#1-problem-statement)
2. [Current State Audit](#2-current-state-audit)
3. [Phase 2 — Centralised Cached Loader](#3-phase-2--centralised-cached-loader)
4. [Phase 3 — Repository Pattern](#4-phase-3--repository-pattern)
5. [Migration Sequence](#5-migration-sequence)
6. [Test Strategy Updates](#6-test-strategy-updates)
7. [Risks & Mitigations](#7-risks--mitigations)

---

## 1. Problem Statement

Every HTTP request into the BP API currently results in at least one synchronous disk read, a full XML
parse, and immediate GC pressure because the parsed document is discarded at the end of the request.
The same three-line block is copy-pasted 30+ times across nine services:

```csharp
if (!File.Exists(_xmlFilePath))
    throw new FileNotFoundException($"… not found: {_xmlFilePath}");
var doc = XDocument.Load(_xmlFilePath);
```

Beyond raw performance, this pattern has structural problems:

| Problem | Impact |
|---|---|
| No caching | Every request hits the disk, even for read-only data that never changes |
| Duplicated I/O guard | Bug in the guard (e.g. wrong message, missing check) must be fixed in 30+ places |
| Mixed concerns | Each service method does I/O, querying, and model mapping in one block |
| Tight coupling to the file system | Unit tests must create real temp directories and XML files |
| No abstraction over data source | Replacing XML with a database or external API requires rewriting every service method |
| Mutable-file race potential | `XDocument` is loaded, mutated in memory, and saved — but if a cache exists elsewhere, it holds a stale copy |

---

## 2. Current State Audit

### 2.1 Service → XML file mapping

| Service class | XML file(s) read | XML file(s) written | Load helper? |
|---|---|---|---|
| `AccountManagementService` | `users.xml` | `users.xml` (invite, update, delete) | `Load()` helper ✅ but also inline mutation methods ❌ |
| `BillingService` | `billing.xml` | — | `Load()` helper ✅ |
| `BrokerHomeService` | `broker-home-metrics.xml`, `broker-renewal-watchlist.xml`, `broker-open-support-requests.xml` | — | Three separate helpers ✅ |
| `BrokerRenewalsService` | `broker-renewals.xml` | — | `LoadXml()` helper ✅ |
| `ContractService` | `contracts.xml`, `contract-history.xml`, `contracts-table.xml`, `contracts-renew.xml`, `contracts-meters.xml`, `renewals.xml`, `pricing-intelligence.xml`, `pricing-trend.xml`, `pricing-snapshot-details.xml` | `formal-quotes.xml` | Two helpers for 2 files ⚠️; inline for remaining 8 ❌ |
| `NotificationService` | `notifications.xml` | — | No helper — fully inline ❌ |
| `SupportCaseService` | `support-cases.xml`, `request-types.xml`, `case-chats.xml` | `support-cases.xml`, `case-chats.xml` | Helper for `support-cases.xml` ✅; inline for other 2 ❌ |
| `UsageService` | `usage.xml`, `contract-usage.xml`, `usage-trend.xml` | — | Helper for `usage.xml` ✅; inline for other 2 ❌ |
| `UserService` | `users.xml` | `users.xml` (update profile, reset password) | `ReadUsersFromXml()` helper ✅ but inline in mutation methods ❌ |

**Totals:**
- 25 distinct XML files
- ~30+ `File.Exists` checks
- ~25+ `XDocument.Load` calls
- 9 services, all registered `AddScoped` (no caching between requests)
- **5 services write to XML files** (cache-invalidation must be handled)

### 2.2 Mutable files (require cache invalidation strategy)

| File | Written by | Operation |
|---|---|---|
| `formal-quotes.xml` | `ContractService.SubmitFormalQuote` | Appends new quote element |
| `support-cases.xml` | `SupportCaseService.CreateCase` | Appends new case element |
| `case-chats.xml` | `SupportCaseService` chat methods | Appends new chat message |
| `users.xml` | `UserService` (update, reset password) | Updates user elements |
| `users.xml` | `AccountManagementService` (invite, update, delete) | Adds / updates / removes user elements |

### 2.3 Registration in `Program.cs` (current)

All services are registered `AddScoped` — one instance per request, no shared state:

```csharp
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IBillingService, BillingService>();
// ... all nine services the same way
```

`IMemoryCache` is not registered at all.

---

## 3. Phase 2 — Centralised Cached Loader

### 3.1 Why

Phase 2 eliminates the duplication and adds caching with the smallest possible structural change.
Every service keeps its existing public interface. The only diff per service is:

1. Constructor gains one `IXmlDataLoader loader` parameter.
2. Private `Load…()` helpers are replaced by calls to `_loader.Load(path)`.
3. Mutation methods call `_loader.Evict(path)` after saving the file.

No controller, no model, no test fixture, no DI registration (other than adding the loader) needs
to change.

### 3.2 Design

#### New folder / namespace: `BP/Infrastructure/`

```
BP/
└── Infrastructure/
    ├── IXmlDataLoader.cs
    └── CachedXmlDataLoader.cs
```

#### `IXmlDataLoader` interface

```csharp
// BP/Infrastructure/IXmlDataLoader.cs
namespace BP.Infrastructure;

/// <summary>
/// Abstracts XML file loading so services are decoupled from the file system.
/// Implementations may cache the parsed document.
/// </summary>
public interface IXmlDataLoader
{
    /// <summary>
    /// Returns the parsed XDocument for <paramref name="filePath"/>, from cache if available.
    /// Throws <see cref="FileNotFoundException"/> if the file does not exist.
    /// </summary>
    XDocument Load(string filePath);

    /// <summary>
    /// Removes <paramref name="filePath"/> from the cache.
    /// Must be called by any service method that writes to the file.
    /// </summary>
    void Evict(string filePath);
}
```

#### `CachedXmlDataLoader` implementation

```csharp
// BP/Infrastructure/CachedXmlDataLoader.cs
using Microsoft.Extensions.Caching.Memory;
using System.Xml.Linq;

namespace BP.Infrastructure;

public sealed class CachedXmlDataLoader : IXmlDataLoader
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;

    // TTL is kept short (60 s) because files are updated at runtime by mutation methods.
    // Read-heavy, rarely-mutated files effectively stay in cache between requests.
    public CachedXmlDataLoader(IMemoryCache cache, IConfiguration config)
    {
        _cache = cache;
        _ttl = TimeSpan.FromSeconds(
            config.GetValue<int>("XmlCache:TtlSeconds", defaultValue: 60));
    }

    public XDocument Load(string filePath)
    {
        return _cache.GetOrCreate(filePath, entry =>
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Data file not found: {filePath}");

            entry.SlidingExpiration = _ttl;
            return XDocument.Load(filePath);
        })!;
    }

    public void Evict(string filePath) => _cache.Remove(filePath);
}
```

> **Why `SlidingExpiration` instead of `AbsoluteExpiration`?**  
> Read-only files are accessed on every request; sliding expiration keeps them warm indefinitely
> under normal load. Writable files are explicitly evicted after each mutation, so the TTL is
> only a safety-net fallback for them.

#### `appsettings.json` addition

```json
"XmlCache": {
  "TtlSeconds": 60
}
```

Set `TtlSeconds: 0` in `appsettings.Development.json` to disable caching during active development
(forces a fresh read on every request so XML changes are immediately visible).

#### `Program.cs` changes

```csharp
// Add these two lines alongside the existing service registrations
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IXmlDataLoader, CachedXmlDataLoader>();
// All existing AddScoped<...> calls stay unchanged
```

`IXmlDataLoader` is registered `AddSingleton` because the cache it wraps must survive across
request scopes. `IMemoryCache` (registered by `AddMemoryCache`) is also a singleton.

### 3.3 Service-by-service migration

#### `BillingService` — simplest case (read-only, single file)

**Before:**
```csharp
public class BillingService : IBillingService
{
    private readonly string _xmlFilePath;

    public BillingService(IWebHostEnvironment env)
    {
        _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "billing.xml");
    }

    private XDocument Load()
    {
        if (!File.Exists(_xmlFilePath))
            throw new FileNotFoundException($"Billing data file not found: {_xmlFilePath}");
        return XDocument.Load(_xmlFilePath);
    }

    public BillingSummary GetSummary()
    {
        var s = Load().Root!.Element("Summary")!;
        // ...
    }
}
```

**After:**
```csharp
public class BillingService : IBillingService
{
    private readonly string _xmlFilePath;
    private readonly IXmlDataLoader _loader;

    public BillingService(IWebHostEnvironment env, IXmlDataLoader loader)
    {
        _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "billing.xml");
        _loader = loader;
    }

    // Load() helper is removed entirely — _loader.Load() used directly

    public BillingSummary GetSummary()
    {
        var s = _loader.Load(_xmlFilePath).Root!.Element("Summary")!;
        // ...
    }
}
```

**Changes:** remove `Load()`, add `IXmlDataLoader` constructor param, replace every `Load()` call
with `_loader.Load(_xmlFilePath)`.

---

#### `NotificationService` — no helper today (inline fix + loader)

**Before:** inline `File.Exists + XDocument.Load` directly inside `GetNotifications()`.

**After:** same pattern as `BillingService` — inject `IXmlDataLoader`, call `_loader.Load(_xmlFilePath)`.

---

#### `BrokerRenewalsService` — single file, read-only

Replace the private `LoadXml()` helper with `_loader.Load(_xmlFilePath)` at each call site.
Remove the helper method. Add `IXmlDataLoader loader` constructor parameter.

---

#### `BrokerHomeService` — three read-only files, three helpers

Replace `LoadMetricsXml()`, `LoadRenewalWatchlistXml()`, `LoadOpenSupportRequestsXml()` with direct
`_loader.Load(_metricsFilePath)` etc. calls. Remove all three helpers. Add loader to constructor.

---

#### `UsageService` — three files, one has a helper

Replace `LoadXml()` helper and the two inline blocks with `_loader.Load(path)` calls at each site.
Add loader to constructor.

---

#### `SupportCaseService` — two mutable files

Mutable files: `support-cases.xml`, `case-chats.xml`.

```csharp
// After saving the file, evict from cache:
doc.Save(_xmlFilePath);
_loader.Evict(_xmlFilePath);   // ← new line required after every Save()
```

All three files use `_loader.Load(path)`. The two helpers (`LoadXmlDoc`, `LoadXml`) are removed.
Add loader to constructor.

---

#### `UserService` — single mutable file, multiple mutation methods

`users.xml` is read in every method and written in update/reset-password methods.

```csharp
// Every mutation method ends with:
doc.Save(_xmlFilePath);
_loader.Evict(_xmlFilePath);   // ← required after every Save()
```

Replace `ReadUsersFromXml()` helper and all inline `File.Exists + XDocument.Load` blocks with
`_loader.Load(_xmlFilePath)`. Add loader to constructor.

---

#### `AccountManagementService` — delegates to `UserService` but also accesses `users.xml` directly

Same mutable-file treatment as `UserService`. After any `doc.Save(...)`:

```csharp
doc.Save(_userxmlFilePath);
_loader.Evict(_userxmlFilePath);
```

Remove `Load()` helper. Add loader to constructor.

---

#### `ContractService` — most complex (9 files, 1 mutable)

Has two helpers today (`LoadXml`, `LoadContractsTableXml`) and seven inline load blocks.

**Strategy:** consolidate all into a single private helper that forwards to `_loader`:

```csharp
// One tiny wrapper so call sites keep the same name
private XDocument LoadFile(string path) => _loader.Load(path);
```

All existing call sites change from:
```csharp
if (!File.Exists(_xmlFilePathXxx))
    throw new FileNotFoundException(...);
var doc = XDocument.Load(_xmlFilePathXxx);
```
to:
```csharp
var doc = LoadFile(_xmlFilePathXxx);
```

For the mutable `formal-quotes.xml`, after `doc.Save(...)`:
```csharp
doc.Save(_xmlFilePathFormalQuotes);
_loader.Evict(_xmlFilePathFormalQuotes);
```

---

### 3.4 Summary of changes per file

| File | Action |
|---|---|
| `BP/Infrastructure/IXmlDataLoader.cs` | **Create** |
| `BP/Infrastructure/CachedXmlDataLoader.cs` | **Create** |
| `BP/Program.cs` | Add `AddMemoryCache()` + `AddSingleton<IXmlDataLoader>` |
| `BP/appsettings.json` | Add `XmlCache.TtlSeconds` |
| `BP/appsettings.Development.json` | Add `XmlCache.TtlSeconds: 0` (disable caching in dev) |
| `BP/Services/BillingService.cs` | Inject loader, remove `Load()`, replace call sites |
| `BP/Services/NotificationService.cs` | Inject loader, remove inline block, replace call sites |
| `BP/Services/BrokerRenewalsService.cs` | Inject loader, remove `LoadXml()`, replace call sites |
| `BP/Services/BrokerHomeService.cs` | Inject loader, remove 3 helpers, replace call sites |
| `BP/Services/UsageService.cs` | Inject loader, remove `LoadXml()`, inline→loader for other 2 |
| `BP/Services/SupportCaseService.cs` | Inject loader, remove helpers, add `Evict` after writes |
| `BP/Services/UserService.cs` | Inject loader, remove `ReadUsersFromXml()`, add `Evict` after writes |
| `BP/Services/AccountManagementService.cs` | Inject loader, remove `Load()`, add `Evict` after writes |
| `BP/Services/ContractService.cs` | Inject loader, add `LoadFile()` thin wrapper, add `Evict` after write |

**Total files touched:** 14 (2 new, 12 modified)  
**Lines of code removed:** ~90 (all `File.Exists + throw + XDocument.Load` blocks)

### 3.5 Testing after Phase 2

Unit tests use a new `TestXmlDataLoader` in `BP.Tests/Helpers/`:

```csharp
// BP.Tests/Helpers/TestXmlDataLoader.cs
using System.Xml.Linq;
using BP.Infrastructure;

namespace BP.Tests.Helpers;

/// <summary>
/// In-memory IXmlDataLoader for unit tests.
/// No temp files needed — service fixture populates this directly.
/// </summary>
public sealed class TestXmlDataLoader : IXmlDataLoader
{
    private readonly Dictionary<string, XDocument> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string filePath, string xmlContent)
        => _store[filePath] = XDocument.Parse(xmlContent);

    public XDocument Load(string filePath)
    {
        if (_store.TryGetValue(filePath, out var doc)) return doc;
        throw new FileNotFoundException($"[TestXmlDataLoader] No XML registered for: {filePath}");
    }

    public void Evict(string filePath) => _store.Remove(filePath);
}
```

Service fixture constructors simplify to:

```csharp
// Before (Phase 1 style)
public BrokerRenewalsServiceFixture()
{
    var dataPath = Path.Combine(ContentRootPath, "Data");
    Directory.CreateDirectory(dataPath);
    File.WriteAllText(Path.Combine(dataPath, "broker-renewals.xml"), TestXmlData.BrokerRenewals);
}

// After (Phase 2 style)
public BrokerRenewalsServiceFixture()
{
    Loader = new TestXmlDataLoader();
    Loader.Register("broker-renewals.xml", TestXmlData.BrokerRenewals);
}
public TestXmlDataLoader Loader { get; }
```

Unit test constructors simplify to:

```csharp
// Before
public BrokerRenewalsServiceTests(BrokerRenewalsServiceFixture fixture)
{
    var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
    _sut = new BrokerRenewalsService(env);
}

// After — no temp directory, no IWebHostEnvironment mock needed at all
public BrokerRenewalsServiceTests(BrokerRenewalsServiceFixture fixture)
{
    _sut = new BrokerRenewalsService(fixture.Loader);
}
```

Integration tests (`BrokerApiFactory`) are unaffected — they use the real app with real XML files.
The only DI change needed in `BrokerApiFactory` is allowing `CachedXmlDataLoader` to be resolved,
which happens automatically once `Program.cs` registers it.

---

## 4. Phase 3 — Repository Pattern

### 4.1 Why

Phase 2 solves performance and duplication. Phase 3 solves **testability** and **maintainability**:

- **Unit tests still depend on XML content** — even with `TestXmlDataLoader`, unit tests must craft
  valid XML strings. With repositories, service tests mock `IContractRepository.GetById("x")` and
  return a plain C# object — no XML, no parsing, no string crafting.
- **Business logic is tangled with querying** — `ContractService.GetPricingIntelligence` does XML
  querying (find `ContractPricing` by attribute, then `Snapshot` by id, then build `PricingCell`
  objects) and business logic (build `PricingIntelligenceResponse`) in one method. These should be
  separate.
- **Impossible to swap the data source** — today every service is hardwired to XML. A real
  database migration requires rewriting nine services. With repositories, only the repository
  implementations change; services and controllers are untouched.

### 4.2 Repository domain grouping

Each repository owns one logical domain. It exposes typed domain methods, not raw `XElement` access.

```
BP/
└── Repositories/
    ├── Interfaces/
    │   ├── IContractRepository.cs
    │   ├── IBrokerRenewalsRepository.cs
    │   ├── IBrokerHomeRepository.cs
    │   ├── IPricingRepository.cs
    │   ├── IUserRepository.cs
    │   ├── ISupportCaseRepository.cs
    │   ├── IUsageRepository.cs
    │   ├── IBillingRepository.cs
    │   └── INotificationRepository.cs
    └── Xml/
        ├── XmlContractRepository.cs
        ├── XmlBrokerRenewalsRepository.cs
        ├── XmlBrokerHomeRepository.cs
        ├── XmlPricingRepository.cs
        ├── XmlUserRepository.cs
        ├── XmlSupportCaseRepository.cs
        ├── XmlUsageRepository.cs
        ├── XmlBillingRepository.cs
        └── XmlNotificationRepository.cs
```

The `Xml/` subfolder signals these are XML-backed implementations. A future `Sql/` subfolder would
contain SQL-backed implementations for the same interfaces.

### 4.3 Repository interface design principles

1. **Return domain objects, never `XElement`** — repositories translate XML into models; callers
   never see the persistence technology.
2. **Return `null` for missing single items, empty collection for missing lists** — consistent with
   current service behaviour.
3. **Throw `FileNotFoundException` (Phase 2) or `DataException` (future DB) for infrastructure
   failures** — service methods do not change their exception semantics.
4. **Separate read from write** — mutation methods (`Save`, `Add`, `Update`, `Delete`) are explicit
   and always call `_loader.Evict` (Phase 2) or `DbContext.SaveChanges` (Phase 3 DB migration).

### 4.4 Repository interface definitions

#### `IContractRepository`

```csharp
public interface IContractRepository
{
    // contracts.xml
    (DateTime LastUpdated, List<XElement> Elements) LoadAll();
    ContractDropdownResponse GetDropdown();
    ContractSummary? GetSummary(string contractId);

    // contract-history.xml
    List<ContractHistoryEntry>? GetHistory(string contractId);

    // contracts-table.xml
    ContractTableResponse GetTable();
    ContractDetail? GetDetailById(string id);

    // contracts-meters.xml
    MeterTableResponse? GetMetersByContractId(string contractId);

    // contracts-renew.xml
    RenewContractsResponse GetRenewContracts();

    // renewals.xml
    RenewalTableResponse GetRenewalContracts();
    ContractQuoteHistory? GetQuoteByContractId(string contractId);
    List<string> GetRenewalAddressDropdown();
    RenewalSummary GetRenewalSummary();
    RenewalQuoteDetail? GetRenewalQuoteDetail(string quoteId);

    // formal-quotes.xml (write)
    FormalQuoteResponse AddFormalQuote(FormalQuoteRequest request, string linkedCaseId);
}
```

#### `IPricingRepository`

```csharp
public interface IPricingRepository
{
    // pricing-intelligence.xml
    PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot);

    // pricing-trend.xml
    PricingTrendResponse? GetPricingTrend(string contractId);

    // pricing-snapshot-details.xml
    PricingSnapshotDetailsResponse? GetPricingSnapshotDetails(string contractId);
}
```

#### `IBrokerRenewalsRepository`

```csharp
public interface IBrokerRenewalsRepository
{
    BrokerRenewalsSummary GetSummary();
    BrokerCustomerRenewalsListResponse GetCustomerList(string sortBy, string sortOrder);
    BrokerCustomerRenewalSummary GetCustomerSummary(string companyName);
    BrokerCustomerContractsResponse GetCustomerContracts(
        string companyName, string serviceAddress, string expiringFilter,
        string sortBy, string sortOrder);
    List<string> GetServiceAddressDropdown(string companyName);
    List<BrokerContractDropdownOption> GetContractDropdown(string companyName);
    BrokerContractQuoteHistory? GetQuoteByContractId(string contractId);
    BrokerContractDetails? GetContractDetails(string contractId);
}
```

#### `IBrokerHomeRepository`

```csharp
public interface IBrokerHomeRepository
{
    BrokerSummaryMetrics GetSummaryMetrics();
    BrokerRecentActivity GetRecentActivity();
    (DateTime LastUpdated, List<BrokerRenewalWatchlistItem> Items) GetRenewalWatchlist();
    List<BrokerOpenSupportRequest> GetOpenSupportRequests();
}
```

#### `IUserRepository`

```csharp
public interface IUserRepository
{
    User? GetByEmail(string email);
    User? GetById(int userId);
    IEnumerable<User> GetAll();
    void Update(User user);           // writes + evicts
    void UpdatePassword(string email, string hashedPassword);
}
```

#### `ISupportCaseRepository`

```csharp
public interface ISupportCaseRepository
{
    List<SupportCase> GetAll(string contractId, string status, string priority);
    SupportCase? GetById(string caseId);
    List<RequestType> GetRequestTypes();
    string Create(SupportRequest request);               // writes + evicts; returns new caseId
    void AddChatMessage(string caseId, string message);  // writes + evicts
    List<CaseChat> GetChats(string caseId);
}
```

#### `IUsageRepository`

```csharp
public interface IUsageRepository
{
    MeterSummary? GetMeterSummary(string meterId);
    ContractUsageResponse? GetContractUsage(string contractId);
    UsageTrendResponse? GetUsageTrend(string meterId);
}
```

#### `IBillingRepository`

```csharp
public interface IBillingRepository
{
    BillingSummary GetSummary();
    List<BillingInvoice> GetInvoices();
    BillingContractBillingSummary? GetContractBillingSummary(string contractId);
    ContractInvoiceResponse? GetContractInvoices(string contractId);
}
```

#### `INotificationRepository`

```csharp
public interface INotificationRepository
{
    NotificationsResponse GetAll();
    void MarkAsRead(int notificationId);
}
```

### 4.5 Service changes in Phase 3

Services become pure business-logic coordinators. Using `ContractService` as an example:

**Before (Phase 2):**
```csharp
public PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot)
{
    var doc = _loader.Load(_xmlFilePathPricingIntelligence);
    var contractEl = doc.Root!.Elements("ContractPricing")
        .FirstOrDefault(e => ((string)e.Attribute("contractId")!).Equals(contractId, ...));
    if (contractEl is null) return null;
    var snapshotEl = contractEl.Elements("Snapshot")
        .FirstOrDefault(s => ((string)s.Attribute("id")!).Equals(snapshot, ...));
    if (snapshotEl is null) return null;
    // ... build PricingTermColumn list, PricingMatrixRow list, return response
}
```

**After (Phase 3):**
```csharp
// IContractService still has the same public method — controllers unchanged
public PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot)
    => _pricingRepo.GetPricingIntelligence(contractId, snapshot);
```

All XML querying lives in `XmlPricingRepository`. The service only coordinates.

For services with real business logic (e.g. `SubmitFormalQuote` which creates a linked support
case), the service retains that orchestration:

```csharp
public FormalQuoteResponse SubmitFormalQuote(FormalQuoteRequest request)
{
    // Business logic: coordinate two repositories
    var caseId = _supportCaseRepo.Create(new SupportRequest { ... });
    return _contractRepo.AddFormalQuote(request, caseId);
}
```

### 4.6 DI registration in Phase 3

```csharp
// Repository registrations (replace service registrations for data-access concerns)
builder.Services.AddScoped<IContractRepository, XmlContractRepository>();
builder.Services.AddScoped<IPricingRepository, XmlPricingRepository>();
builder.Services.AddScoped<IBrokerRenewalsRepository, XmlBrokerRenewalsRepository>();
builder.Services.AddScoped<IBrokerHomeRepository, XmlBrokerHomeRepository>();
builder.Services.AddScoped<IUserRepository, XmlUserRepository>();
builder.Services.AddScoped<ISupportCaseRepository, XmlSupportCaseRepository>();
builder.Services.AddScoped<IUsageRepository, XmlUsageRepository>();
builder.Services.AddScoped<IBillingRepository, XmlBillingRepository>();
builder.Services.AddScoped<INotificationRepository, XmlNotificationRepository>();

// Service registrations remain (they now wrap repositories)
builder.Services.AddScoped<IContractService, ContractService>();
// ...
```

### 4.7 Summary of changes in Phase 3

| Folder | Action |
|---|---|
| `BP/Repositories/Interfaces/*.cs` | **Create** 9 interface files |
| `BP/Repositories/Xml/*.cs` | **Create** 9 XML-backed implementations (extract XML code from services) |
| `BP/Services/*.cs` | **Refactor** — remove XML code, inject repositories, keep orchestration |
| `BP/Program.cs` | Add 9 repository registrations |
| `BP.Tests/Unit/*` | **Update** fixtures to mock repositories (no XML, no temp files) |

---

## 5. Migration Sequence

The phases are designed to be independently deployable. Each phase leaves the application in a
fully working state with all 299+ tests green.

```
Phase 1 (done)     Phase 2                       Phase 3
                   ┌─────────────────────┐       ┌──────────────────────────────────┐
Current ──────────►│ IXmlDataLoader      │──────►│ Repository interfaces + Xml impls│
  9 services       │ CachedXmlDataLoader │       │ Services become coordinators      │
  30+ inline       │ TestXmlDataLoader   │       │ Unit tests use mock repos         │
  File.Exists      │ ~14 files touched   │       │ ~20 new files, ~9 files refactored│
  XDocument.Load   │ ~90 lines removed   │       │                                  │
                   └─────────────────────┘       └──────────────────────────────────┘
```

### Recommended order within Phase 2

1. Create `IXmlDataLoader` + `CachedXmlDataLoader` + register in `Program.cs`
2. Migrate simplest services first (one-file, read-only): `BillingService`, `NotificationService`,
   `BrokerRenewalsService`
3. Migrate multi-file read-only services: `BrokerHomeService`, `UsageService`
4. Migrate mutable single-file services: `UserService`, `AccountManagementService`
5. Migrate mutable multi-file services: `SupportCaseService`
6. Migrate `ContractService` last (most complex)
7. Update unit test fixtures to `TestXmlDataLoader`
8. Run full test suite (should be 299+ green)

### Recommended order within Phase 3

1. Extract `IPricingRepository` + `XmlPricingRepository` first (smallest scope, no mutations)
2. Extract `IBillingRepository`, `INotificationRepository`, `IUsageRepository`
3. Extract `IBrokerHomeRepository`, `IBrokerRenewalsRepository`
4. Extract `IUserRepository`, `ISupportCaseRepository` (both have mutations)
5. Extract `IContractRepository` last (most complex, has `formal-quotes.xml` mutation)
6. Update all unit tests to inject mock repositories
7. Run full test suite

---

## 6. Test Strategy Updates

### Phase 2 test changes

| What changes | Old approach | New approach |
|---|---|---|
| Unit test fixture | Creates real temp directory + writes real XML files | Creates `TestXmlDataLoader`, calls `Register(path, xmlString)` |
| `IWebHostEnvironment` mock | Required in every unit test constructor | No longer needed for unit tests (service takes loader, not env) |
| Service constructor | `new BillingService(mockEnv)` | `new BillingService(testLoader)` |
| "Missing file" tests | Pass path to non-existent directory | `TestXmlDataLoader` throws `FileNotFoundException` for unregistered paths |
| Integration tests | Unchanged — `BrokerApiFactory` uses real app | Unchanged |

### Phase 3 test changes

| What changes | Old approach | New approach |
|---|---|---|
| Service unit tests | Require XML content in `TestXmlDataLoader` | Mock repository interfaces with Moq — no XML at all |
| Repository unit tests | New test class per repository | Uses `TestXmlDataLoader` + minimal XML (same pattern as current service tests) |
| Integration tests | Unchanged | Unchanged |

---

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Cache serves stale data after a mutation | Medium | Medium | `Evict()` called immediately after every `doc.Save()`. Short 60 s TTL as safety net. |
| `IMemoryCache` grows unbounded | Low | Low | 25 small XML files; total cache size is negligible (< 1 MB). |
| Phase 2 breaks `IWebHostEnvironment`-based unit tests | High (intentional) | Low | Update fixtures as part of Phase 2 (see §3.5). Tests become simpler, not harder. |
| `ContractService` migration in Phase 2 is risky | Medium | Medium | Migrate last, one XML file at a time, running tests after each file. |
| Phase 3 interface design becomes stale | Low | Medium | Interfaces are defined after reviewing all existing service method signatures. Keep interfaces narrow — add methods on demand. |
| Two services writing to `users.xml` concurrently | Low | High | Not introduced by this refactor — pre-existing. Long-term: move to a proper database. Short-term: `Evict` after every write is sufficient for a single-instance server. |

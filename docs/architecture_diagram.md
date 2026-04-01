# ElMasria E-Commerce — System Architecture

## Onion Architecture Diagram

```
╔══════════════════════════════════════════════════════════════════════════════╗
║                           EXTERNAL CLIENTS                                  ║
║                                                                              ║
║    ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐       ║
║    │  Angular 18 SPA  │   │  Mobile App      │   │  Admin Panel     │       ║
║    │  (RTL Arabic UI) │   │  (Future)        │   │  (Angular)       │       ║
║    └────────┬─────────┘   └────────┬─────────┘   └────────┬─────────┘       ║
║             │                      │                      │                  ║
║             └──────────────────────┼──────────────────────┘                  ║
║                                    │ HTTPS (REST API)                        ║
╚════════════════════════════════════╪═════════════════════════════════════════╝
                                     │
╔════════════════════════════════════╪═════════════════════════════════════════╗
║  LAYER 4 — API (Presentation)     │                                          ║
║  ┌─────────────────────────────────┴──────────────────────────────────────┐  ║
║  │  ElMasria.API                                                          │  ║
║  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │  ║
║  │  │ Controllers  │ │ Middleware   │ │ Filters      │ │ Swagger      │  │  ║
║  │  │ /api/v1/*    │ │ • Exception  │ │ • Validation │ │ • JWT Auth   │  │  ║
║  │  │              │ │ • Logging    │ │ • Auth       │ │ • API Docs   │  │  ║
║  │  │              │ │ • CORS       │ │ • Rate Limit │ │              │  │  ║
║  │  │              │ │ • Security   │ │              │ │              │  │  ║
║  │  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘  │  ║
║  └───────────────────────────────┬────────────────────────────────────────┘  ║
║                                  │ depends on                                ║
╚══════════════════════════════════╪═══════════════════════════════════════════╝
                                   ▼
╔══════════════════════════════════════════════════════════════════════════════╗
║  LAYER 3 — APPLICATION (Use Cases / Business Orchestration)                  ║
║  ┌────────────────────────────────────────────────────────────────────────┐  ║
║  │  ElMasria.Application                                                  │  ║
║  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │  ║
║  │  │ Services     │ │ DTOs         │ │ Validators   │ │ Mappings     │  │  ║
║  │  │ • Product    │ │ • Request    │ │ • Fluent     │ │ • AutoMapper │  │  ║
║  │  │ • Order      │ │ • Response   │ │ • Arabic     │ │ • Profiles   │  │  ║
║  │  │ • Auth       │ │ • Filter     │ │   Messages   │ │              │  │  ║
║  │  │ • Cart       │ │              │ │              │ │              │  │  ║
║  │  │ • Search     │ │              │ │              │ │              │  │  ║
║  │  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘  │  ║
║  │  ┌──────────────────────────────────────────────────────────────────┐  │  ║
║  │  │ Interfaces (contracts consumed from Domain)                      │  │  ║
║  │  │ IProductService, IOrderService, IAuthService, ICartService, ...  │  │  ║
║  │  └──────────────────────────────────────────────────────────────────┘  │  ║
║  └───────────────────────────────┬────────────────────────────────────────┘  ║
║                                  │ depends on                                ║
╚══════════════════════════════════╪═══════════════════════════════════════════╝
                                   ▼
╔══════════════════════════════════════════════════════════════════════════════╗
║  LAYER 1 — DOMAIN (Enterprise Business Rules)        ⚠ ZERO DEPENDENCIES   ║
║  ┌────────────────────────────────────────────────────────────────────────┐  ║
║  │  ElMasria.Domain                                                       │  ║
║  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │  ║
║  │  │ Entities     │ │ Enums        │ │ Events       │ │ Exceptions   │  │  ║
║  │  │ • Product    │ │ • OrderStatus│ │ • OrderPlaced│ │ • DomainEx   │  │  ║
║  │  │ • Order      │ │ • PayMethod  │ │ • StockLow   │ │ • NotFound   │  │  ║
║  │  │ • Category   │ │ • PayStatus  │ │ • PayConfirm │ │ • OutOfStock │  │  ║
║  │  │ • Cart       │ │ • ReviewStat │ │              │ │ • CouponExp  │  │  ║
║  │  │ • User       │ │              │ │              │ │              │  │  ║
║  │  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘  │  ║
║  │  ┌──────────────────────────────────────────────────────────────────┐  │  ║
║  │  │ Interfaces (Repository contracts — implemented in Infrastructure)│  │  ║
║  │  │ IGenericRepository<T>, IProductRepository, IOrderRepository,     │  │  ║
║  │  │ ICategoryRepository, ICartRepository, IUnitOfWork                │  │  ║
║  │  └──────────────────────────────────────────────────────────────────┘  │  ║
║  └────────────────────────────────────────────────────────────────────────┘  ║
╚══════════════════════════════════════════════════════════════════════════════╝
                                   ▲
                                   │ implements interfaces from Domain
╔══════════════════════════════════╪═══════════════════════════════════════════╗
║  LAYER 2 — INFRASTRUCTURE       │    (External Concerns)                     ║
║  ┌───────────────────────────────┴────────────────────────────────────────┐  ║
║  │  ElMasria.Infrastructure                                               │  ║
║  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │  ║
║  │  │ Data         │ │ Identity     │ │ Services     │ │ Caching      │  │  ║
║  │  │ • DbContext  │ │ • JWT Token  │ │ • Paymob     │ │ • Redis      │  │  ║
║  │  │ • Configs    │ │ • Auth Svc   │ │ • Email/SMTP │ │ • CacheAside │  │  ║
║  │  │ • Repos      │ │ • CurrentUser│ │ • Image Proc │ │ • Keys       │  │  ║
║  │  │ • UoW        │ │ • Policies   │ │ • Slug Gen   │ │              │  │  ║
║  │  │ • Migrations │ │              │ │ • Search     │ │              │  │  ║
║  │  └──────┬───────┘ └──────┬───────┘ └──────┬───────┘ └──────┬───────┘  │  ║
║  └─────────┼────────────────┼────────────────┼────────────────┼──────────┘  ║
╚════════════╪════════════════╪════════════════╪════════════════╪═════════════╝
             │                │                │                │
             ▼                ▼                ▼                ▼
   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
   │  SQL Server  │  │  ASP.NET     │  │  Paymob API  │  │  Redis       │
   │  (EF Core 8) │  │  Identity    │  │  (HTTPS)     │  │  (Cache)     │
   └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
                                       ┌──────────────┐
                                       │  SMTP Server │
                                       │  (Email)     │
                                       └──────────────┘
```

## Request Flow

```
Client Request
    │
    ▼
┌─────────────────────────────┐
│ Middleware Pipeline (order!) │
│ 1. ExceptionHandlerMiddleware│
│ 2. SecurityHeadersMiddleware │
│ 3. RequestLoggingMiddleware  │
│ 4. CORS                     │
│ 5. RateLimiting              │
│ 6. Authentication            │
│ 7. Authorization             │
│ 8. ResponseCompression       │
│ 9. Routing → Controllers     │
└──────────────┬──────────────┘
               ▼
┌──────────────────────────────┐
│ Controller                    │
│ • Model binding + validation │
│ • Calls Application service  │
│ • Returns ApiResponse<T>     │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Application Service           │
│ • Business orchestration     │
│ • DTO ↔ Entity mapping       │
│ • Calls domain methods       │
│ • Calls repository via IUoW  │
│ • Raises domain events       │
│ • Caching (Redis)            │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Domain Entity                 │
│ • Validates business rules   │
│ • Mutates state via methods  │
│ • Raises domain events       │
└──────────────────────────────┘
               ▼
┌──────────────────────────────┐
│ Repository (Infrastructure)   │
│ • EF Core queries            │
│ • AsNoTracking for reads     │
│ • Compiled queries           │
└──────────────┬───────────────┘
               ▼
          SQL Server
```

## Solution Structure

```
ElMasria.Ecommerce.sln
│
├── src/
│   ├── ElMasria.Domain/                          ← Layer 1 (innermost)
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   │   ├── IGenericRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── Repositories/
│   │   ├── Common/
│   │   └── ElMasria.Domain.csproj               ← NO NuGet packages
│   │
│   ├── ElMasria.Application/                     ← Layer 3
│   │   ├── DTOs/
│   │   ├── Services/
│   │   ├── Interfaces/
│   │   ├── Validators/
│   │   ├── Mappings/
│   │   ├── Common/
│   │   └── ElMasria.Application.csproj          ← References: Domain only
│   │
│   ├── ElMasria.Infrastructure/                  ← Layer 2
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   ├── Identity/
│   │   ├── Services/
│   │   ├── Caching/
│   │   └── ElMasria.Infrastructure.csproj       ← References: Domain + Application
│   │
│   └── ElMasria.API/                             ← Layer 4 (outermost)
│       ├── Controllers/
│       ├── Middleware/
│       ├── Extensions/
│       ├── Filters/
│       ├── Program.cs
│       └── ElMasria.API.csproj                  ← References: Application only
│
├── tests/
│   ├── ElMasria.UnitTests/
│   └── ElMasria.IntegrationTests/
│
├── docs/
├── database/
├── Directory.Build.props
├── .editorconfig
├── .gitignore
└── docker-compose.yml
```

## NuGet Package Map

### ElMasria.Domain — ZERO packages
No external dependencies whatsoever. Pure C# only.

### ElMasria.Application
| Package | Version | Purpose |
|---------|---------|---------|
| AutoMapper | 13.0.1 | DTO ↔ Entity mapping |
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.0.1 | DI registration |
| FluentValidation | 11.9.2 | Input validation with Arabic messages |
| FluentValidation.AspNetCore | 11.3.0 | ASP.NET integration |
| Microsoft.Extensions.Caching.Abstractions | 8.0.0 | Cache interface |

### ElMasria.Infrastructure
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.10 | ORM |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.10 | SQL Server provider |
| Microsoft.EntityFrameworkCore.Tools | 8.0.10 | Migrations CLI |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.10 | Identity + EF |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.10 | JWT auth |
| StackExchange.Redis | 2.7.33 | Redis client |
| SixLabors.ImageSharp | 3.1.5 | Image processing |
| Serilog.AspNetCore | 8.0.2 | Structured logging |
| Serilog.Sinks.File | 5.0.0 | File logging |
| Serilog.Sinks.Seq | 7.0.1 | Seq log aggregation |

### ElMasria.API
| Package | Version | Purpose |
|---------|---------|---------|
| Swashbuckle.AspNetCore | 6.8.1 | Swagger/OpenAPI |
| Serilog.AspNetCore | 8.0.2 | Logging pipeline |
| AspNetCoreRateLimit | 5.0.0 | Rate limiting |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.10 | JWT middleware |

### ElMasria.UnitTests
| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.9.2 | Test runner |
| Moq | 4.20.72 | Mocking |
| FluentAssertions | 6.12.1 | Readable assertions |
| Bogus | 35.6.1 | Fake data (Arabic support) |
| coverlet.collector | 6.0.2 | Code coverage |

### ElMasria.IntegrationTests
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Mvc.Testing | 8.0.10 | WebApplicationFactory |
| Testcontainers.MsSql | 3.10.0 | SQL container |
| xunit, Moq, FluentAssertions | (same) | Testing |

## API Conventions

### Response Envelope
```json
{
  "success": true,
  "statusCode": 200,
  "message": "تم جلب المنتجات بنجاح",
  "messageEn": "Products retrieved successfully",
  "data": { ... },
  "errors": [],
  "meta": {
    "currentPage": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasPrevious": false,
    "hasNext": true
  }
}
```

### Error Response
```json
{
  "success": false,
  "statusCode": 400,
  "message": "بيانات غير صالحة",
  "messageEn": "Invalid input data",
  "data": null,
  "errors": [
    { "field": "Price", "message": "السعر يجب أن يكون أكبر من صفر" },
    { "field": "NameAr", "message": "اسم المنتج بالعربية مطلوب" }
  ]
}
```

### HTTP Status Code Conventions
| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET, PUT, PATCH |
| 201 | Created | Successful POST (resource created) |
| 204 | No Content | Successful DELETE, logout |
| 400 | Bad Request | Validation failure |
| 401 | Unauthorized | Missing/invalid JWT |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Duplicate resource (SKU, email) |
| 422 | Unprocessable | Business rule violation |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Server Error | Unhandled exception |

## Security Architecture

| Concern | Implementation |
|---------|---------------|
| Access Token | JWT HS256, 60-minute expiry |
| Refresh Token | 64-byte random, SHA-256 hashed in DB, 30-day expiry |
| Token Rotation | New refresh token on every refresh, old invalidated |
| Password Hash | ASP.NET Identity PBKDF2 (SHA-256, 10K iterations) |
| Account Lockout | 5 failed attempts → 15-minute lockout |
| Role Hierarchy | SuperAdmin → Admin → Customer |
| CORS | Whitelist: production domain + localhost:4200 (dev) |
| Rate Limiting | Login: 5/min, Register: 3/min, General: 100/min |

## Caching Strategy (Redis)

| Data | Key Pattern | TTL | Invalidation |
|------|-------------|-----|-------------|
| Category tree | `categories:tree` | 2 hours | Category CRUD |
| Featured products | `products:featured:{count}` | 1 hour | Product update |
| Product detail | `products:detail:{id}` | 30 min | Product update |
| Product list | `products:list:{filterHash}` | 10 min | Product CRUD |
| Dashboard stats | `admin:dashboard:stats` | 5 min | Order/payment events |
| Paymob auth | `paymob:auth_token` | 55 min | On expiry |
| Autocomplete | `search:auto:{query}:{lang}` | 30 sec | Product CRUD |
| Active cart | `cart:{userId}` or `cart:session:{id}` | 30 min | Cart mutation |

## Performance Targets

| Endpoint | P95 Target | DB Query Budget |
|----------|-----------|----------------|
| Product List | < 300ms | 1 query (filtered + paginated) |
| Product Detail | < 200ms | 2 queries (product + images) |
| Category Tree | < 100ms | 0 queries (cached) |
| Cart Operations | < 500ms | 2 queries |
| Order Creation | < 2s | 6 queries (transactional) |
| Search | < 200ms | 1 query |
| Autocomplete | < 50ms | 0 queries (cached) |
| Dashboard Stats | < 1s | 3 queries (cached) |

Concurrent user target: 500 (peak Egyptian sales events)

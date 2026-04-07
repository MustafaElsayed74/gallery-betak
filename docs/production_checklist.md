# Production Readiness Launch Checklist

Before flipping the DNS to go "live" in production, verify every checkbox accurately.

## 1. Application Configurations 🛠️

- [ ] Ensure `ASPNETCORE_ENVIRONMENT` is set strictly to `Production`.
- [ ] Ensure `IsDevelopment()` features are disabled (Swagger is correctly bounded in `Program.cs`, developer exception pages are disabled via `GlobalExceptionHandlerMiddleware`).
- [ ] Confirm the database connection string is mapped to the live production server and uses `TrustServerCertificate=false` if employing real certificates in intra-network nodes.
- [ ] Ensure Redis `ConnectionString` is properly wired and no longer pointing to localhost but the secure local IP of the cache node.
- [ ] The `JwtSettings:SecretKey` in `appsettings.Production.json` must be a high-entropy string rotated away from any local development keys.

## 2. Integrations & Secrets 🔑

- [ ] Provide correct live `PaymobSettings` (ApiKey, HmacSecret, Integration IDs). Confirm Paymob Webhook callbacks are pointing to the live domain URL.
- [ ] Provide correct `SmtpSettings` (password/app-passwords) to ensure outgoing order confirmation emails succeed.
- [ ] Remove all dummy data, test products, and seed scripts from final build deployment phases. (Use a clean initialized `.mdf` on launch).

## 3. Security Hardening 🛡️

- [ ] Overwrite `RequireHttpsMetadata = true` in `AuthExtensions.cs` to ensure tokens only pass over TLS.
- [ ] Make sure `CorsSettings:AllowedOrigins` restricts exactly to the frontend production URLs (e.g., `https://gallery-betak.com`, `https://www.gallery-betak.com`) rather than `*` or `localhost`.
- [ ] Turn off `app.UseDeveloperExceptionPage()` in `Program.cs` explicitly if not naturally overriden by the environment.
- [ ] Ensure default Identity generic `TestUser` or `Admin: password123` defaults are entirely purged and replaced with a cryptographic initialization admin account.

## 4. Performance Check 🚀

- [ ] Angular frontend is built strictly leveraging `npm run build --configuration=production` to exploit AOT (Ahead-of-Time) compilation and minification.
- [ ] SQL Server `MinBatchSize` and `MaxBatchSize` on context are healthy. No N+1 queries detected during staging load tests.

Once verified, the repository is ready for cut-over.


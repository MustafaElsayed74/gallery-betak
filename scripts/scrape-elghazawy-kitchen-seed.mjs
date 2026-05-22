import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";

const categoryUrl = "https://elghazawy.com/ar/sub-category/kitchen-tools";
const jsonOutput = resolve("src/GalleryBetak.Infrastructure/Data/Seed/elghazawy-kitchen-tools.json");
const sqlOutput = resolve("database/03_Seed_Data.sql");
const concurrency = Number(process.env.SCRAPE_CONCURRENCY ?? 12);

function decodeHtml(value) {
  if (!value) return "";
  return value
    .replace(/&amp;/g, "&")
    .replace(/&quot;/g, "\"")
    .replace(/&#039;/g, "'")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&#x([0-9a-f]+);/gi, (_, hex) => String.fromCodePoint(Number.parseInt(hex, 16)))
    .replace(/&#(\d+);/g, (_, number) => String.fromCodePoint(Number.parseInt(number, 10)));
}

function cleanText(value) {
  const decoded = decodeHtml(String(value ?? ""));
  return decoded.replace(/\s+/g, " ").trim() || null;
}

function sqlString(value) {
  if (value === null || value === undefined || value === "") return "NULL";
  return `N'${String(value).replace(/'/g, "''")}'`;
}

function sqlNumber(value) {
  if (value === null || value === undefined || value === "") return "NULL";
  return Number(value).toFixed(2);
}

function slugify(value, fallback) {
  const text = cleanText(value) ?? fallback;
  const slug = text
    .toLowerCase()
    .replace(/&/g, " and ")
    .replace(/[^\p{L}\p{N}]+/gu, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 220)
    .replace(/-$/g, "");

  return slug || fallback;
}

function getProductId(url) {
  return url.match(/\/product\/(\d+)\//)?.[1] ?? crypto.randomUUID().replace(/-/g, "").slice(0, 12);
}

function getEnglishName(url) {
  const last = new URL(url).pathname.split("/").filter(Boolean).at(-1) ?? "";
  return cleanText(decodeURIComponent(last.replace(/\+/g, " ")));
}

function getMeta(html, key) {
  const escaped = key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const patterns = [
    new RegExp(`<meta[^>]+name=["']${escaped}["'][^>]+content=["'](?<value>[^"']*)["'][^>]*>`, "i"),
    new RegExp(`<meta[^>]+content=["'](?<value>[^"']*)["'][^>]+name=["']${escaped}["'][^>]*>`, "i"),
    new RegExp(`<meta[^>]+property=["']${escaped}["'][^>]+content=["'](?<value>[^"']*)["'][^>]*>`, "i"),
    new RegExp(`<meta[^>]+content=["'](?<value>[^"']*)["'][^>]+property=["']${escaped}["'][^>]*>`, "i"),
  ];

  for (const pattern of patterns) {
    const match = html.match(pattern);
    if (match?.groups?.value) return cleanText(match.groups.value);
  }

  return null;
}

function findProductNode(node) {
  if (!node) return null;
  if (Array.isArray(node)) {
    for (const item of node) {
      const found = findProductNode(item);
      if (found) return found;
    }
    return null;
  }

  if (typeof node !== "object") return null;

  const type = node["@type"];
  if (
    (typeof type === "string" && type.toLowerCase().includes("product")) ||
    (Array.isArray(type) && type.some((item) => String(item).toLowerCase().includes("product")))
  ) {
    return node;
  }

  return findProductNode(node["@graph"]) ?? findProductNode(node.mainEntity) ?? findProductNode(node.item);
}

function getJsonLdProduct(html) {
  const scripts = html.matchAll(/<script[^>]*type\s*=\s*["']application\/ld\+json["'][^>]*>(?<json>.*?)<\/script>/gis);
  for (const script of scripts) {
    try {
      const parsed = JSON.parse(decodeHtml(script.groups.json).trim());
      const product = findProductNode(parsed);
      if (product) return product;
    } catch {
      continue;
    }
  }

  return null;
}

function getImages(html, product) {
  const images = [];
  const structured = product?.image;
  if (Array.isArray(structured)) images.push(...structured);
  else if (structured) images.push(structured);

  return [...new Set(images.map((image) => decodeHtml(String(image))).filter((image) => image && image.length <= 500))].slice(0, 6);
}

function getPrice(product) {
  const offer = Array.isArray(product?.offers) ? product.offers[0] : product?.offers;
  const price = Number(String(offer?.price ?? "").replace(/[^0-9.]/g, ""));
  const originalPrice = Number(String(offer?.highPrice ?? "").replace(/[^0-9.]/g, ""));

  return {
    price: Number.isFinite(price) && price > 0 ? price : null,
    originalPrice: Number.isFinite(originalPrice) && originalPrice > price ? originalPrice : null,
  };
}

async function fetchText(url, timeoutMs = 45000) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(url, {
      signal: controller.signal,
      headers: {
        "user-agent": "GalleryBetakSeedBot/1.0",
        accept: "text/html,application/xhtml+xml",
      },
    });

    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return await response.text();
  } finally {
    clearTimeout(timeout);
  }
}

async function scrapeProduct(url, index, total) {
  console.log(`[${index}/${total}] ${url}`);
  let html = null;
  let lastError = null;
  for (const timeoutMs of [45000, 90000]) {
    try {
      html = await fetchText(url, timeoutMs);
      break;
    } catch (error) {
      lastError = error;
    }
  }

  if (!html) {
    throw lastError ?? new Error("Unable to fetch product page");
  }

  const product = getJsonLdProduct(html);
  const sourceId = getProductId(url);
  const nameEn = getEnglishName(url);
  const nameAr = cleanText(product?.name) ?? cleanText(getMeta(html, "og:title")?.replace(/^الغزاوي\s*\|\s*/, ""));
  const descriptionAr = getMeta(html, "description") ?? nameAr;
  const { price, originalPrice } = getPrice(product);
  const imageUrls = getImages(html, product);

  if (!nameAr || !nameEn || !price || imageUrls.length === 0) {
    throw new Error("Incomplete product data");
  }

  return {
    sourceId,
    sku: `ELG-${sourceId}`,
    slug: `${slugify(nameEn, `elg-${sourceId}`)}-${sourceId}`.slice(0, 300),
    nameAr,
    nameEn,
    descriptionAr,
    descriptionEn: nameEn,
    price,
    originalPrice,
    stockQuantity: 30,
    material: null,
    origin: "Elghazawy",
    imageUrls,
    sourceUrl: url,
  };
}

async function mapWithConcurrency(items, limit, mapper) {
  const results = new Array(items.length);
  let cursor = 0;

  async function worker() {
    while (cursor < items.length) {
      const index = cursor++;
      try {
        results[index] = await mapper(items[index], index);
      } catch (error) {
        console.warn(`Skipped ${items[index]} :: ${error.message}`);
      }
    }
  }

  await Promise.all(Array.from({ length: Math.min(limit, items.length) }, worker));
  return results.filter(Boolean);
}

function buildSql(payload) {
  const legacySkus = Array.from({ length: 10 }, (_, index) => `SKU${String(index + 1).padStart(3, "0")}`);
  const lines = [
    "-- =====================================================================",
    "-- GalleryBetak E-Commerce - Elghazawy Kitchen Tools Seed Data",
    `-- Source: ${payload.source}`,
    `-- Products: ${payload.products.length}`,
    "-- Execute AFTER 01_DDL_Schema.sql",
    "-- =====================================================================",
    "",
    "SET NOCOUNT ON;",
    "",
    "DECLARE @KitchenCategoryId INT;",
    "",
    "DELETE PT FROM [dbo].[ProductTags] PT INNER JOIN [dbo].[Products] P ON P.[Id] = PT.[ProductId] WHERE P.[SKU] LIKE N'ELG-%' OR P.[SKU] IN (" + legacySkus.map(sqlString).join(", ") + ");",
    "DELETE PI FROM [dbo].[ProductImages] PI INNER JOIN [dbo].[Products] P ON P.[Id] = PI.[ProductId] WHERE P.[SKU] LIKE N'ELG-%' OR P.[SKU] IN (" + legacySkus.map(sqlString).join(", ") + ");",
    "DELETE FROM [dbo].[Products] WHERE [SKU] LIKE N'ELG-%' OR [SKU] IN (" + legacySkus.map(sqlString).join(", ") + ");",
    "DELETE FROM [dbo].[Categories] WHERE [Slug] IN (N'home-furniture', N'decorations', N'lighting', N'rugs', N'textiles') AND NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [CategoryId] = [dbo].[Categories].[Id]);",
    "",
    "IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'kitchen-tools')",
    "BEGIN",
    `    INSERT INTO [dbo].[Categories] ([NameAr], [NameEn], [Slug], [DescriptionAr], [DescriptionEn], [ImageUrl], [ParentId], [DisplayOrder], [IsActive]) VALUES (${sqlString(payload.category.nameAr)}, ${sqlString(payload.category.nameEn)}, N'kitchen-tools', ${sqlString(payload.category.descriptionAr)}, ${sqlString(payload.category.descriptionEn)}, ${sqlString(payload.category.imageUrl)}, NULL, 1, 1);`,
    "END;",
    "",
    "SELECT @KitchenCategoryId = [Id] FROM [dbo].[Categories] WHERE [Slug] = N'kitchen-tools';",
    "",
  ];

  payload.products.forEach((product, index) => {
    const featured = index < 12 ? 1 : 0;
    lines.push(`INSERT INTO [dbo].[Products] ([NameAr], [NameEn], [Slug], [DescriptionAr], [DescriptionEn], [Price], [OriginalPrice], [SKU], [StockQuantity], [CategoryId], [IsFeatured], [IsActive], [Material], [Origin], [SourceUrl], [ImportedAt])`);
    lines.push(`SELECT ${sqlString(product.nameAr)}, ${sqlString(product.nameEn)}, ${sqlString(product.slug)}, ${sqlString(product.descriptionAr)}, ${sqlString(product.descriptionEn)}, ${sqlNumber(product.price)}, ${sqlNumber(product.originalPrice)}, ${sqlString(product.sku)}, ${product.stockQuantity}, @KitchenCategoryId, ${featured}, 1, ${sqlString(product.material)}, ${sqlString(product.origin)}, ${sqlString(product.sourceUrl)}, SYSUTCDATETIME()`);
    lines.push(`WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [SKU] = ${sqlString(product.sku)});`);
    product.imageUrls.forEach((imageUrl, imageIndex) => {
      lines.push(`INSERT INTO [dbo].[ProductImages] ([ProductId], [ImageUrl], [ThumbnailUrl], [AltTextAr], [AltTextEn], [DisplayOrder], [IsPrimary])`);
      lines.push(`SELECT P.[Id], ${sqlString(imageUrl)}, NULL, ${sqlString(product.nameAr)}, ${sqlString(product.nameEn)}, ${imageIndex + 1}, ${imageIndex === 0 ? 1 : 0} FROM [dbo].[Products] P WHERE P.[SKU] = ${sqlString(product.sku)} AND NOT EXISTS (SELECT 1 FROM [dbo].[ProductImages] PI WHERE PI.[ProductId] = P.[Id] AND PI.[ImageUrl] = ${sqlString(imageUrl)});`);
    });
    lines.push("");
  });

  lines.push("PRINT N'Elghazawy kitchen tools seed data inserted successfully.';");
  lines.push("GO");
  return `${lines.join("\n")}\n`;
}

const categoryHtml = await fetchText(categoryUrl, 60000);
const urls = [...new Set([...categoryHtml.matchAll(/https:\/\/elghazawy\.com\/ar\/product\/[^"'<>\s]+/g)].map((match) => decodeHtml(match[0])))];

const products = await mapWithConcurrency(urls, concurrency, (url, index) => scrapeProduct(url, index + 1, urls.length));

const payload = {
  source: categoryUrl,
  scrapedAtUtc: new Date().toISOString(),
  category: {
    nameAr: "أدوات ومعدات المطبخ",
    nameEn: "Kitchen Tools",
    slug: "kitchen-tools",
    descriptionAr: "منتجات أدوات ومعدات المطبخ المستوردة من الغزاوي.",
    descriptionEn: "Kitchen tools and equipment imported from Elghazawy.",
    imageUrl: "https://elghazawy.com/front_assets/img/logo-large.png",
  },
  products,
};

await mkdir(dirname(jsonOutput), { recursive: true });
await writeFile(jsonOutput, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
await writeFile(sqlOutput, buildSql(payload), "utf8");

console.log(`ProductsWritten=${products.length}`);
console.log(`JsonOutput=${jsonOutput}`);
console.log(`SqlOutput=${sqlOutput}`);

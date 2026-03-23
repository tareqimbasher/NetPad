CREATE DATABASE "LibraryDb"
    WITH OWNER = postgres
    ENCODING = 'UTF8'
    TEMPLATE = template0;

-- ======================
-- Tables
-- ======================

CREATE TABLE "Author"
(
    "Id"        SERIAL PRIMARY KEY,
    "Name"      TEXT        NOT NULL,
    "Email"     TEXT UNIQUE NOT NULL,
    "CreatedAt" TIMESTAMP DEFAULT NOW()
);

CREATE TABLE "Book"
(
    "Id"            SERIAL PRIMARY KEY,
    "Title"         TEXT NOT NULL,
    "PublishedYear" INT,
    "AuthorId"      INT  NOT NULL REFERENCES "Author" ("Id") ON DELETE CASCADE,
    "CreatedAt"     TIMESTAMP DEFAULT NOW()
);

-- ======================
-- Seed authors
-- ======================

INSERT INTO "Author" ("Name", "Email")
VALUES ('Jane Austen', 'jane.austen@example.com'),
       ('George Orwell', 'george.orwell@example.com'),
       ('Mary Shelley', 'mary.shelley@example.com')
ON CONFLICT ("Email") DO NOTHING;

-- ======================
-- Seed books
-- ======================

INSERT INTO "Book" ("Title", "PublishedYear", "AuthorId")
SELECT 'Pride and Prejudice', 1813, "Id"
FROM "Author"
WHERE "Email" = 'jane.austen@example.com'
ON CONFLICT DO NOTHING;

INSERT INTO "Book" ("Title", "PublishedYear", "AuthorId")
SELECT '1984', 1949, "Id"
FROM "Author"
WHERE "Email" = 'george.orwell@example.com'
ON CONFLICT DO NOTHING;

INSERT INTO "Book" ("Title", "PublishedYear", "AuthorId")
SELECT 'Animal Farm', 1945, "Id"
FROM "Author"
WHERE "Email" = 'george.orwell@example.com'
ON CONFLICT DO NOTHING;

INSERT INTO "Book" ("Title", "PublishedYear", "AuthorId")
SELECT 'Frankenstein', 1818, "Id"
FROM "Author"
WHERE "Email" = 'mary.shelley@example.com'
ON CONFLICT DO NOTHING;

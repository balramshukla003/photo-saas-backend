-- ═══════════════════════════════════════════════════════════════
--  PhotoPrint SaaS — MySQL 8 / MariaDB
--  DB FIRST: Run this schema FIRST, then scaffold EF Core models
--  Scaffold command at bottom of this file
-- ═══════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS photoprint
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE photoprint;

-- ─────────────────────────────────────────────────────────────
-- TABLE: users
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
    id            CHAR(36)       NOT NULL DEFAULT (UUID()),
    email         VARCHAR(255)   NOT NULL,
    password_hash VARCHAR(512)   NOT NULL,
    full_name     VARCHAR(255)   NOT NULL DEFAULT '',
    is_active     TINYINT(1)     NOT NULL DEFAULT 1,
    created_at    DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP
                                 ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_users        PRIMARY KEY (id),
    CONSTRAINT uq_users_email  UNIQUE      (email)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;


-- ─────────────────────────────────────────────────────────────
-- TABLE: licenses
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS licenses (
    id            CHAR(36)       NOT NULL DEFAULT (UUID()),
    user_id       CHAR(36)       NOT NULL,
    license_key   VARCHAR(64)    NOT NULL,
    is_active     TINYINT(1)     NOT NULL DEFAULT 1,
    plan          VARCHAR(50)    NOT NULL DEFAULT 'standard',
    issued_at     DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at    DATETIME       NOT NULL,
    notes         TEXT               NULL,

    CONSTRAINT pk_licenses             PRIMARY KEY (id),
    CONSTRAINT uq_licenses_key         UNIQUE      (license_key),
    CONSTRAINT fk_licenses_user        FOREIGN KEY (user_id)
                                       REFERENCES  users (id)
                                       ON DELETE   CASCADE
                                       ON UPDATE   CASCADE
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- Indexes for query performance
CREATE INDEX idx_licenses_user_id   ON licenses (user_id);
CREATE INDEX idx_licenses_expires   ON licenses (expires_at);


-- ─────────────────────────────────────────────────────────────
-- SEED: default admin + 1-year license
-- Password: Admin@123  (BCrypt hash below)
-- Regenerate hash with: BCrypt.Net.BCrypt.HashPassword("Admin@123", 11)
-- ─────────────────────────────────────────────────────────────
INSERT INTO users (id, email, password_hash, full_name, is_active)
VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'admin@photoprint.com',
    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi',
    'Admin User',
    1
);

INSERT INTO licenses (id, user_id, license_key, is_active, plan, issued_at, expires_at)
VALUES (
    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'PP-STANDARD-2024-ADMIN-001',
    1,
    'standard',
    NOW(),
    DATE_ADD(NOW(), INTERVAL 1 YEAR)
);


-- ═══════════════════════════════════════════════════════════════
--  SCAFFOLD COMMAND
--  Run from /PhotoPrint.API project root after DB is created:
--
--  dotnet ef dbcontext scaffold \
--    "Server=localhost;Port=3306;Database=photoprint;User=root;Password=YOUR_PASSWORD;CharSet=utf8mb4;" \
--    Pomelo.EntityFrameworkCore.MySql \
--    --output-dir Data/Scaffolded \
--    --context-dir Data \
--    --context PhotoPrintDbContext \
--    --data-annotations \
--    --no-onconfiguring \
--    --force
--
--  Then move/rename as needed. The DbContext connection string
--  is injected via Program.cs — never hardcoded.
-- ═══════════════════════════════════════════════════════════════

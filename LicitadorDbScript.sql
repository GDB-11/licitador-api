CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE SCHEMA IF NOT EXISTS "Auth";
CREATE SCHEMA IF NOT EXISTS "Company";
CREATE SCHEMA IF NOT EXISTS "Document";

-- =====================================================
-- SCHEMA: Auth (Autenticación y Usuarios)
-- =====================================================

CREATE TABLE "Auth"."Users" (
    "UserId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(500) NOT NULL,
    "FullName" VARCHAR(255) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "CK_Users_Email" CHECK ("Email" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

CREATE INDEX "IDX_Users_Email" ON "Auth"."Users"("Email");
CREATE INDEX "IDX_Users_IsActive" ON "Auth"."Users"("IsActive");
CREATE INDEX "IDX_Users_CreatedDate" ON "Auth"."Users"("CreatedDate" DESC);

-- =====================================================
-- SCHEMA: Company (Datos Empresariales)
-- =====================================================

CREATE TABLE "Company"."Companies" (
    "CompanyId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "Ruc" CHAR(11) NOT NULL UNIQUE,
    "RazonSocial" VARCHAR(500) NOT NULL,
    "DomicilioLegal" VARCHAR(1000) NOT NULL,
    "Telefono" VARCHAR(50) NULL,
    "Email" VARCHAR(255) NOT NULL,
    "IsMype" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "CK_Companies_Ruc" CHECK ("Ruc" ~ '^[0-9]{11}$'),
    CONSTRAINT "CK_Companies_Email" CHECK ("Email" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

CREATE INDEX "IDX_Companies_Ruc" ON "Company"."Companies"("Ruc");
CREATE INDEX "IDX_Companies_CreatedDate" ON "Company"."Companies"("CreatedDate" DESC);

-- Tabla intermedia: Relación muchos a muchos entre Users y Companies
CREATE TABLE "Company"."UserCompanies" (
    "UserCompanyId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "UserId" UUID NOT NULL,
    "CompanyId" UUID NOT NULL,
    "Role" VARCHAR(50) NULL, -- Ej: 'Owner', 'Admin', 'Collaborator'
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "FK_UserCompanies_Users" FOREIGN KEY ("UserId") 
        REFERENCES "Auth"."Users"("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_UserCompanies_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Company"."Companies"("CompanyId") ON DELETE CASCADE,
    CONSTRAINT "UK_UserCompanies_User_Company" UNIQUE ("UserId", "CompanyId")
);

CREATE INDEX "IDX_UserCompanies_UserId" ON "Company"."UserCompanies"("UserId");
CREATE INDEX "IDX_UserCompanies_CompanyId" ON "Company"."UserCompanies"("CompanyId");
CREATE INDEX "IDX_UserCompanies_IsActive" ON "Company"."UserCompanies"("IsActive");

CREATE TABLE "Company"."LegalRepresentatives" (
    "LegalRepresentativeId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "CompanyId" UUID NOT NULL,
    "FullName" VARCHAR(255) NOT NULL,
    "DocumentType" VARCHAR(50) NOT NULL,
    "DocumentNumber" VARCHAR(50) NOT NULL,
    "PowerRegistrationLocation" VARCHAR(255) NULL,
    "PowerRegistrationSheet" VARCHAR(100) NULL,
    "PowerRegistrationEntry" VARCHAR(100) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "FK_LegalRepresentatives_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Company"."Companies"("CompanyId") ON DELETE CASCADE,
    CONSTRAINT "CK_LegalRepresentatives_DocumentType" 
        CHECK ("DocumentType" IN ('DNI', 'CE', 'Pasaporte', 'Carnet de Extranjería'))
);

CREATE INDEX "IDX_LegalRepresentatives_CompanyId" ON "Company"."LegalRepresentatives"("CompanyId");
CREATE INDEX "IDX_LegalRepresentatives_IsActive" ON "Company"."LegalRepresentatives"("IsActive");
CREATE INDEX "IDX_LegalRepresentatives_DocumentNumber" ON "Company"."LegalRepresentatives"("DocumentNumber");

CREATE TABLE "Company"."BankAccounts" (
    "BankAccountId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "CompanyId" UUID NOT NULL,
    "BankName" VARCHAR(200) NOT NULL,
    "AccountNumber" VARCHAR(50) NOT NULL,
    "CciCode" VARCHAR(20) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "FK_BankAccounts_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Company"."Companies"("CompanyId") ON DELETE CASCADE,
    CONSTRAINT "CK_BankAccounts_CciCode" CHECK (LENGTH("CciCode") = 20)
);

CREATE INDEX "IDX_BankAccounts_CompanyId" ON "Company"."BankAccounts"("CompanyId");
CREATE INDEX "IDX_BankAccounts_IsActive" ON "Company"."BankAccounts"("IsActive");

CREATE TABLE "Company"."NotificationSettings" (
    "NotificationSettingId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "CompanyId" UUID NOT NULL,
    "AuthorizeEmailNotifications" BOOLEAN NOT NULL DEFAULT TRUE,
    "NotificationEmail" VARCHAR(255) NOT NULL,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "FK_NotificationSettings_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Company"."Companies"("CompanyId") ON DELETE CASCADE,
    CONSTRAINT "UK_NotificationSettings_CompanyId" UNIQUE ("CompanyId")
);

CREATE INDEX "IDX_NotificationSettings_CompanyId" ON "Company"."NotificationSettings"("CompanyId");

-- =====================================================
-- SCHEMA: Document (Gestión de Documentos)
-- =====================================================

CREATE TYPE "Document"."DocumentType" AS ENUM (
    'Anexo1_DeclaracionJuradaDatos',
    'Anexo2_PactoIntegridad',
    'Anexo3_DeclaracionJuradaImpedimentos'
);

CREATE TABLE "Document"."GeneratedDocuments" (
    "GeneratedDocumentId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "CompanyId" UUID NOT NULL,
    "LegalRepresentativeId" UUID NULL,
    "DocumentType" "Document"."DocumentType" NOT NULL,
    "FileName" VARCHAR(500) NOT NULL,
    "FilePath" VARCHAR(1000) NOT NULL,
    "FileSize" BIGINT NULL,
    "LicitacionNumber" VARCHAR(100) NULL,
    "EntityName" VARCHAR(500) NULL,
    "GeneratedBy" UUID NOT NULL,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    
    CONSTRAINT "FK_GeneratedDocuments_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Company"."Companies"("CompanyId") ON DELETE CASCADE,
    CONSTRAINT "FK_GeneratedDocuments_LegalRepresentatives" FOREIGN KEY ("LegalRepresentativeId") 
        REFERENCES "Company"."LegalRepresentatives"("LegalRepresentativeId") ON DELETE SET NULL,
    CONSTRAINT "FK_GeneratedDocuments_Users" FOREIGN KEY ("GeneratedBy") 
        REFERENCES "Auth"."Users"("UserId") ON DELETE CASCADE
);

CREATE INDEX "IDX_GeneratedDocuments_CompanyId" ON "Document"."GeneratedDocuments"("CompanyId");
CREATE INDEX "IDX_GeneratedDocuments_DocumentType" ON "Document"."GeneratedDocuments"("DocumentType");
CREATE INDEX "IDX_GeneratedDocuments_CreatedDate" ON "Document"."GeneratedDocuments"("CreatedDate" DESC);
CREATE INDEX "IDX_GeneratedDocuments_GeneratedBy" ON "Document"."GeneratedDocuments"("GeneratedBy");

-- =====================================================
-- TABLA PARA AUDITORÍA
-- =====================================================

CREATE TABLE "Auth"."AuditLog" (
    "AuditLogId" UUID PRIMARY KEY DEFAULT uuidv7(),
    "UserId" UUID NOT NULL,
    "Action" VARCHAR(100) NOT NULL,
    "EntityName" VARCHAR(100) NULL,
    "EntityId" UUID NULL,
    "OldValues" JSONB NULL,
    "NewValues" JSONB NULL,
    "IpAddress" VARCHAR(45) NULL,
    "UserAgent" VARCHAR(500) NULL,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_AuditLog_Users" FOREIGN KEY ("UserId") 
        REFERENCES "Auth"."Users"("UserId") ON DELETE CASCADE
);

CREATE INDEX "IDX_AuditLog_UserId" ON "Auth"."AuditLog"("UserId");
CREATE INDEX "IDX_AuditLog_CreatedDate" ON "Auth"."AuditLog"("CreatedDate" DESC);
CREATE INDEX "IDX_AuditLog_Action" ON "Auth"."AuditLog"("Action");

-- =====================================================
-- TRIGGERS PARA UpdatedDate
-- =====================================================

CREATE OR REPLACE FUNCTION "UpdateTimestamp"()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedDate" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER "TR_Users_UpdateTimestamp"
    BEFORE UPDATE ON "Auth"."Users"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();

CREATE TRIGGER "TR_Companies_UpdateTimestamp"
    BEFORE UPDATE ON "Company"."Companies"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();

CREATE TRIGGER "TR_UserCompanies_UpdateTimestamp"
    BEFORE UPDATE ON "Company"."UserCompanies"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();

CREATE TRIGGER "TR_LegalRepresentatives_UpdateTimestamp"
    BEFORE UPDATE ON "Company"."LegalRepresentatives"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();

CREATE TRIGGER "TR_BankAccounts_UpdateTimestamp"
    BEFORE UPDATE ON "Company"."BankAccounts"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();

CREATE TRIGGER "TR_GeneratedDocuments_UpdateTimestamp"
    BEFORE UPDATE ON "Document"."GeneratedDocuments"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();

CREATE TRIGGER "TR_NotificationSettings_UpdateTimestamp"
    BEFORE UPDATE ON "Company"."NotificationSettings"
    FOR EACH ROW
    EXECUTE FUNCTION "UpdateTimestamp"();
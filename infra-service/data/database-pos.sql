-- =====================================================================
-- DATABASES SEPARADOS PARA MICROSSERVIÇOS - B-COMMERCE
-- Versão: 2.0
-- Data: 10/08/2025
-- Descrição: Schema dividido por domínio/microsserviço
-- =====================================================================

-- =====================================================================
-- CLIENT SERVICE DATABASE
-- =====================================================================
-- CREATE DATABASE client_service_db;
-- \c client_service_db;

-- ================================================
-- EXTENSÕES GLOBAIS
-- ================================================
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ================================================
-- ENUMS - CLIENT SERVICE
-- ================================================
CREATE TYPE address_type_enum AS ENUM ('shipping', 'billing');
CREATE TYPE consent_type_enum AS ENUM ('marketing_email', 'newsletter_subscription', 'terms_of_service', 'privacy_policy', 'cookies_essential', 'cookies_analytics', 'cookies_marketing');
CREATE TYPE card_brand_enum AS ENUM ('visa', 'mastercard', 'amex', 'elo', 'hipercard', 'diners_club', 'discover', 'jcb', 'aura', 'other');
CREATE TYPE user_role_enum AS ENUM ('customer', 'admin');
CREATE TYPE audit_operation_type_enum AS ENUM ('INSERT', 'UPDATE', 'DELETE', 'LOGIN_SUCCESS', 'LOGIN_FAILURE', 'PASSWORD_RESET_REQUEST', 'PASSWORD_RESET_SUCCESS', 'SYSTEM_ACTION');

-- ================================================
-- FUNÇÕES - CLIENT SERVICE
-- ================================================
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION is_cpf_valid(cpf TEXT)
RETURNS BOOLEAN AS $$
DECLARE
    cpf_clean TEXT;
    cpf_array INT[];
    sum1 INT := 0;
    sum2 INT := 0;
    i INT;
BEGIN
    cpf_clean := REGEXP_REPLACE(cpf, '[^0-9]', '', 'g');
    
    IF LENGTH(cpf_clean) != 11 OR cpf_clean ~ '(\d)\1{10}' THEN
        RETURN FALSE;
    END IF;
    
    cpf_array := STRING_TO_ARRAY(cpf_clean, NULL)::INT[];
    
    FOR i IN 1..9 LOOP
        sum1 := sum1 + cpf_array[i] * (11 - i);
    END LOOP;
    
    sum1 := 11 - (sum1 % 11);
    IF sum1 >= 10 THEN sum1 := 0; END IF;
    IF sum1 != cpf_array[10] THEN RETURN FALSE; END IF;
    
    FOR i IN 1..10 LOOP
        sum2 := sum2 + cpf_array[i] * (12 - i);
    END LOOP;
    
    sum2 := 11 - (sum2 % 11);
    IF sum2 >= 10 THEN sum2 := 0; END IF;
    IF sum2 != cpf_array[11] THEN RETURN FALSE; END IF;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

CREATE OR REPLACE FUNCTION trigger_log_address_history()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO address_history (address_id, client_id, address_snapshot)
    VALUES (OLD.address_id, OLD.client_id, to_jsonb(OLD));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ================================================
-- TABELAS - CLIENT SERVICE
-- ================================================

-- Tabela de Clientes
CREATE TABLE clients (
    client_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(155) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    email_verified_at TIMESTAMPTZ,
    phone VARCHAR(20),
    password_hash VARCHAR(255) NOT NULL,
    cpf VARCHAR(11) UNIQUE,
    date_of_birth DATE,
    newsletter_opt_in BOOLEAN NOT NULL DEFAULT FALSE,
    status VARCHAR(20) NOT NULL DEFAULT 'ativo' CHECK (status IN ('ativo', 'inativo', 'banido')),
    role user_role_enum NOT NULL DEFAULT 'customer',
    failed_login_attempts SMALLINT NOT NULL DEFAULT 0,
    account_locked_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT chk_cpf_valid CHECK (cpf IS NULL OR is_cpf_valid(cpf))
);

-- Tabela de Endereços
CREATE TABLE addresses (
    address_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    type address_type_enum NOT NULL,
    postal_code VARCHAR(8) NOT NULL,
    street VARCHAR(150) NOT NULL,
    street_number VARCHAR(20) NOT NULL,
    complement VARCHAR(100),
    neighborhood VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state_code VARCHAR(2) NOT NULL,
    country_code VARCHAR(2) NOT NULL DEFAULT 'BR',
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Histórico de Endereços
CREATE TABLE address_history (
    address_history_id BIGSERIAL PRIMARY KEY,
    address_id UUID NOT NULL,
    client_id UUID NOT NULL,
    address_snapshot JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Consentimentos (LGPD)
CREATE TABLE consents (
    consent_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    type consent_type_enum NOT NULL,
    terms_version VARCHAR(30),
    is_granted BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT uq_client_consent_type UNIQUE (client_id, type)
);

-- Tabela de Cartões Salvos
CREATE TABLE saved_cards (
    saved_card_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    nickname VARCHAR(50),
    last_four_digits VARCHAR(4) NOT NULL,
    brand card_brand_enum NOT NULL,
    gateway_token VARCHAR(255) NOT NULL UNIQUE,
    expiry_date DATE NOT NULL,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Tokens de Verificação de E-mail
CREATE TABLE email_verification_tokens (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Refresh Tokens
CREATE TABLE refresh_tokens (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    token_value VARCHAR(256) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Tokens Revogados (logout)
CREATE TABLE revoked_tokens (
    jti UUID PRIMARY KEY,
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    expires_at TIMESTAMPTZ NOT NULL
);

-- Tabela de Auditoria
CREATE TABLE audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(63) NOT NULL,
    record_id TEXT,
    operation_type audit_operation_type_enum NOT NULL,
    previous_data JSONB,
    new_data JSONB,
    change_description TEXT,
    user_identifier TEXT,
    user_ip_address INET,
    logged_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices para Client Service
CREATE UNIQUE INDEX idx_clients_active_email ON clients (email) WHERE deleted_at IS NULL;
CREATE INDEX idx_clients_status ON clients (status) WHERE deleted_at IS NULL;
CREATE INDEX idx_clients_role ON clients (role);
CREATE INDEX idx_addresses_client_id ON addresses (client_id);
CREATE UNIQUE INDEX uq_addresses_default_per_client_type ON addresses (client_id, type) WHERE is_default = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_consents_client_id ON consents (client_id);
CREATE INDEX idx_saved_cards_client_id ON saved_cards (client_id);
CREATE UNIQUE INDEX uq_saved_cards_default_per_client ON saved_cards (client_id) WHERE is_default = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_refresh_tokens_client_id ON refresh_tokens (client_id);
CREATE INDEX idx_revoked_tokens_expires_at ON revoked_tokens (expires_at);
CREATE INDEX idx_audit_log_table_record ON audit_log (table_name, record_id);
CREATE INDEX idx_audit_log_logged_at ON audit_log (logged_at DESC);

-- Triggers para Client Service
CREATE TRIGGER set_timestamp_clients BEFORE UPDATE ON clients FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_addresses BEFORE UPDATE ON addresses FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER log_address_changes BEFORE UPDATE ON addresses FOR EACH ROW WHEN (OLD.* IS DISTINCT FROM NEW.*) EXECUTE FUNCTION trigger_log_address_history();
CREATE TRIGGER set_timestamp_consents BEFORE UPDATE ON consents FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_saved_cards BEFORE UPDATE ON saved_cards FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- CATALOG SERVICE DATABASE
-- =====================================================================
-- CREATE DATABASE catalog_service_db;
-- \c catalog_service_db;

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ================================================
-- ENUMS - CATALOG SERVICE
-- ================================================
CREATE TYPE audit_operation_type_enum AS ENUM ('INSERT', 'UPDATE', 'DELETE', 'SYSTEM_ACTION');

-- ================================================
-- FUNÇÕES - CATALOG SERVICE
-- ================================================
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trigger_update_products_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector = 
        setweight(to_tsvector('portuguese', COALESCE(NEW.name, '')), 'A') ||
        setweight(to_tsvector('portuguese', COALESCE(NEW.base_sku, '')), 'A') ||
        setweight(to_tsvector('portuguese', COALESCE(NEW.description, '')), 'B');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ================================================
-- TABELAS - CATALOG SERVICE
-- ================================================

-- Tabela de Categorias
CREATE TABLE categories (
    category_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(150) NOT NULL UNIQUE,
    description TEXT,
    parent_category_id UUID REFERENCES categories(category_id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Marcas
CREATE TABLE brands (
    brand_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(150) NOT NULL UNIQUE,
    description TEXT,
    logo_url VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Cores
CREATE TABLE colors (
    color_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL UNIQUE,
    hex_code CHAR(7) UNIQUE CHECK (hex_code ~ '^#[0-9a-fA-F]{6}$'),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Tamanhos
CREATE TABLE sizes (
    size_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL UNIQUE,
    size_code VARCHAR(20) UNIQUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Produtos
CREATE TABLE products (
    product_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    base_sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(200) NOT NULL UNIQUE,
    description TEXT,
    category_id UUID NOT NULL REFERENCES categories(category_id) ON DELETE RESTRICT,
    brand_id UUID REFERENCES brands(brand_id) ON DELETE SET NULL,
    base_price NUMERIC(10,2) NOT NULL CHECK (base_price >= 0),
    sale_price NUMERIC(10,2) CHECK (sale_price IS NULL OR sale_price >= 0),
    sale_price_start_date TIMESTAMPTZ,
    sale_price_end_date TIMESTAMPTZ,
    stock_quantity INTEGER NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    weight_kg NUMERIC(6,3) CHECK (weight_kg IS NULL OR weight_kg > 0),
    height_cm INTEGER CHECK (height_cm IS NULL OR height_cm > 0),
    width_cm INTEGER CHECK (width_cm IS NULL OR width_cm > 0),
    depth_cm INTEGER CHECK (depth_cm IS NULL OR depth_cm > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    search_vector TSVECTOR,
    CONSTRAINT chk_sale_price CHECK (sale_price IS NULL OR sale_price < base_price),
    CONSTRAINT chk_sale_dates CHECK ((sale_price IS NULL) OR (sale_price IS NOT NULL AND sale_price_start_date IS NOT NULL AND sale_price_end_date IS NOT NULL))
);

-- Tabela de Imagens de Produtos
CREATE TABLE product_images (
    product_image_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    image_url VARCHAR(255) NOT NULL,
    alt_text VARCHAR(255),
    is_cover BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Variações de Produto
CREATE TABLE product_variants (
    product_variant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    sku VARCHAR(50) NOT NULL UNIQUE,
    color_id UUID REFERENCES colors(color_id) ON DELETE RESTRICT,
    size_id UUID REFERENCES sizes(size_id) ON DELETE RESTRICT,
    stock_quantity INTEGER NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    additional_price NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    image_url VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT uq_product_variant_attributes UNIQUE (product_id, color_id, size_id)
);

-- Tabela de Reviews (agora no catalog service)
CREATE TABLE reviews (
    review_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL, -- Sem FK, comunicação via eventos
    product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    order_id UUID, -- Sem FK, comunicação via eventos
    rating SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment TEXT,
    is_approved BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Auditoria
CREATE TABLE audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(63) NOT NULL,
    record_id TEXT,
    operation_type audit_operation_type_enum NOT NULL,
    previous_data JSONB,
    new_data JSONB,
    change_description TEXT,
    user_identifier TEXT,
    user_ip_address INET,
    logged_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices para Catalog Service
CREATE INDEX idx_categories_parent_category_id ON categories (parent_category_id) WHERE parent_category_id IS NOT NULL;
CREATE INDEX idx_categories_is_active ON categories (is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_brands_is_active ON brands (is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_category_id ON products (category_id);
CREATE INDEX idx_products_brand_id ON products (brand_id) WHERE brand_id IS NOT NULL;
CREATE INDEX idx_products_is_active ON products (is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_search_vector ON products USING GIN (search_vector);
CREATE INDEX idx_product_images_product_id ON product_images (product_id);
CREATE UNIQUE INDEX uq_product_images_cover_per_product ON product_images (product_id) WHERE is_cover = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_product_variants_product_id ON product_variants (product_id);
CREATE INDEX idx_product_variants_color_id ON product_variants (color_id);
CREATE INDEX idx_product_variants_size_id ON product_variants (size_id);
CREATE INDEX idx_reviews_client_id ON reviews (client_id);
CREATE INDEX idx_reviews_product_id ON reviews (product_id);
CREATE INDEX idx_reviews_is_approved ON reviews (is_approved);
CREATE INDEX idx_audit_log_table_record ON audit_log (table_name, record_id);
CREATE INDEX idx_audit_log_logged_at ON audit_log (logged_at DESC);

-- Triggers para Catalog Service
CREATE TRIGGER set_timestamp_categories BEFORE UPDATE ON categories FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_brands BEFORE UPDATE ON brands FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_colors BEFORE UPDATE ON colors FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_sizes BEFORE UPDATE ON sizes FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_products BEFORE UPDATE ON products FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER update_search_vector_trigger BEFORE INSERT OR UPDATE ON products FOR EACH ROW EXECUTE FUNCTION trigger_update_products_search_vector();
CREATE TRIGGER set_timestamp_product_images BEFORE UPDATE ON product_images FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_product_variants BEFORE UPDATE ON product_variants FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_reviews BEFORE UPDATE ON reviews FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- SALES SERVICE DATABASE
-- =====================================================================
-- CREATE DATABASE sales_service_db;
-- \c sales_service_db;

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ================================================
-- ENUMS - SALES SERVICE
-- ================================================
CREATE TYPE order_status_enum AS ENUM ('pending', 'processing', 'shipped', 'delivered', 'canceled', 'returned');
CREATE TYPE payment_method_enum AS ENUM ('credit_card', 'debit_card', 'pix', 'bank_slip');
CREATE TYPE payment_status_enum AS ENUM ('pending', 'approved', 'declined', 'refunded', 'partially_refunded', 'chargeback', 'error');
CREATE TYPE address_type_enum AS ENUM ('shipping', 'billing');
CREATE TYPE coupon_type AS ENUM ('general', 'user_specific');
CREATE TYPE audit_operation_type_enum AS ENUM ('INSERT', 'UPDATE', 'DELETE', 'ORDER_STATUS_CHANGE', 'PAYMENT_STATUS_CHANGE', 'SYSTEM_ACTION');

-- ================================================
-- FUNÇÕES - SALES SERVICE
-- ================================================
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION generate_order_code()
RETURNS VARCHAR AS $$
BEGIN
    RETURN 'ORD-' || TO_CHAR(CURRENT_DATE, 'YYYY-') || UPPER(SUBSTRING(REPLACE(gen_random_uuid()::text, '-', ''), 1, 8));
END;
$$ LANGUAGE plpgsql VOLATILE;

-- ================================================
-- TABELAS - SALES SERVICE
-- ================================================

-- Tabela de Cupons
CREATE TABLE coupons (
    coupon_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    discount_percentage NUMERIC(5,2),
    discount_amount NUMERIC(10,2),
    valid_from TIMESTAMPTZ NOT NULL,
    valid_until TIMESTAMPTZ NOT NULL,
    max_uses INTEGER,
    times_used INTEGER NOT NULL DEFAULT 0,
    min_purchase_amount NUMERIC(10,2),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    type coupon_type NOT NULL DEFAULT 'general',
    client_id UUID, -- Sem FK, comunicação via eventos
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT chk_discount_type CHECK ( 
        (discount_percentage IS NOT NULL AND discount_amount IS NULL) OR 
        (discount_percentage IS NULL AND discount_amount IS NOT NULL) 
    ),
    CONSTRAINT chk_valid_until CHECK (valid_until > valid_from)
);

-- Tabela de Pedidos
CREATE TABLE orders (
    order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference_code VARCHAR(20) UNIQUE NOT NULL DEFAULT generate_order_code(),
    client_id UUID NOT NULL, -- Sem FK, comunicação via eventos
    coupon_id UUID REFERENCES coupons(coupon_id) ON DELETE SET NULL,
    status order_status_enum NOT NULL DEFAULT 'pending',
    items_total_amount NUMERIC(10,2) NOT NULL CHECK (items_total_amount >= 0),
    discount_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    shipping_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    grand_total_amount NUMERIC(12,2) GENERATED ALWAYS AS (items_total_amount - discount_amount + shipping_amount) STORED,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Itens do Pedido
CREATE TABLE order_items (
    order_item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    product_variant_id UUID NOT NULL, -- Sem FK, comunicação via eventos
    item_sku VARCHAR(100) NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(10,2) NOT NULL,
    line_item_total_amount NUMERIC(12,2) GENERATED ALWAYS AS (unit_price * quantity) STORED,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Endereços do Pedido (snapshot no momento da compra)
CREATE TABLE order_addresses (
    order_address_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    address_type address_type_enum NOT NULL,
    recipient_name VARCHAR(255) NOT NULL,
    postal_code CHAR(8) NOT NULL,
    street VARCHAR(150) NOT NULL,
    street_number VARCHAR(20) NOT NULL,
    complement VARCHAR(100),
    neighborhood VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state_code CHAR(2) NOT NULL,
    country_code CHAR(2) NOT NULL DEFAULT 'BR',
    phone VARCHAR(20),
    original_address_id UUID, -- Sem FK, referência apenas
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Pagamentos
CREATE TABLE payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(order_id) ON DELETE RESTRICT,
    method payment_method_enum NOT NULL,
    status payment_status_enum NOT NULL DEFAULT 'pending',
    amount NUMERIC(10,2) NOT NULL,
    transaction_id VARCHAR(100) UNIQUE,
    method_details JSONB,
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version INTEGER NOT NULL DEFAULT 1
);

-- Tabela de Auditoria
CREATE TABLE audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(63) NOT NULL,
    record_id TEXT,
    operation_type audit_operation_type_enum NOT NULL,
    previous_data JSONB,
    new_data JSONB,
    change_description TEXT,
    user_identifier TEXT,
    user_ip_address INET,
    logged_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices para Sales Service
CREATE INDEX idx_coupons_client_id ON coupons (client_id) WHERE type = 'user_specific';
CREATE INDEX idx_coupons_is_active_and_valid ON coupons (is_active, valid_until) WHERE deleted_at IS NULL;
CREATE INDEX idx_orders_client_id ON orders (client_id);
CREATE INDEX idx_orders_coupon_id ON orders (coupon_id) WHERE coupon_id IS NOT NULL;
CREATE INDEX idx_orders_status ON orders (status);
CREATE INDEX idx_orders_created_at ON orders (created_at DESC);
CREATE INDEX idx_order_items_order_id ON order_items (order_id);
CREATE INDEX idx_order_items_product_variant_id ON order_items (product_variant_id);
CREATE INDEX idx_order_addresses_order_id ON order_addresses (order_id);
CREATE INDEX idx_payments_order_id ON payments (order_id);
CREATE INDEX idx_payments_status ON payments (status);
CREATE INDEX idx_payments_method_details_gin ON payments USING GIN (method_details);
CREATE INDEX idx_audit_log_table_record ON audit_log (table_name, record_id);
CREATE INDEX idx_audit_log_logged_at ON audit_log (logged_at DESC);

-- Triggers para Sales Service
CREATE TRIGGER set_timestamp_coupons BEFORE UPDATE ON coupons FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_orders BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_order_items BEFORE UPDATE ON order_items FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER set_timestamp_payments BEFORE UPDATE ON payments FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- CART SERVICE DATABASE (MongoDB - Estrutura de Documentos)
-- =====================================================================
-- Use MongoDB para o Cart Service
-- Exemplo de estrutura de documento:
/*
{
  "_id": ObjectId("..."),
  "cart_id": "uuid",
  "client_id": "uuid",
  "items": [
    {
      "product_variant_id": "uuid",
      "sku": "string",
      "name": "string",
      "quantity": 2,
      "unit_price": 1299.99,
      "currency": "BRL",
      "added_at": ISODate("...")
    }
  ],
  "totals": {
    "items_total": 2599.98,
    "currency": "BRL"
  },
  "created_at": ISODate("..."),
  "updated_at": ISODate("..."),
  "expires_at": ISODate("...")
}
*/

-- =====================================================================
-- EVENTOS DE DOMÍNIO (Para cada serviço)
-- =====================================================================

-- =====================================================================
-- EVENTOS DE DOMÍNIO (Outbox Pattern - Para cada serviço)
-- =====================================================================

-- Tabela comum para todos os serviços (deve ser replicada em cada DB)
CREATE TABLE domain_events (
    event_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(200) NOT NULL,
    event_data JSONB NOT NULL,
    event_version INTEGER NOT NULL DEFAULT 1,
    correlation_id UUID,
    causation_id UUID,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMPTZ,
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    retry_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices para domain_events
CREATE INDEX idx_domain_events_unpublished ON domain_events (is_published, created_at) WHERE is_published = FALSE;
CREATE INDEX idx_domain_events_aggregate ON domain_events (aggregate_type, aggregate_id);
CREATE INDEX idx_domain_events_correlation ON domain_events (correlation_id) WHERE correlation_id IS NOT NULL;

-- Função para publicar eventos
CREATE OR REPLACE FUNCTION publish_domain_event(
    p_aggregate_id UUID,
    p_aggregate_type VARCHAR,
    p_event_type VARCHAR,
    p_event_data JSONB,
    p_correlation_id UUID DEFAULT NULL
)
RETURNS UUID AS $
DECLARE
    v_event_id UUID;
BEGIN
    INSERT INTO domain_events (
        aggregate_id, 
        aggregate_type, 
        event_type, 
        event_data, 
        correlation_id
    )
    VALUES (
        p_aggregate_id,
        p_aggregate_type,
        p_event_type,
        p_event_data,
        p_correlation_id
    )
    RETURNING event_id INTO v_event_id;
    
    RETURN v_event_id;
END;
$ LANGUAGE plpgsql;

-- =====================================================================
-- DOCKER-COMPOSE PARA MICROSSERVIÇOS
-- =====================================================================

/*
version: '3.8'

services:
  # Client Service Database
  client-service-db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: client_service_db
      POSTGRES_USER: client_service
      POSTGRES_PASSWORD: client_pass
    ports:
      - "5432:5432"
    volumes:
      - client_db_data:/var/lib/postgresql/data
      - ./databases/client-service/init.sql:/docker-entrypoint-initdb.d/init.sql

  # Catalog Service Database
  catalog-service-db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: catalog_service_db
      POSTGRES_USER: catalog_service
      POSTGRES_PASSWORD: catalog_pass
    ports:
      - "5433:5432"
    volumes:
      - catalog_db_data:/var/lib/postgresql/data
      - ./databases/catalog-service/init.sql:/docker-entrypoint-initdb.d/init.sql

  # Sales Service Database
  sales-service-db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: sales_service_db
      POSTGRES_USER: sales_service
      POSTGRES_PASSWORD: sales_pass
    ports:
      - "5434:5432"
    volumes:
      - sales_db_data:/var/lib/postgresql/data
      - ./databases/sales-service/init.sql:/docker-entrypoint-initdb.d/init.sql

  # Cart Service Database (MongoDB)
  cart-service-db:
    image: mongo:7
    environment:
      MONGO_INITDB_ROOT_USERNAME: cart_service
      MONGO_INITDB_ROOT_PASSWORD: cart_pass
      MONGO_INITDB_DATABASE: cart_service_db
    ports:
      - "27017:27017"
    volumes:
      - cart_db_data:/data/db
      - ./databases/cart-service/init.js:/docker-entrypoint-initdb.d/init.js

  # RabbitMQ para comunicação entre serviços
  rabbitmq:
    image: rabbitmq:3-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: b_commerce
      RABBITMQ_DEFAULT_PASS: b_commerce_pass
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

  # Keycloak para autenticação
  keycloak:
    image: quay.io/keycloak/keycloak:23.0.0
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://keycloak-db:5432/keycloak
      KC_DB_USERNAME: keycloak
      KC_DB_PASSWORD: keycloak
    ports:
      - "8080:8080"
    command: start-dev
    depends_on:
      - keycloak-db

  # Keycloak Database
  keycloak-db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: keycloak
      POSTGRES_USER: keycloak
      POSTGRES_PASSWORD: keycloak
    volumes:
      - keycloak_db_data:/var/lib/postgresql/data

volumes:
  client_db_data:
  catalog_db_data:
  sales_db_data:
  cart_db_data:
  rabbitmq_data:
  keycloak_db_data:
*/

-- =====================================================================
-- CONNECTION STRINGS POR SERVIÇO
-- =====================================================================

/*
CLIENT SERVICE:
Server=localhost;Port=5432;Database=client_service_db;User Id=client_service;Password=client_pass;

CATALOG SERVICE:
Server=localhost;Port=5433;Database=catalog_service_db;User Id=catalog_service;Password=catalog_pass;

SALES SERVICE:
Server=localhost;Port=5434;Database=sales_service_db;User Id=sales_service;Password=sales_pass;

CART SERVICE (MongoDB):
mongodb://cart_service:cart_pass@localhost:27017/cart_service_db

RABBITMQ:
amqp://b_commerce:b_commerce_pass@localhost:5672/

KEYCLOAK:
http://localhost:8080/realms/b-commerce-realm
*/

-- =====================================================================
-- EVENTOS INTER-SERVIÇOS
-- =====================================================================

/*
CLIENT SERVICE EVENTS:
- UserRegistered: { client_id, email, first_name, last_name }
- UserUpdated: { client_id, changes }
- UserBlocked: { client_id, reason }
- AddressUpdated: { client_id, address_id, type }
- ConsentUpdated: { client_id, consent_type, is_granted }

CATALOG SERVICE EVENTS:
- ProductCreated: { product_id, base_sku, name, category_id }
- ProductUpdated: { product_id, changes }
- StockChanged: { product_variant_id, previous_stock, current_stock }
- PriceChanged: { product_id, previous_price, current_price }
- ReviewSubmitted: { review_id, product_id, client_id, rating }

CART SERVICE EVENTS:
- ItemAddedToCart: { client_id, product_variant_id, quantity }
- ItemRemovedFromCart: { client_id, product_variant_id }
- CartAbandoned: { client_id, cart_id, items }
- CartCleared: { client_id, cart_id }

SALES SERVICE EVENTS:
- OrderCreated: { order_id, client_id, items }
- OrderStatusChanged: { order_id, previous_status, current_status }
- PaymentProcessed: { order_id, payment_id, status, amount }
- OrderShipped: { order_id, tracking_code }
- OrderDelivered: { order_id, delivered_at }
*/

-- =====================================================================
-- VIEWS PARA REPORTS (Cross-Service)
-- =====================================================================

-- Para relatórios que necessitam dados de múltiplos serviços,
-- usar Event Sourcing ou Read Models (CQRS)

-- Exemplo: View materializada para relatório de vendas
-- (seria populada via eventos dos outros serviços)
CREATE TABLE sales_report_view (
    report_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL,
    client_id UUID NOT NULL,
    client_email VARCHAR(255) NOT NULL,
    product_id UUID NOT NULL,
    product_name VARCHAR(255) NOT NULL,
    category_name VARCHAR(100),
    brand_name VARCHAR(100),
    quantity INTEGER NOT NULL,
    unit_price NUMERIC(10,2) NOT NULL,
    total_amount NUMERIC(12,2) NOT NULL,
    order_status VARCHAR(50) NOT NULL,
    payment_status VARCHAR(50),
    order_date TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices para relatórios
CREATE INDEX idx_sales_report_client_id ON sales_report_view (client_id);
CREATE INDEX idx_sales_report_order_date ON sales_report_view (order_date DESC);
CREATE INDEX idx_sales_report_product_id ON sales_report_view (product_id);
CREATE INDEX idx_sales_report_order_status ON sales_report_view (order_status);

-- =====================================================================
-- MIGRATION STRATEGY
-- =====================================================================

/*
FASE 1: Preparação
1. Criar os novos bancos de dados
2. Executar os schemas específicos
3. Implementar eventos de domínio em cada serviço

FASE 2: Migração de Dados
1. Migrar dados do banco monolítico para os bancos específicos
2. Remover foreign keys entre domínios diferentes
3. Implementar validação via eventos

FASE 3: Comunicação Assíncrona
1. Implementar publishers/consumers para cada serviço
2. Configurar retry policies e dead letter queues
3. Implementar saga patterns para transações distribuídas

FASE 4: Validação
1. Testes de integração entre serviços
2. Monitoramento de eventos e mensagens
3. Rollback plan se necessário
*/

-- =====================================================================
-- OBSERVABILIDADE E MONITORING
-- =====================================================================

-- Tabela para tracking de mensagens (em cada serviço)
CREATE TABLE message_tracking (
    tracking_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    correlation_id UUID NOT NULL,
    event_type VARCHAR(200) NOT NULL,
    service_name VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL, -- sent, received, processed, failed
    payload JSONB,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMPTZ
);

CREATE INDEX idx_message_tracking_correlation ON message_tracking (correlation_id);
CREATE INDEX idx_message_tracking_status ON message_tracking (status);
CREATE INDEX idx_message_tracking_created_at ON message_tracking (created_at DESC);

-- =====================================================================
-- FIM DO SCHEMA PARA MICROSSERVIÇOS
-- =====================================================================
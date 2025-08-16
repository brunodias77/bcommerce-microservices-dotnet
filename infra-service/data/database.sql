-- =====================================================================
-- SCHEMA DE BANCO DE DADOS PARA E-COMMERCE - MICROSSERVIÇOS
-- Versão: 1.0
-- Data: 27/07/2025
-- Descrição: Schema completo para todos os microsserviços
-- =====================================================================

-- ================================================
-- EXTENSÕES
-- ================================================
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ================================================
-- ENUMS (Tipos de Dados Customizados)
-- ================================================
CREATE TYPE coupon_type AS ENUM ('general', 'user_specific');
CREATE TYPE order_status_enum AS ENUM ('pending', 'processing', 'shipped', 'delivered', 'canceled', 'returned');
CREATE TYPE payment_method_enum AS ENUM ('credit_card', 'debit_card', 'pix', 'bank_slip');
CREATE TYPE payment_status_enum AS ENUM ('pending', 'approved', 'declined', 'refunded', 'partially_refunded', 'chargeback', 'error');
CREATE TYPE address_type_enum AS ENUM ('shipping', 'billing');
CREATE TYPE consent_type_enum AS ENUM ('marketing_email', 'newsletter_subscription', 'terms_of_service', 'privacy_policy', 'cookies_essential', 'cookies_analytics', 'cookies_marketing');
CREATE TYPE card_brand_enum AS ENUM ('visa', 'mastercard', 'amex', 'elo', 'hipercard', 'diners_club', 'discover', 'jcb', 'aura', 'other');
CREATE TYPE audit_operation_type_enum AS ENUM ('INSERT', 'UPDATE', 'DELETE', 'LOGIN_SUCCESS', 'LOGIN_FAILURE', 'PASSWORD_RESET_REQUEST', 'PASSWORD_RESET_SUCCESS', 'ORDER_STATUS_CHANGE', 'PAYMENT_STATUS_CHANGE', 'SYSTEM_ACTION');
CREATE TYPE user_role_enum AS ENUM ('customer', 'admin');

-- ================================================
-- FUNÇÕES GLOBAIS E DE APOIO
-- ================================================

-- Função para atualizar a coluna 'updated_at' automaticamente
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Função para gerar códigos de pedido únicos
CREATE OR REPLACE FUNCTION generate_order_code()
RETURNS VARCHAR AS $$
BEGIN
    RETURN 'ORD-' || TO_CHAR(CURRENT_DATE, 'YYYY-') || UPPER(SUBSTRING(REPLACE(gen_random_uuid()::text, '-', ''), 1, 8));
END;
$$ LANGUAGE plpgsql VOLATILE;

-- Função para validar a estrutura de um CPF
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

-- Função para registrar o histórico de alterações de endereços
CREATE OR REPLACE FUNCTION trigger_log_address_history()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO address_history (address_id, client_id, address_snapshot)
    VALUES (OLD.address_id, OLD.client_id, to_jsonb(OLD));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Função para atualizar o vetor de busca (Full-Text Search) da tabela de produtos
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

-- Função de gatilho genérica para popular a tabela de auditoria
CREATE OR REPLACE FUNCTION trigger_log_audit()
RETURNS TRIGGER AS $$
DECLARE
    audit_row audit_log;
    record_id TEXT;
    user_identifier TEXT;
    excluded_cols TEXT[] = ARRAY['created_at', 'updated_at', 'version', 'password_hash', 'search_vector'];
BEGIN
    -- Tenta obter o identificador do usuário a partir de uma variável de sessão
    BEGIN
        user_identifier := CURRENT_SETTING('app.current_user', true);
    EXCEPTION WHEN OTHERS THEN
        user_identifier := 'system';
    END;
    
    -- Extrai o ID do registro (PK) dinamicamente
    IF TG_ARGV[0] IS NOT NULL THEN
        IF (TG_OP = 'DELETE') THEN
            EXECUTE 'SELECT ($1).' || quote_ident(TG_ARGV[0]) INTO record_id USING OLD;
        ELSE
            EXECUTE 'SELECT ($1).' || quote_ident(TG_ARGV[0]) INTO record_id USING NEW;
        END IF;
    END IF;
    
    audit_row = ROW(
        NEXTVAL('audit_log_audit_log_id_seq'),
        TG_TABLE_NAME,
        record_id,
        TG_OP::audit_operation_type_enum,
        NULL,
        NULL,
        NULL,
        user_identifier,
        INET_CLIENT_ADDR(),
        CURRENT_TIMESTAMP
    );
    
    IF (TG_OP = 'UPDATE') THEN
        audit_row.previous_data = to_jsonb(OLD) - excluded_cols;
        audit_row.new_data = to_jsonb(NEW) - excluded_cols;
    ELSIF (TG_OP = 'DELETE') THEN
        audit_row.previous_data = to_jsonb(OLD) - excluded_cols;
    ELSIF (TG_OP = 'INSERT') THEN
        audit_row.new_data = to_jsonb(NEW) - excluded_cols;
    END IF;
    
    INSERT INTO audit_log VALUES (audit_row.*);
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- ================================================
-- ESTRUTURA DE TABELAS
-- ================================================

-- Tabela de Clientes
CREATE TABLE clients (
    client_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    keycloak_user_id UUID UNIQUE, -- vínculo com Keycloak
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

-- Índices para clients
CREATE UNIQUE INDEX idx_clients_active_email ON clients (email) WHERE deleted_at IS NULL;
CREATE INDEX idx_clients_status ON clients (status) WHERE deleted_at IS NULL;
CREATE INDEX idx_clients_role ON clients (role);

-- Triggers para clients
CREATE TRIGGER set_timestamp_clients 
    BEFORE UPDATE ON clients 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER audit_clients_trigger 
    AFTER INSERT OR UPDATE OR DELETE ON clients 
    FOR EACH ROW EXECUTE FUNCTION trigger_log_audit('client_id');

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

-- Índices para addresses
CREATE INDEX idx_addresses_client_id ON addresses (client_id);
CREATE UNIQUE INDEX uq_addresses_default_per_client_type ON addresses (client_id, type) 
    WHERE is_default = TRUE AND deleted_at IS NULL;

-- Triggers para addresses
CREATE TRIGGER set_timestamp_addresses 
    BEFORE UPDATE ON addresses 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER log_address_changes 
    BEFORE UPDATE ON addresses 
    FOR EACH ROW WHEN (OLD.* IS DISTINCT FROM NEW.*) 
    EXECUTE FUNCTION trigger_log_address_history();

CREATE TRIGGER audit_addresses_trigger 
    AFTER INSERT OR UPDATE OR DELETE ON addresses 
    FOR EACH ROW EXECUTE FUNCTION trigger_log_audit('address_id');

-- Tabela de Histórico de Endereços
CREATE TABLE address_history (
    address_history_id BIGSERIAL PRIMARY KEY,
    address_id UUID NOT NULL,
    client_id UUID NOT NULL,
    address_snapshot JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_address_history_address_id ON address_history (address_id);

-- Tabela de Categorias de Produtos
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

-- Índices para categories
CREATE INDEX idx_categories_parent_category_id ON categories (parent_category_id) WHERE parent_category_id IS NOT NULL;
CREATE INDEX idx_categories_is_active ON categories (is_active) WHERE deleted_at IS NULL;

-- Triggers para categories
CREATE TRIGGER set_timestamp_categories 
    BEFORE UPDATE ON categories 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

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

-- Índices para brands
CREATE INDEX idx_brands_is_active ON brands (is_active) WHERE deleted_at IS NULL;

-- Triggers para brands
CREATE TRIGGER set_timestamp_brands 
    BEFORE UPDATE ON brands 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

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

-- Índices para products
CREATE INDEX idx_products_category_id ON products (category_id);
CREATE INDEX idx_products_brand_id ON products (brand_id) WHERE brand_id IS NOT NULL;
CREATE INDEX idx_products_is_active ON products (is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_search_vector ON products USING GIN (search_vector);

-- Triggers para products
CREATE TRIGGER set_timestamp_products 
    BEFORE UPDATE ON products 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER update_search_vector_trigger 
    BEFORE INSERT OR UPDATE ON products 
    FOR EACH ROW EXECUTE FUNCTION trigger_update_products_search_vector();

CREATE TRIGGER audit_products_trigger 
    AFTER INSERT OR UPDATE OR DELETE ON products 
    FOR EACH ROW EXECUTE FUNCTION trigger_log_audit('product_id');

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

-- Índices para product_images
CREATE INDEX idx_product_images_product_id ON product_images (product_id);
CREATE UNIQUE INDEX uq_product_images_cover_per_product ON product_images (product_id) 
    WHERE is_cover = TRUE AND deleted_at IS NULL;

-- Triggers para product_images
CREATE TRIGGER set_timestamp_product_images 
    BEFORE UPDATE ON product_images 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabelas de Variação (Cores e Tamanhos)
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

CREATE TRIGGER set_timestamp_colors 
    BEFORE UPDATE ON colors 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

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

CREATE TRIGGER set_timestamp_sizes 
    BEFORE UPDATE ON sizes 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

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

-- Índices para product_variants
CREATE INDEX idx_product_variants_product_id ON product_variants (product_id);
CREATE INDEX idx_product_variants_color_id ON product_variants (color_id);
CREATE INDEX idx_product_variants_size_id ON product_variants (size_id);

CREATE TRIGGER set_timestamp_product_variants 
    BEFORE UPDATE ON product_variants 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

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
    client_id UUID REFERENCES clients(client_id) ON DELETE RESTRICT,
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

-- Índices para coupons
CREATE INDEX idx_coupons_client_id ON coupons (client_id) WHERE type = 'user_specific';
CREATE INDEX idx_coupons_is_active_and_valid ON coupons (is_active, valid_until) WHERE deleted_at IS NULL;

CREATE TRIGGER set_timestamp_coupons 
    BEFORE UPDATE ON coupons 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de Pedidos
CREATE TABLE orders (
    order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference_code VARCHAR(20) UNIQUE NOT NULL DEFAULT generate_order_code(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE RESTRICT,
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

-- Índices para orders
CREATE INDEX idx_orders_client_id ON orders (client_id);
CREATE INDEX idx_orders_coupon_id ON orders (coupon_id) WHERE coupon_id IS NOT NULL;
CREATE INDEX idx_orders_status ON orders (status);
CREATE INDEX idx_orders_created_at ON orders (created_at DESC);

CREATE TRIGGER set_timestamp_orders 
    BEFORE UPDATE ON orders 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER audit_orders_trigger 
    AFTER INSERT OR UPDATE OR DELETE ON orders 
    FOR EACH ROW EXECUTE FUNCTION trigger_log_audit('order_id');

-- Tabela de Itens do Pedido
CREATE TABLE order_items (
    order_item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    product_variant_id UUID REFERENCES product_variants(product_variant_id) ON DELETE SET NULL,
    item_sku VARCHAR(100) NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(10,2) NOT NULL,
    line_item_total_amount NUMERIC(12,2) GENERATED ALWAYS AS (unit_price * quantity) STORED,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version INTEGER NOT NULL DEFAULT 1
);

-- Índices para order_items
CREATE INDEX idx_order_items_order_id ON order_items (order_id);
CREATE INDEX idx_order_items_product_variant_id ON order_items (product_variant_id);

CREATE TRIGGER set_timestamp_order_items 
    BEFORE UPDATE ON order_items 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de Endereços do Pedido (snapshot do endereço no momento da compra)
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
    original_address_id UUID REFERENCES addresses(address_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_order_addresses_order_id ON order_addresses (order_id);

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

-- Índices para payments
CREATE INDEX idx_payments_order_id ON payments (order_id);
CREATE INDEX idx_payments_status ON payments (status);
CREATE INDEX idx_payments_method_details_gin ON payments USING GIN (method_details);

CREATE TRIGGER set_timestamp_payments 
    BEFORE UPDATE ON payments 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de Carrinhos de Compra
CREATE TABLE carts (
    cart_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID UNIQUE REFERENCES clients(client_id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ
);

CREATE TRIGGER set_timestamp_carts 
    BEFORE UPDATE ON carts 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de Itens do Carrinho
CREATE TABLE cart_items (
    cart_item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id UUID NOT NULL REFERENCES carts(cart_id) ON DELETE CASCADE,
    product_variant_id UUID NOT NULL REFERENCES product_variants(product_variant_id) ON DELETE CASCADE,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(10,2) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'BRL',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_cart_item_variant UNIQUE (cart_id, product_variant_id)
);

-- Índices para cart_items
CREATE INDEX idx_cart_items_cart_id ON cart_items (cart_id);
CREATE INDEX idx_cart_items_product_variant_id ON cart_items (product_variant_id);

CREATE TRIGGER set_timestamp_cart_items 
    BEFORE UPDATE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de Avaliações de Produto
CREATE TABLE reviews (
    review_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID REFERENCES clients(client_id) ON DELETE SET NULL,
    product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    order_id UUID REFERENCES orders(order_id) ON DELETE SET NULL,
    rating SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment TEXT,
    is_approved BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

-- Índices para reviews
CREATE INDEX idx_reviews_client_id ON reviews (client_id);
CREATE INDEX idx_reviews_product_id ON reviews (product_id);
CREATE INDEX idx_reviews_is_approved ON reviews (is_approved);

CREATE TRIGGER set_timestamp_reviews 
    BEFORE UPDATE ON reviews 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de tokens de verificação de e-mail
CREATE TABLE email_verification_tokens (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_evt_client_id ON email_verification_tokens (client_id);

-- Tabela de consentimentos (LGPD)
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

-- Índices para consents
CREATE INDEX idx_consents_client_id ON consents (client_id);

CREATE TRIGGER set_timestamp_consents 
    BEFORE UPDATE ON consents 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de cartões salvos
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

-- Índices para saved_cards
CREATE INDEX idx_saved_cards_client_id ON saved_cards (client_id);
CREATE UNIQUE INDEX uq_saved_cards_default_per_client ON saved_cards (client_id) 
    WHERE is_default = TRUE AND deleted_at IS NULL;

CREATE TRIGGER set_timestamp_saved_cards 
    BEFORE UPDATE ON saved_cards 
    FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de Logs de Auditoria
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

-- Índices para audit_log
CREATE INDEX idx_audit_log_table_record ON audit_log (table_name, record_id);
CREATE INDEX idx_audit_log_logged_at ON audit_log (logged_at DESC);
CREATE INDEX idx_audit_log_user_identifier ON audit_log (user_identifier);

-- Tabela para Refresh Tokens
CREATE TABLE refresh_tokens (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    token_value VARCHAR(256) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX idx_refresh_tokens_client_id ON refresh_tokens (client_id);

-- Tabela para tokens revogados (logout)
CREATE TABLE revoked_tokens (
    jti UUID PRIMARY KEY,
    client_id UUID NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    expires_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_revoked_tokens_expires_at ON revoked_tokens (expires_at);

-- =====================================================================
-- FIM DO SCRIPT DE SCHEMA
-- =====================================================================
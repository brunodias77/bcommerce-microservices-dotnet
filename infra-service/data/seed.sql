-- =====================================================================
-- SCRIPT DE SEED PARA E-COMMERCE - DADOS DE TESTE
-- Versão: 1.0
-- Data: 27/07/2025
-- Descrição: Dados iniciais para desenvolvimento e testes
-- =====================================================================

-- =====================================================================
-- BLOCO 1: USUÁRIOS E CLIENTES
-- =====================================================================

-- Inserir clientes de teste
INSERT INTO clients (client_id, first_name, last_name, email, password_hash, role, status, email_verified_at, phone, cpf, date_of_birth, newsletter_opt_in)
VALUES
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'Bruno', 'Dias', 'bruno@admin.com', '$2a$11$nEF/xbFnmHZa60Hsk/WZPedbkaIWwIrTAk5s0oSroeat0IZlfs8GO', 'admin', 'ativo', '2025-06-22 22:43:21.744035+00', '14991781010', NULL, NULL, true),
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'Ana', 'Silva', 'ana.silva@exemplo.com', '$2a$11$nEF/xbFnmHZa60Hsk/WZPedbkaIWwIrTAk5s0oSroeat0IZlfs8GO', 'customer', 'ativo', CURRENT_TIMESTAMP, '11987654321', '95366490049', '1990-05-15', true),
    ('b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', 'Carlos', 'Pereira', 'carlos.p@exemplo.com', '$2a$11$nEF/xbFnmHZa60Hsk/WZPedbkaIWwIrTAk5s0oSroeat0IZlfs8GO', 'customer', 'ativo', NULL, '21912345678', '96455471059', '1985-11-20', false),
    ('c1c2c3c4-d1d2-e1e2-f1f2-a1a2a3a4a5a6', 'Mariana', 'Costa', 'mariana.costa@exemplo.com', '$2a$11$nEF/xbFnmHZa60Hsk/WZPedbkaIWwIrTAk5s0oSroeat0IZlfs8GO', 'customer', 'inativo', CURRENT_TIMESTAMP, '31988887777', '82484394020', '2000-02-10', true);

-- Inserir endereços para os clientes
INSERT INTO addresses (client_id, type, postal_code, street, street_number, complement, neighborhood, city, state_code, is_default)
VALUES
    -- Endereço para Bruno Dias (Admin)
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'shipping', '17500005', 'Avenida Sampaio Vidal', '455', 'Sala 3', 'Centro', 'Marília', 'SP', true),
    -- Endereços para Ana Silva
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'shipping', '01311000', 'Avenida Paulista', '1500', 'Apto 101', 'Bela Vista', 'São Paulo', 'SP', true),
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'billing', '01311000', 'Avenida Paulista', '1500', 'Apto 101', 'Bela Vista', 'São Paulo', 'SP', true),
    -- Endereço para Carlos Pereira
    ('b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', 'shipping', '22070010', 'Avenida Atlântica', '2000', NULL, 'Copacabana', 'Rio de Janeiro', 'RJ', true),
    -- Endereço para Mariana Costa
    ('c1c2c3c4-d1d2-e1e2-f1f2-a1a2a3a4a5a6', 'shipping', '30130005', 'Avenida Afonso Pena', '1500', 'Andar 4', 'Centro', 'Belo Horizonte', 'MG', true);

-- Inserir cartão salvo para teste
INSERT INTO saved_cards (saved_card_id, client_id, nickname, last_four_digits, brand, gateway_token, expiry_date, is_default)
VALUES
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'Cartão Principal', '1234', 'visa', 'tok_abc123xyz789', '2028-12-31', true);

-- =====================================================================
-- BLOCO 2: CATÁLOGO DE PRODUTOS
-- =====================================================================--
 Inserir categorias principais e subcategorias
INSERT INTO categories (category_id, name, slug, description, parent_category_id, is_active, sort_order)
VALUES
    ('c9f0595e-8e83-4a6b-85c3-8f1e7c5d6a3a', 'Smartphones', 'smartphones', 'Celulares de última geração', NULL, true, 10),
    ('a8b3c7d2-1e4f-4a9d-b6c8-9f0a1b2c3d4e', 'Tablets', 'tablets', 'Dispositivos portáteis para produtividade e entretenimento', NULL, true, 20),
    ('f5e6d7c8-b9a0-4b1c-8d2e-3f4a5b6c7d8e', 'Notebooks', 'notebooks', 'Computadores portáteis para trabalho e estudo', NULL, true, 30),
    ('d4e5f6a7-b8c9-4d0e-9f1a-2b3c4d5e6f7a', 'Acessórios', 'acessorios', 'Complementos para seus dispositivos', NULL, true, 40),
    ('b3c4d5e6-f7a8-49b0-c1d2-3e4f5a6b7c8d', 'Wearables', 'wearables', 'Dispositivos vestíveis e smartwatches', NULL, true, 50),
    ('e2f3a4b5-c6d7-48e9-f0a1-b2c3d4e5f6a7', 'Áudio', 'audio', 'Fones de ouvido e dispositivos de som', NULL, true, 60),
    ('acc1a1a1-b1b1-c1c1-d1d1-e1e1e1e1e1e1', 'Capas para Celular', 'capas-para-celular', 'Capas de proteção para smartphones', 'd4e5f6a7-b8c9-4d0e-9f1a-2b3c4d5e6f7a', true, 1),
    ('acc2b2b2-c2c2-d2d2-e2e2-f2f2f2f2f2f2', 'Carregadores', 'carregadores', 'Carregadores e cabos', 'd4e5f6a7-b8c9-4d0e-9f1a-2b3c4d5e6f7a', true, 2);

-- Inserir marcas
INSERT INTO brands (brand_id, name, slug, description, logo_url, is_active)
VALUES
    ('aa11bb22-cc33-44dd-ee55-ff66aa77bb88', 'Apple', 'apple', 'Tecnologia inovadora e design premium', 'https://exemplo.com/logos/apple.png', true),
    ('bb22cc33-dd44-55ee-ff66-aa77bb88cc99', 'Samsung', 'samsung', 'Inovação e qualidade para todos os dispositivos', 'https://exemplo.com/logos/samsung.png', true),
    ('cc33dd44-ee55-66ff-aa77-bb88cc99ddaa', 'Xiaomi', 'xiaomi', 'Tecnologia de alta qualidade a preços acessíveis', 'https://exemplo.com/logos/xiaomi.png', true),
    ('dd44ee55-ff66-77aa-bb88-cc99dd00ee11', 'Google', 'google', 'Produtos e serviços de tecnologia do Google', NULL, true),
    ('ee55ff66-aa77-88bb-cc99-dd00ee11ff22', 'Dell', 'dell', 'Computadores e notebooks de alta performance', NULL, true);

-- Inserir cores e tamanhos
INSERT INTO colors (color_id, name, hex_code, is_active) 
VALUES
    ('11111111-2222-3333-4444-555555555555', 'Preto', '#000000', true),
    ('22222222-3333-4444-5555-666666666666', 'Branco', '#FFFFFF', true),
    ('33333333-4444-5555-6666-777777777777', 'Azul', '#0000FF', true),
    ('c0101010-a1a1-b1b1-c1c1-d1d1d1d1d1d1', 'Vermelho', '#FF0000', true);

INSERT INTO sizes (size_id, name, size_code, sort_order, is_active)
VALUES
    ('8a5326b0-b539-426b-8b5e-26f531c37181', '128GB', '128', 10, true),
    ('f32c9c22-e4d6-4e58-96b2-6a7b7d036814', '256GB', '256', 20, true),
    ('1d8a2786-63e2-4b71-9252-cf82c875d9d7', '512GB', '512', 30, true),
    ('9e1b3d50-5d6e-4f7a-8b3c-2e1f4a6b8c9d', '1TB', '1024', 40, true);-- 
Inserir produtos
INSERT INTO products (product_id, base_sku, name, slug, description, category_id, brand_id, base_price, sale_price, sale_price_start_date, sale_price_end_date, stock_quantity, weight_kg, width_cm, height_cm, depth_cm, is_active)
VALUES
    -- Apple Products
    ('a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', 'AP-IPH13-128', 'iPhone 13', 'iphone-13', 'iPhone 13 com tela Super Retina XDR de 6.1 polegadas.', 'c9f0595e-8e83-4a6b-85c3-8f1e7c5d6a3a', 'aa11bb22-cc33-44dd-ee55-ff66aa77bb88', 4999.00, NULL, NULL, NULL, 100, 0.174, 7.15, 14.67, 0.77, true),
    ('b2c3d4e5-f6a7-4b6c-9d0e-1f2a3b4c5d6e', 'AP-IPADPRO', 'iPad Pro 11"', 'ipad-pro-11', 'iPad Pro com chip M1 e tela Liquid Retina.', 'a8b3c7d2-1e4f-4a9d-b6c8-9f0a1b2c3d4e', 'aa11bb22-cc33-44dd-ee55-ff66aa77bb88', 7999.00, NULL, NULL, NULL, 50, 0.466, 17.85, 24.76, 0.59, true),
    ('c3d4e5f6-a7b8-4c7d-8e9f-0a1b2c3d4e5f', 'AP-MACBOOKAIR', 'MacBook Air M2', 'macbook-air-m2', 'MacBook Air com chip M2 e tela Retina de 13.6".', 'f5e6d7c8-b9a0-4b1c-8d2e-3f4a5b6c7d8e', 'aa11bb22-cc33-44dd-ee55-ff66aa77bb88', 8999.00, NULL, NULL, NULL, 30, 1.24, 30.41, 21.5, 1.13, true),
    ('d4e5f6a7-b8c9-4d8e-9f0a-1b2c3d4e5f6a', 'AP-AIRPODSPRO', 'AirPods Pro', 'airpods-pro', 'Fones de ouvido com cancelamento de ruído ativo.', 'e2f3a4b5-c6d7-48e9-f0a1-b2c3d4e5f6a7', 'aa11bb22-cc33-44dd-ee55-ff66aa77bb88', 2199.00, NULL, NULL, NULL, 200, 0.054, 6.06, 4.52, 2.17, true),
    
    -- Samsung Products
    ('e5f6a7b8-c9d0-4e9f-8a1b-2c3d4e5f6a7b', 'SS-S23ULTRA', 'Galaxy S23 Ultra', 'galaxy-s23-ultra', 'Smartphone premium com câmera de 200MP.', 'c9f0595e-8e83-4a6b-85c3-8f1e7c5d6a3a', 'bb22cc33-dd44-55ee-ff66-aa77bb88cc99', 6999.00, NULL, NULL, NULL, 80, 0.234, 7.81, 16.34, 0.89, true),
    ('f6a7b8c9-d0e1-4f9a-8b2c-3d4e5f6a7b8c', 'SS-TABS8', 'Galaxy Tab S8', 'galaxy-tab-s8', 'Tablet com S Pen incluído e processador Snapdragon.', 'a8b3c7d2-1e4f-4a9d-b6c8-9f0a1b2c3d4e', 'bb22cc33-dd44-55ee-ff66-aa77bb88cc99', 5499.00, NULL, NULL, NULL, 40, 0.503, 16.53, 25.38, 0.63, true),
    ('a7b8c9d0-e1f2-4a9b-8c3d-4e5f6a7b8c9d', 'SS-BOOK2PRO', 'Galaxy Book2 Pro', 'galaxy-book2-pro', 'Notebook ultrafino com tela AMOLED.', 'f5e6d7c8-b9a0-4b1c-8d2e-3f4a5b6c7d8e', 'bb22cc33-dd44-55ee-ff66-aa77bb88cc99', 7999.00, NULL, NULL, NULL, 25, 1.11, 35.54, 22.58, 1.17, true),
    ('b8c9d0e1-f2a3-4b9c-8d4e-5f6a7b8c9d0e', 'SS-BUDS2PRO', 'Galaxy Buds2 Pro', 'galaxy-buds2-pro', 'Fones de ouvido com áudio de alta qualidade.', 'e2f3a4b5-c6d7-48e9-f0a1-b2c3d4e5f6a7', 'bb22cc33-dd44-55ee-ff66-aa77bb88cc99', 1299.00, NULL, NULL, NULL, 150, 0.0055, 1.99, 2.16, 1.87, true),
    
    -- Xiaomi Products
    ('c9d0e1f2-a3b4-4c9d-8e5f-6a7b8c9d0e1f', 'XM-13PRO', 'Xiaomi 13 Pro', 'xiaomi-13-pro', 'Smartphone com câmera Leica e Snapdragon 8 Gen 2.', 'c9f0595e-8e83-4a6b-85c3-8f1e7c5d6a3a', 'cc33dd44-ee55-66ff-aa77-bb88cc99ddaa', 5999.00, NULL, NULL, NULL, 120, 0.229, 7.46, 16.29, 0.84, true),
    ('d0e1f2a3-b4c5-4d9e-8f6a-7b8c9d0e1f2a', 'XM-PAD5', 'Xiaomi Pad 5', 'xiaomi-pad-5', 'Tablet com tela de 120Hz e alto-falantes quad.', 'a8b3c7d2-1e4f-4a9d-b6c8-9f0a1b2c3d4e', 'cc33dd44-ee55-66ff-aa77-bb88cc99ddaa', 2999.00, NULL, NULL, NULL, 60, 0.511, 16.63, 25.47, 0.69, true),
    
    -- Outros produtos
    ('11aa22bb-33cc-44dd-55ee-66ff77aa88bb', 'GO-PIXEL8', 'Google Pixel 8', 'google-pixel-8', 'O smartphone do Google com câmera inteligente.', 'c9f0595e-8e83-4a6b-85c3-8f1e7c5d6a3a', 'dd44ee55-ff66-77aa-bb88-cc99dd00ee11', 4500.00, NULL, NULL, NULL, 75, 0.187, 7.0, 15.0, 0.89, true),
    ('22bb33cc-44dd-55ee-66ff-77aa88bb99cc', 'DE-XPS15', 'Dell XPS 15', 'dell-xps-15', 'Notebook premium para criadores de conteúdo.', 'f5e6d7c8-b9a0-4b1c-8d2e-3f4a5b6c7d8e', 'ee55ff66-aa77-88bb-cc99-dd00ee11ff22', 12500.00, 11999.00, '2025-07-20 00:00:00', '2025-08-20 23:59:59', 15, 1.92, 34.4, 23.0, 1.8, true);-- In
serir imagens de produtos
INSERT INTO product_images (product_image_id, product_id, image_url, alt_text, is_cover, sort_order) 
VALUES
    ('d8c5f6e4-a3b2-4c1d-9e8f-7a6b5c4d3e2f', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', '/assets/products/iphone-13-front.jpg', 'iPhone 13 na cor preta, vista frontal', true, 0),
    ('e9d6f7e5-b4c3-4d2e-8f9a-8b7c6d5e4f3a', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', '/assets/products/iphone-13-back.jpg', 'iPhone 13 na cor preta, vista traseira', false, 1),
    ('b2c9c0b8-e7f6-4a5b-9c2d-1e0f9a8b7c6d', 'd4e5f6a7-b8c9-4d8e-9f0a-1b2c3d4e5f6a', '/assets/products/airpods-pro.jpg', 'AirPods Pro, vista frontal', true, 0),
    ('c3d0d1c9-f8a7-4b6c-8d3e-2f1a0b9c8d7e', 'e5f6a7b8-c9d0-4e9f-8a1b-2c3d4e5f6a7b', '/assets/products/galaxy-s23-ultra.jpg', 'Galaxy S23 Ultra, vista frontal', true, 0),
    ('e5f2f3e1-ba9c-4d8e-8f5a-4b3c2d1e0f9a', 'a7b8c9d0-e1f2-4a9b-8c3d-4e5f6a7b8c9d', '/assets/products/galaxy-book2-pro.jpg', 'Galaxy Book Pro 2, vista frontal', true, 0),
    ('d4e1e2d0-a9b8-4c7d-9e4f-3a2b1c0d9e8f', 'f6a7b8c9-d0e1-4f9a-8b2c-3d4e5f6a7b8c', '/assets/products/galaxy-tab-s8.jpg', 'Galaxy Tab S8, vista frontal', true, 0),
    ('f0e7a8f6-c5d4-4e3f-9a0b-9c8d7e6f5a4b', 'b2c3d4e5-f6a7-4b6c-9d0e-1f2a3b4c5d6e', '/assets/products/ipad-pro-11.jpg', 'iPad Pro 11, vista frontal', true, 0),
    ('f6a3a4f2-cb0d-4e9f-9a6b-5c4d3e2f1a0b', 'b8c9d0e1-f2a3-4b9c-8d4e-5f6a7b8c9d0e', '/assets/products/galaxy-buds2-pro.jpg', 'Galaxy Buds Pro 2, vista frontal', true, 0);

-- Inserir variações de produtos
INSERT INTO product_variants (product_variant_id, product_id, sku, color_id, size_id, stock_quantity, additional_price, is_active) 
VALUES
    -- Variações iPhone 13
    ('01a0b1c2-d3e4-4f5a-b6c7-d8e9f0a1b2c3', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', 'AP-IPH13-BLK-128', '11111111-2222-3333-4444-555555555555', '8a5326b0-b539-426b-8b5e-26f531c37181', 40, 0.00, true),
    ('12b1c2d3-e4f5-4a6b-c7d8-e9f0a1b2c3d4', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', 'AP-IPH13-WHT-128', '22222222-3333-4444-5555-666666666666', '8a5326b0-b539-426b-8b5e-26f531c37181', 35, 0.00, true),
    ('23c2d3e4-f5a6-4b7c-d8e9-f0a1b2c3d4e5', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', 'AP-IPH13-BLU-256', '33333333-4444-5555-6666-777777777777', 'f32c9c22-e4d6-4e58-96b2-6a7b7d036814', 25, 500.00, true),
    
    -- Variação Padrão iPad Pro
    ('56f5a6b7-c8d9-4e0f-a1b2-c3d4e5f6a7b8', 'b2c3d4e5-f6a7-4b6c-9d0e-1f2a3b4c5d6e', 'AP-IPADPRO-STD', NULL, NULL, 50, 0.00, true),
    
    -- Variação Padrão AirPods Pro
    ('78b7c8d9-e0f1-4a2b-c3d4-e5f6a7b8c9d0', 'd4e5f6a7-b8c9-4d8e-9f0a-1b2c3d4e5f6a', 'AP-AIRPODSPRO-STD', NULL, NULL, 200, 0.00, true),
    
    -- Variações Galaxy S23 Ultra
    ('34d3e4f5-a6b7-4c8d-e9f0-a1b2c3d4e5f6', 'e5f6a7b8-c9d0-4e9f-8a1b-2c3d4e5f6a7b', 'SS-S23ULTRA-BLK-256', '11111111-2222-3333-4444-555555555555', 'f32c9c22-e4d6-4e58-96b2-6a7b7d036814', 50, 0.00, true),
    ('45e4f5a6-b7c8-4d9e-f0a1-b2c3d4e5f6a7', 'e5f6a7b8-c9d0-4e9f-8a1b-2c3d4e5f6a7b', 'SS-S23ULTRA-WHT-512', '22222222-3333-4444-5555-666666666666', '1d8a2786-63e2-4b71-9252-cf82c875d9d7', 30, 800.00, true),
    
    -- Variações Xiaomi 13 Pro
    ('b2f1a2b3-c4d5-4e6f-a7b8-c9d0e1f2a3b4', 'c9d0e1f2-a3b4-4c9d-8e5f-6a7b8c9d0e1f', 'XM-13PRO-BLK-256', '11111111-2222-3333-4444-555555555555', 'f32c9c22-e4d6-4e58-96b2-6a7b7d036814', 70, 0.00, true),
    ('c3a2b3c4-d5e6-4f7a-b8c9-d0e1f2a3b4c5', 'c9d0e1f2-a3b4-4c9d-8e5f-6a7b8c9d0e1f', 'XM-13PRO-RED-256', 'c0101010-a1a1-b1b1-c1c1-d1d1d1d1d1d1', 'f32c9c22-e4d6-4e58-96b2-6a7b7d036814', 50, 150.00, true),
    
    -- Outras variações padrão
    ('a7e6f7a8-b9c0-4db2-e2f3-a4b5c6d7e8a9', '11aa22bb-33cc-44dd-55ee-66ff77aa88bb', 'GO-PIXEL8-STD', NULL, NULL, 75, 0.00, true),
    ('b8f7a8b9-c0d1-4ec3-f3a4-b5c6d7e8f9b0', '22bb33cc-44dd-55ee-66ff-77aa88bb99cc', 'DE-XPS15-STD', NULL, NULL, 15, 0.00, true);-- ===
==================================================================
-- BLOCO 3: CUPONS E PROMOÇÕES
-- =====================================================================

-- Inserir cupons de desconto
INSERT INTO coupons (coupon_id, code, description, discount_percentage, discount_amount, valid_from, valid_until, max_uses, min_purchase_amount, is_active, type, client_id)
VALUES
    ('c1d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 'BEMVINDO10', '10% de desconto na primeira compra', 10.00, NULL, '2025-01-01 00:00:00', '2025-12-31 23:59:59', 1000, 100.00, true, 'general', NULL),
    ('d2e2f2a2-b3c4-4d6e-9f0a-1b2c3d4e5f6a', 'CARLOSVIP', 'Cupom de R$50 para Carlos Pereira', NULL, 50.00, '2025-07-01 00:00:00', '2025-08-31 23:59:59', 1, 200.00, true, 'user_specific', 'b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6'),
    ('e3f3a3b3-c4d5-4e7f-8a1b-2c3d4e5f6a7b', 'EXPIRADO20', 'Cupom de teste expirado', 20.00, NULL, '2024-01-01 00:00:00', '2024-12-31 23:59:59', 100, NULL, true, 'general', NULL),
    ('f4a4b4c4-d5e6-4f8a-9b2c-3d4e5f6a7b8c', 'FRETE50', 'Frete grátis para compras acima de R$500', NULL, 50.00, '2025-07-01 00:00:00', '2025-12-31 23:59:59', 500, 500.00, true, 'general', NULL),
    ('a5b5c5d5-e6f7-4a9b-8c3d-4e5f6a7b8c9d', 'TECH15', '15% de desconto em produtos de tecnologia', 15.00, NULL, '2025-07-01 00:00:00', '2025-09-30 23:59:59', 200, 300.00, true, 'general', NULL);

-- =====================================================================
-- BLOCO 4: CARRINHOS E PEDIDOS DE TESTE
-- =====================================================================

-- Inserir carrinho ativo para teste
INSERT INTO carts (cart_id, client_id, expires_at) 
VALUES 
    ('c1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5d', 'ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', CURRENT_TIMESTAMP + INTERVAL '7 day');

-- Inserir itens no carrinho
INSERT INTO cart_items (cart_id, product_variant_id, quantity, unit_price)
VALUES
    ('c1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5d', '45e4f5a6-b7c8-4d9e-f0a1-b2c3d4e5f6a7', 1, 7799.00),
    ('c1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5d', '78b7c8d9-e0f1-4a2b-c3d4-e5f6a7b8c9d0', 1, 2199.00);

-- Inserir pedidos de exemplo
INSERT INTO orders (order_id, client_id, coupon_id, status, items_total_amount, discount_amount, shipping_amount)
VALUES
    ('01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 'ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'c1d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 'delivered', 7198.00, 719.80, 50.00),
    ('02e2f2a2-b3c4-4d6e-9f0a-1b2c3d4e5f6a', 'a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', NULL, 'shipped', 5999.00, 0.00, 35.50),
    ('03f3a3b3-c4d5-4e7f-8a1b-2c3d4e5f6a7b', 'b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', NULL, 'canceled', 1299.00, 0.00, 25.00),
    ('04a4b4c4-d5e6-4f8a-9b2c-3d4e5f6a7b8c', 'c1c2c3c4-d1d2-e1e2-f1f2-a1a2a3a4a5a6', NULL, 'pending', 2999.00, 0.00, 30.00);

-- Inserir itens dos pedidos
INSERT INTO order_items (order_id, product_variant_id, item_sku, item_name, quantity, unit_price)
VALUES
    -- Itens do Pedido 1
    ('01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', '01a0b1c2-d3e4-4f5a-b6c7-d8e9f0a1b2c3', 'AP-IPH13-128', 'iPhone 13 128GB Preto', 1, 4999.00),
    ('01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', '78b7c8d9-e0f1-4a2b-c3d4-e5f6a7b8c9d0', 'AP-AIRPODSPRO', 'AirPods Pro', 1, 2199.00),
    
    -- Itens do Pedido 2
    ('02e2f2a2-b3c4-4d6e-9f0a-1b2c3d4e5f6a', 'b2f1a2b3-c4d5-4e6f-a7b8-c9d0e1f2a3b4', 'XM-13PRO-BLK-256', 'Xiaomi 13 Pro 256GB Preto', 1, 5999.00),
    
    -- Itens do Pedido 3
    ('03f3a3b3-c4d5-4e7f-8a1b-2c3d4e5f6a7b', 'b8c9d0e1-f2a3-4b9c-8d4e-5f6a7b8c9d0e', 'SS-BUDS2PRO', 'Galaxy Buds2 Pro', 1, 1299.00),
    
    -- Itens do Pedido 4
    ('04a4b4c4-d5e6-4f8a-9b2c-3d4e5f6a7b8c', 'd0e1f2a3-b4c5-4d9e-8f6a-7b8c9d0e1f2a', 'XM-PAD5', 'Xiaomi Pad 5', 1, 2999.00);-- Ins
erir endereços dos pedidos (snapshot no momento da compra)
INSERT INTO order_addresses (order_id, address_type, recipient_name, postal_code, street, street_number, complement, neighborhood, city, state_code, phone)
VALUES
    -- Endereços do Pedido 1
    ('01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 'shipping', 'Bruno Dias', '17500005', 'Avenida Sampaio Vidal', '455', 'Sala 3', 'Centro', 'Marília', 'SP', '14991781010'),
    ('01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 'billing', 'Bruno Dias', '17500005', 'Avenida Sampaio Vidal', '455', 'Sala 3', 'Centro', 'Marília', 'SP', '14991781010'),
    
    -- Endereço do Pedido 2
    ('02e2f2a2-b3c4-4d6e-9f0a-1b2c3d4e5f6a', 'shipping', 'Ana Silva', '01311000', 'Avenida Paulista', '1500', 'Apto 101', 'Bela Vista', 'São Paulo', 'SP', '11987654321'),
    
    -- Endereço do Pedido 3
    ('03f3a3b3-c4d5-4e7f-8a1b-2c3d4e5f6a7b', 'shipping', 'Carlos Pereira', '22070010', 'Avenida Atlântica', '2000', NULL, 'Copacabana', 'Rio de Janeiro', 'RJ', '21912345678'),
    
    -- Endereço do Pedido 4
    ('04a4b4c4-d5e6-4f8a-9b2c-3d4e5f6a7b8c', 'shipping', 'Mariana Costa', '30130005', 'Avenida Afonso Pena', '1500', 'Andar 4', 'Centro', 'Belo Horizonte', 'MG', '31988887777');

-- =====================================================================
-- BLOCO 5: PAGAMENTOS
-- =====================================================================

-- Inserir pagamentos de exemplo
INSERT INTO payments (payment_id, order_id, method, status, amount, transaction_id, method_details, processed_at)
VALUES
    -- Pagamento Aprovado para Pedido 1 (Cartão de Crédito)
    ('fedcba98-7654-3210-fedc-ba9876543210', '01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 'credit_card', 'approved', 6528.20, 'txn_cc_abc123', '{"card_brand": "visa", "last4": "1234", "installments": 6}', CURRENT_TIMESTAMP),
    
    -- Pagamento Aprovado para Pedido 2 (PIX)
    ('abcdef01-2345-6789-abcd-ef0123456789', '02e2f2a2-b3c4-4d6e-9f0a-1b2c3d4e5f6a', 'pix', 'approved', 6034.50, 'txn_pix_def456', '{"qr_code_id": "pixqrid_789"}', CURRENT_TIMESTAMP),
    
    -- Pagamento Pendente para Pedido 4 (Boleto)
    ('12345678-90ab-cdef-1234-567890abcdef', '04a4b4c4-d5e6-4f8a-9b2c-3d4e5f6a7b8c', 'bank_slip', 'pending', 3029.00, 'txn_boleto_ghi789', '{"barcode": "12345678901234567890123456789012345678901234", "due_date": "2025-08-20"}', NULL);

-- =====================================================================
-- BLOCO 6: AVALIAÇÕES E REVIEWS
-- =====================================================================

-- Inserir avaliações de produtos
INSERT INTO reviews (review_id, client_id, product_id, order_id, rating, comment, is_approved)
VALUES
    -- Avaliação Aprovada do Bruno para o iPhone 13
    ('ab1cde2f-3a4b-5c6d-7e8f-9a0b1c2d3e4f', 'ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d', '01d1e1f1-a2b3-4c5d-8e9f-0a1b2c3d4e5f', 5, 'Produto excelente, entrega super rápida! Recomendo muito.', true),
    
    -- Avaliação Pendente da Ana para o Xiaomi 13 Pro
    ('bc2def3a-4b5c-6d7e-8f9a-0b1c2d3e4f5a', 'a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'c9d0e1f2-a3b4-4c9d-8e5f-6a7b8c9d0e1f', '02e2f2a2-b3c4-4d6e-9f0a-1b2c3d4e5f6a', 4, 'Ótimo custo-benefício, mas a bateria poderia durar mais. No geral, satisfeita com a compra.', false),
    
    -- Avaliação Aprovada do Carlos para Galaxy Buds2 Pro
    ('cd3efa4b-5c6d-7e8f-9a0b-1c2d3e4f5a6b', 'b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', 'b8c9d0e1-f2a3-4b9c-8d4e-5f6a7b8c9d0e', '03f3a3b3-c4d5-4e7f-8a1b-2c3d4e5f6a7b', 3, 'Som bom, mas o cancelamento de ruído não é tão eficiente quanto esperava.', true);

-- =====================================================================
-- BLOCO 7: CONSENTIMENTOS LGPD
-- =====================================================================

-- Inserir consentimentos LGPD para os clientes
INSERT INTO consents (client_id, type, terms_version, is_granted)
VALUES
    -- Consentimentos para Bruno (Admin)
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'terms_of_service', 'v1.0', true),
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'privacy_policy', 'v1.0', true),
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'marketing_email', 'v1.0', true),
    ('ff1d107c-4a5e-44b0-9885-cdeb2c1a1566', 'newsletter_subscription', 'v1.0', true),
    
    -- Consentimentos para Ana Silva
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'terms_of_service', 'v1.0', true),
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'privacy_policy', 'v1.0', true),
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'marketing_email', 'v1.0', true),
    ('a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6', 'cookies_essential', 'v1.0', true),
    
    -- Consentimentos para Carlos Pereira
    ('b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', 'terms_of_service', 'v1.0', true),
    ('b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', 'privacy_policy', 'v1.0', true),
    ('b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6', 'marketing_email', 'v1.0', false),
    
    -- Consentimentos para Mariana Costa
    ('c1c2c3c4-d1d2-e1e2-f1f2-a1a2a3a4a5a6', 'terms_of_service', 'v1.0', true),
    ('c1c2c3c4-d1d2-e1e2-f1f2-a1a2a3a4a5a6', 'privacy_policy', 'v1.0', true),
    ('c1c2c3c4-d1d2-e1e2-f1f2-a1a2a3a4a5a6', 'newsletter_subscription', 'v1.0', true);

-- =====================================================================
-- ATUALIZAÇÃO DE DADOS
-- =====================================================================

-- Atualizar cliente com email verificado
UPDATE clients
SET 
    email_verified_at = NOW(),
    updated_at = NOW(),
    version = version + 1
WHERE 
    client_id = 'a1a2a3a4-b1b2-c1c2-d1d2-e1e2e3e4e5e6';

-- =====================================================================
-- FIM DO SCRIPT DE SEED
-- =====================================================================
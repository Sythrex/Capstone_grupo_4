CREATE TABLE region (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL
);

CREATE TABLE comuna (
    id INT IDENTITY(1,1) PRIMARY KEY,
    region_id INT NOT NULL,
    nombre VARCHAR(50) NOT NULL,
    CONSTRAINT fk_region FOREIGN KEY (region_id) REFERENCES region (id)
);

CREATE TABLE tipo_vehiculo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL
);

CREATE TABLE vehiculo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    patente VARCHAR(10) NOT NULL,
    vin VARCHAR(50) NOT NULL,
    anio INT NOT NULL,
    kilometraje INT,
    color VARCHAR(20),
    tipo_id INT,
    CONSTRAINT fk_tipo FOREIGN KEY (tipo_id) REFERENCES tipo_vehiculo (id)
);

CREATE TABLE cliente (
    id INT IDENTITY(1,1) PRIMARY KEY,
    rut VARCHAR(10) NOT NULL,
    nombre VARCHAR(50) NOT NULL,
    correo VARCHAR(50),
    telefono VARCHAR(20),
    direccion VARCHAR(100),
    comuna_id INT,
    CONSTRAINT fk_comuna FOREIGN KEY (comuna_id) REFERENCES comuna (id)
);

CREATE TABLE cliente_vehiculo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    cliente_id INT NOT NULL,
    vehiculo_id INT NOT NULL,
    principal BIT,
    fecha_desde DATE NOT NULL,
    fecha_hasta DATE,
    created_at DATE NOT NULL,
    CONSTRAINT fk_cliente FOREIGN KEY (cliente_id) REFERENCES cliente (id),
    CONSTRAINT fk_vehiculo FOREIGN KEY (vehiculo_id) REFERENCES vehiculo (id)
);

CREATE TABLE categoria (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200)
);

CREATE TABLE repuesto (
    id INT IDENTITY(1,1) PRIMARY KEY,
    sku VARCHAR(50) NOT NULL,
    nombre VARCHAR(50) NOT NULL,
    usado BIT NOT NULL,
    marca VARCHAR(50),
    categoria_id INT,
    CONSTRAINT fk_categoria FOREIGN KEY (categoria_id) REFERENCES categoria (id)
);

CREATE TABLE taller (
    id INT IDENTITY(1,1) PRIMARY KEY,
    razon_social VARCHAR(100) NOT NULL,
    rut_taller VARCHAR(15) NOT NULL,
    direccion VARCHAR(100) NOT NULL,
    comuna_id INT NOT NULL,
    CONSTRAINT fk_comuna FOREIGN KEY (comuna_id) REFERENCES comuna (id)
);

CREATE TABLE tipo_funcionario (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL
);

CREATE TABLE funcionario (
    id INT IDENTITY(1,1) PRIMARY KEY,
    rut VARCHAR(10) NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    especialidad VARCHAR(50),
    activo BIT NOT NULL,
    tipo_id INT,
    CONSTRAINT fk_tipo_funcionario FOREIGN KEY (tipo_id) REFERENCES tipo_funcionario (id)
);

CREATE TABLE asignacion_talleres (
    id INT IDENTITY(1,1) PRIMARY KEY,
    funcionario_id INT NOT NULL,
    taller_id INT NOT NULL,
    fecha_inicio DATE NOT NULL,
    fecha_termino DATE NOT NULL,
    created_at DATE,
    CONSTRAINT fk_taller FOREIGN KEY (taller_id) REFERENCES taller (id),
    CONSTRAINT fk_funcionario FOREIGN KEY (funcionario_id) REFERENCES funcionario (id)
);

CREATE TABLE repuesto_unidades (
    id INT IDENTITY(1,1) PRIMARY KEY,
    repuesto_id INT NOT NULL,
    taller_id INT NOT NULL,
    stock_disponible INT NOT NULL DEFAULT 0,
    stock_reservado INT DEFAULT 0,
    precio_unitario INT,
    CONSTRAINT fk_taller FOREIGN KEY (taller_id) REFERENCES taller (id),
    CONSTRAINT fk_repuesto FOREIGN KEY (repuesto_id) REFERENCES repuesto (id)
);

CREATE TABLE log_inventario (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    fecha_log DATETIME NOT NULL DEFAULT GETDATE(),
    repuesto_unidades_id INT NOT NULL,
    variacion_stock INT NOT NULL,
    nota VARCHAR(100),
    CONSTRAINT fk_repuesto_stock FOREIGN KEY (repuesto_unidades_id) REFERENCES repuesto_unidades (id)
);

CREATE TABLE tipo_servicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    costo_base INT
);

CREATE TABLE cotizacion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    cliente_id INT NOT NULL,
    funcionario_cotizacion_id INT NOT NULL,
    fecha_cotizacion DATE NOT NULL DEFAULT GETDATE(),
    fecha_vencimiento DATE NOT NULL,
    estado VARCHAR(10) NOT NULL DEFAULT 'Pendiente',
    monto_total INT,
    CONSTRAINT fk_cliente FOREIGN KEY (cliente_id) REFERENCES cliente (id),
    CONSTRAINT fk_funcionario FOREIGN KEY (funcionario_cotizacion_id) REFERENCES funcionario (id)
);

CREATE TABLE atencion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    fecha_ingreso DATETIME NOT NULL DEFAULT GETDATE(),
    kilometraje_ingreso INT NOT NULL,
    observaciones VARCHAR(1000),
    estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
    taller_id INT NOT NULL,
    vehiculo_id INT,
    mecanico_id INT,
    administrativo_id INT NOT NULL,
    cliente_id INT,
    cotizacion_id INT,
    CONSTRAINT fk_taller FOREIGN KEY (taller_id) REFERENCES taller (id),
    CONSTRAINT fk_vehiculo FOREIGN KEY (vehiculo_id) REFERENCES vehiculo (id),
    CONSTRAINT fk_mecanico FOREIGN KEY (mecanico_id) REFERENCES funcionario (id),
    CONSTRAINT fk_administrativo FOREIGN KEY (administrativo_id) REFERENCES funcionario (id),
    CONSTRAINT fk_cliente FOREIGN KEY (cliente_id) REFERENCES cliente (id),
    CONSTRAINT fk_cotizacion FOREIGN KEY (cotizacion_id) REFERENCES cotizacion (id)
);

CREATE TABLE servicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    atencion_id INT NOT NULL,
    tipo_servicio_id INT NOT NULL,
    repuesto_unidades_id INT,
    sub_total INT,
    descripcion VARCHAR(500),
    CONSTRAINT fk_atencion FOREIGN KEY (atencion_id) REFERENCES atencion (id),
    CONSTRAINT fk_tipo_servicio FOREIGN KEY (tipo_servicio_id) REFERENCES tipo_servicio (id),
    CONSTRAINT fk_repuesto_unidades FOREIGN KEY (repuesto_unidades_id) REFERENCES repuesto_unidades (id)
);

CREATE TABLE factura (
    id INT IDENTITY(1,1) PRIMARY KEY,
    folio INT NOT NULL,
    fecha_emision DATETIME NOT NULL DEFAULT GETDATE(),
    monto_neto INT NOT NULL,
    iva INT NOT NULL,
    atencion_id INT,
    sii_params JSON,
    sii_id INT,
    CONSTRAINT fk_atencion FOREIGN KEY (atencion_id) REFERENCES atencion (id),
    CONSTRAINT uk_folio UNIQUE (folio)
);

CREATE TABLE bitacora (
    id INT IDENTITY(1,1) PRIMARY KEY,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    descripcion VARCHAR(1000),
    atencion_id INT NOT NULL,
    estado VARCHAR(20),
    tipo VARCHAR(20),
    CONSTRAINT fk_atencion FOREIGN KEY (atencion_id) REFERENCES atencion (id)
);
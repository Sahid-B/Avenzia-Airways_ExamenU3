-- 1. Secuencias para claves primarias autoincrementales
CREATE SEQUENCE IF NOT EXISTS bookings.orders_order_id_seq;
CREATE SEQUENCE IF NOT EXISTS bookings.order_details_order_detail_id_seq;
CREATE SEQUENCE IF NOT EXISTS bookings.payments_payment_id_seq;
CREATE SEQUENCE IF NOT EXISTS bookings.flight_change_requests_request_id_seq;
CREATE SEQUENCE IF NOT EXISTS bookings.flight_change_history_change_id_seq;
CREATE SEQUENCE IF NOT EXISTS bookings.transaction_history_transaction_id_seq;

-- 2. Tabla de Órdenes
CREATE TABLE IF NOT EXISTS bookings.orders (
    order_id integer NOT NULL DEFAULT nextval('bookings.orders_order_id_seq'::regclass),
    book_ref character(6) NOT NULL,
    order_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    total_amount numeric(10,2) NOT NULL,
    status character varying(50) DEFAULT 'Pending'::character varying,
    CONSTRAINT orders_pkey PRIMARY KEY (order_id),
    CONSTRAINT orders_book_ref_fkey FOREIGN KEY (book_ref)
        REFERENCES bookings.bookings (book_ref) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION
);

-- 3. Tabla de Detalles de Orden
CREATE TABLE IF NOT EXISTS bookings.order_details (
    order_detail_id integer NOT NULL DEFAULT nextval('bookings.order_details_order_detail_id_seq'::regclass),
    order_id integer NOT NULL,
    description character varying(200) NOT NULL,
    amount numeric(10,2) NOT NULL,
    CONSTRAINT order_details_pkey PRIMARY KEY (order_detail_id),
    CONSTRAINT order_details_order_id_fkey FOREIGN KEY (order_id)
        REFERENCES bookings.orders (order_id) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION
);

-- 4. Tabla de Pagos
CREATE TABLE IF NOT EXISTS bookings.payments (
    payment_id integer NOT NULL DEFAULT nextval('bookings.payments_payment_id_seq'::regclass),
    order_id integer NOT NULL,
    external_transaction_id character varying(100) NULL,
    amount numeric(10,2) NOT NULL,
    currency character varying(10) DEFAULT 'USD'::character varying,
    gateway character varying(50) NULL,
    status character varying(50) DEFAULT 'Completed'::character varying,
    response_message text NULL,
    payment_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    confirmation_date timestamp without time zone NULL,
    user_id character varying(450) NULL,
    CONSTRAINT payments_pkey PRIMARY KEY (payment_id),
    CONSTRAINT payments_external_transaction_id_key UNIQUE (external_transaction_id),
    CONSTRAINT payments_order_id_fkey FOREIGN KEY (order_id)
        REFERENCES bookings.orders (order_id) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION
);

-- 5. Tabla de Solicitudes de Cambios de Vuelo (Reprogramaciones)
CREATE TABLE IF NOT EXISTS bookings.flight_change_requests (
    request_id integer NOT NULL DEFAULT nextval('bookings.flight_change_requests_request_id_seq'::regclass),
    book_ref character(6) NOT NULL,
    requested_flight_id integer NOT NULL,
    status character varying(50) DEFAULT 'Pending'::character varying,
    request_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT flight_change_requests_pkey PRIMARY KEY (request_id),
    CONSTRAINT flight_change_requests_book_ref_fkey FOREIGN KEY (book_ref)
        REFERENCES bookings.bookings (book_ref) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT flight_change_requests_requested_flight_id_fkey FOREIGN KEY (requested_flight_id)
        REFERENCES bookings.flights (flight_id) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION
);

-- 6. Tabla de Historial de Cambios Ejecutados
CREATE TABLE IF NOT EXISTS bookings.flight_change_history (
    change_id integer NOT NULL DEFAULT nextval('bookings.flight_change_history_change_id_seq'::regclass),
    book_ref character(6) NOT NULL,
    old_flight_id integer NOT NULL,
    new_flight_id integer NOT NULL,
    reason character varying(255) NULL,
    change_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT flight_change_history_pkey PRIMARY KEY (change_id),
    CONSTRAINT flight_change_history_book_ref_fkey FOREIGN KEY (book_ref)
        REFERENCES bookings.bookings (book_ref) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT flight_change_history_new_flight_id_fkey FOREIGN KEY (new_flight_id)
        REFERENCES bookings.flights (flight_id) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION,
    CONSTRAINT flight_change_history_old_flight_id_fkey FOREIGN KEY (old_flight_id)
        REFERENCES bookings.flights (flight_id) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION
);

-- 7. Tabla de Historial de Transacciones de Usuario
CREATE TABLE IF NOT EXISTS bookings.transaction_history (
    transaction_id integer NOT NULL DEFAULT nextval('bookings.transaction_history_transaction_id_seq'::regclass),
    book_ref character(6) NOT NULL,
    transaction_type character varying(100) NOT NULL,
    details text NULL,
    transaction_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    user_id character varying(450) NULL,
    CONSTRAINT transaction_history_pkey PRIMARY KEY (transaction_id),
    CONSTRAINT transaction_history_book_ref_fkey FOREIGN KEY (book_ref)
        REFERENCES bookings.bookings (book_ref) MATCH SIMPLE
        ON UPDATE NO ACTION ON DELETE NO ACTION
);

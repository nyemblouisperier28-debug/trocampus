-- =====================================================================
-- trocampus : schéma PostgreSQL (version 15 ou plus récente)
-- Exécution : psql -U trocampus -d trocampus -f schema.sql
-- Les tables marquées « déduit » viennent du NOM d'un écran Figma non
-- encore lu : leurs colonnes sont à confirmer.
-- =====================================================================

CREATE EXTENSION IF NOT EXISTS citext;   -- e-mails insensibles à la casse

-- ---------------------------------------------------------------------
-- Fonctions utilitaires
-- ---------------------------------------------------------------------

-- Met à jour updated_at à chaque modification d'une ligne
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Distance en mètres entre deux points GPS (formule de haversine)
CREATE OR REPLACE FUNCTION distance_m(lat1 double precision, lon1 double precision,
                                      lat2 double precision, lon2 double precision)
RETURNS double precision LANGUAGE sql IMMUTABLE AS $$
    SELECT 2 * 6371000 * asin(sqrt(
        power(sin(radians(lat2 - lat1) / 2), 2) +
        cos(radians(lat1)) * cos(radians(lat2)) * power(sin(radians(lon2 - lon1) / 2), 2)))
$$;

-- ---------------------------------------------------------------------
-- 1. Campus et zones          (écrans : Annonces, Recherche, Accueil)
-- ---------------------------------------------------------------------

CREATE TABLE campuses (
    id    bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  text NOT NULL UNIQUE,                 -- « Campus Lyon »
    city  text NOT NULL
);

CREATE TABLE zones (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    campus_id  bigint NOT NULL REFERENCES campuses(id),
    name       text NOT NULL,                   -- « La Doua », « Charpennes »
    latitude   double precision NOT NULL,
    longitude  double precision NOT NULL,
    UNIQUE (campus_id, name)
);

-- ---------------------------------------------------------------------
-- 2. Comptes                  (écrans : Connexion, Inscription, Mot de passe)
-- ---------------------------------------------------------------------

CREATE TABLE users (
    id                 bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    first_name         text NOT NULL,           -- seul champ demandé à l'inscription
    last_name          text,                    -- affiché dans l'historique (« Camille Robert »)
    email              citext NOT NULL UNIQUE,
    password_hash      text NOT NULL,           -- jamais le mot de passe en clair
    role               text NOT NULL DEFAULT 'student'
                       CHECK (role IN ('student', 'admin')),
    status             text NOT NULL DEFAULT 'active'
                       CHECK (status IN ('pending', 'active', 'suspended', 'deleted')),
    campus_id          bigint REFERENCES campuses(id),
    zone_id            bigint REFERENCES zones(id),
    email_verified_at  timestamptz,
    terms_accepted_at  timestamptz NOT NULL,    -- « conditions d'utilisation » acceptées
    created_at         timestamptz NOT NULL DEFAULT now(),
    last_login_at      timestamptz,
    CHECK (char_length(first_name) BETWEEN 1 AND 60)
);

-- Lien « Mot de passe oublié ? » : on ne stocke que l'empreinte du jeton
CREATE TABLE password_reset_tokens (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     bigint NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash  text NOT NULL UNIQUE,
    expires_at  timestamptz NOT NULL,
    used_at     timestamptz,
    created_at  timestamptz NOT NULL DEFAULT now()
);

-- Écran Paramètres (déduit du nom de l'écran)
CREATE TABLE user_settings (
    user_id            bigint PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    search_radius_m    integer NOT NULL DEFAULT 2000 CHECK (search_radius_m > 0),
    notify_messages    boolean NOT NULL DEFAULT true,
    notify_offers      boolean NOT NULL DEFAULT true,
    notify_exchanges   boolean NOT NULL DEFAULT true
);

-- ---------------------------------------------------------------------
-- 3. Annonces                 (écrans : Annonces, Recherche, Publier une annonce)
-- ---------------------------------------------------------------------

CREATE TABLE categories (
    id    bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  text NOT NULL UNIQUE                  -- « Manuels », « Meubles », « Appareils »
);

CREATE TABLE listings (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    owner_id      bigint NOT NULL REFERENCES users(id),
    category_id   bigint NOT NULL REFERENCES categories(id),
    zone_id       bigint NOT NULL REFERENCES zones(id),
    title         text NOT NULL CHECK (char_length(title) BETWEEN 3 AND 120),
    description   text,
    condition     text CHECK (condition IN ('neuf', 'bon_etat', 'usage')),
    listing_type  text NOT NULL CHECK (listing_type IN ('sale', 'exchange')),
    price_cents   integer CHECK (price_cents >= 0),     -- « 12 € » = 1200
    price_points  integer CHECK (price_points >= 0),    -- « 60 points »
    wanted_text   text,                                 -- « cherche des romans »
    status        text NOT NULL DEFAULT 'active'
                  CHECK (status IN ('draft', 'active', 'reserved', 'completed',
                                    'archived', 'removed')),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    -- une vente doit avoir un prix en euros et/ou en points
    CHECK (listing_type <> 'sale' OR price_cents IS NOT NULL OR price_points IS NOT NULL),
    -- recherche plein texte en français (titre + description)
    search_vector tsvector GENERATED ALWAYS AS (
        to_tsvector('french', coalesce(title, '') || ' ' || coalesce(description, ''))
    ) STORED
);

CREATE INDEX idx_listings_search   ON listings USING gin (search_vector);
CREATE INDEX idx_listings_active   ON listings (status, created_at DESC);
CREATE INDEX idx_listings_zone     ON listings (zone_id);
CREATE INDEX idx_listings_category ON listings (category_id);
CREATE INDEX idx_listings_owner    ON listings (owner_id);

CREATE TRIGGER trg_listings_updated BEFORE UPDATE ON listings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE listing_photos (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    listing_id  bigint NOT NULL REFERENCES listings(id) ON DELETE CASCADE,
    url         text NOT NULL,                  -- fichier stocké hors base, on garde le lien
    position    smallint NOT NULL DEFAULT 0,
    UNIQUE (listing_id, position)
);

-- ---------------------------------------------------------------------
-- 4. Offres et rendez-vous    (écrans : Offres d'échange, Détail de la
--    proposition, Offre acceptée / refusée, Échanges et rendez-vous, Historique)
-- ---------------------------------------------------------------------

CREATE TABLE offers (
    id                  bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    listing_id          bigint NOT NULL REFERENCES listings(id),   -- objet visé
    proposer_id         bigint NOT NULL REFERENCES users(id),
    offered_listing_id  bigint REFERENCES listings(id),            -- objet proposé en retour
    offered_points      integer NOT NULL DEFAULT 0 CHECK (offered_points >= 0),
    offered_cents       integer NOT NULL DEFAULT 0 CHECK (offered_cents >= 0),
    message             text,
    status              text NOT NULL DEFAULT 'pending'
                        CHECK (status IN ('pending', 'accepted', 'refused',
                                          'withdrawn', 'expired')),
    refusal_reason      text,
    created_at          timestamptz NOT NULL DEFAULT now(),
    responded_at        timestamptz,
    CHECK (offered_listing_id IS NOT NULL OR offered_points > 0 OR offered_cents > 0),
    CHECK (offered_listing_id IS NULL OR offered_listing_id <> listing_id)
);

-- Un étudiant ne peut avoir qu'une offre en attente par annonce
CREATE UNIQUE INDEX uq_offers_pending ON offers (listing_id, proposer_id)
    WHERE status = 'pending';
CREATE INDEX idx_offers_proposer ON offers (proposer_id, status);
CREATE INDEX idx_offers_listing  ON offers (listing_id, status);

CREATE TABLE appointments (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    offer_id      bigint NOT NULL UNIQUE REFERENCES offers(id),
    place         text NOT NULL,                -- « Devant l'entrée de la BU La Doua »
    scheduled_at  timestamptz NOT NULL,
    status        text NOT NULL DEFAULT 'planned'
                  CHECK (status IN ('planned', 'confirmed', 'done', 'cancelled')),
    proposed_by   bigint NOT NULL REFERENCES users(id),
    created_at    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_appointments_date ON appointments (scheduled_at);

-- Résultat d'un rendez-vous : alimente l'écran Historique des échanges
CREATE TABLE exchange_outcomes (
    id                        bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    appointment_id            bigint NOT NULL UNIQUE REFERENCES appointments(id),
    outcome                   text NOT NULL CHECK (outcome IN ('success', 'failed')),
    failure_note              text,             -- « Rencontre sans échange d'objets. »
    confirmed_by_owner_at     timestamptz,
    confirmed_by_proposer_at  timestamptz,
    completed_at              timestamptz NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------
-- 5. Messagerie               (écran : Messagerie)
-- ---------------------------------------------------------------------

CREATE TABLE conversations (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    listing_id  bigint NOT NULL REFERENCES listings(id),
    offer_id    bigint UNIQUE REFERENCES offers(id),
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE conversation_participants (
    conversation_id  bigint NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    user_id          bigint NOT NULL REFERENCES users(id),
    last_read_at     timestamptz,               -- sert aux « non lus » et au « lu »
    PRIMARY KEY (conversation_id, user_id)
);

CREATE TABLE messages (
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    conversation_id  bigint NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    sender_id        bigint NOT NULL REFERENCES users(id),
    body             text NOT NULL CHECK (char_length(body) BETWEEN 1 AND 2000),
    sent_at          timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_messages_conv ON messages (conversation_id, sent_at);

-- ---------------------------------------------------------------------
-- 6. Invitations et points    (écrans : Points et transactions, Accueil)
-- ---------------------------------------------------------------------

CREATE TABLE invitations (
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    inviter_id       bigint NOT NULL REFERENCES users(id),
    invitee_email    citext NOT NULL,
    invitee_user_id  bigint REFERENCES users(id),
    status           text NOT NULL DEFAULT 'pending'
                     CHECK (status IN ('pending', 'registered', 'confirmed', 'expired')),
    created_at       timestamptz NOT NULL DEFAULT now(),
    confirmed_at     timestamptz,
    UNIQUE (inviter_id, invitee_email)
);

CREATE TABLE points_transactions (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id        bigint NOT NULL REFERENCES users(id),
    amount         integer NOT NULL CHECK (amount <> 0),   -- +20, -60
    kind           text NOT NULL
                   CHECK (kind IN ('invitation', 'exchange', 'purchase', 'adjustment')),
    label          text NOT NULL,                          -- « Invitation · Inès »
    status         text NOT NULL DEFAULT 'confirmed'
                   CHECK (status IN ('pending', 'confirmed', 'completed', 'cancelled')),
    offer_id       bigint REFERENCES offers(id),
    invitation_id  bigint REFERENCES invitations(id),
    created_by     bigint REFERENCES users(id),            -- administrateur (ajustement)
    created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_points_user ON points_transactions (user_id, created_at DESC);

-- Solde, gains et dépenses : seuls les mouvements « confirmed » et « completed » comptent
CREATE VIEW v_user_points AS
SELECT u.id AS user_id,
       COALESCE(SUM(t.amount)
           FILTER (WHERE t.status IN ('confirmed', 'completed')), 0) AS balance,
       COALESCE(SUM(t.amount)
           FILTER (WHERE t.amount > 0 AND t.status IN ('confirmed', 'completed')), 0) AS earned,
       COALESCE(-SUM(t.amount)
           FILTER (WHERE t.amount < 0 AND t.status IN ('confirmed', 'completed')), 0) AS spent
FROM users u
LEFT JOIN points_transactions t ON t.user_id = u.id
GROUP BY u.id;

-- Messages non lus par conversation et par participant
CREATE VIEW v_unread_messages AS
SELECT cp.user_id, cp.conversation_id, COUNT(m.id) AS unread
FROM conversation_participants cp
LEFT JOIN messages m
       ON m.conversation_id = cp.conversation_id
      AND m.sender_id <> cp.user_id
      AND (cp.last_read_at IS NULL OR m.sent_at > cp.last_read_at)
GROUP BY cp.user_id, cp.conversation_id;

-- Dépense de points sans jamais passer sous zéro (verrou sur l'utilisateur)
CREATE OR REPLACE FUNCTION spend_points(p_user bigint, p_amount integer,
                                        p_label text, p_offer bigint DEFAULT NULL)
RETURNS bigint LANGUAGE plpgsql AS $$
DECLARE
    v_balance integer;
    v_id      bigint;
BEGIN
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Montant invalide';
    END IF;
    PERFORM 1 FROM users WHERE id = p_user FOR UPDATE;   -- évite deux dépenses simultanées
    SELECT balance INTO v_balance FROM v_user_points WHERE user_id = p_user;
    IF v_balance < p_amount THEN
        RAISE EXCEPTION 'Solde insuffisant';
    END IF;
    INSERT INTO points_transactions (user_id, amount, kind, label, status, offer_id)
    VALUES (p_user, -p_amount, 'purchase', p_label, 'completed', p_offer)
    RETURNING id INTO v_id;
    RETURN v_id;
END;
$$;

-- ---------------------------------------------------------------------
-- 7. Modération                (écrans administrateur : déduit des noms)
-- ---------------------------------------------------------------------

CREATE TABLE reviews (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    offer_id     bigint NOT NULL REFERENCES offers(id),
    reviewer_id  bigint NOT NULL REFERENCES users(id),
    reviewee_id  bigint NOT NULL REFERENCES users(id),
    rating       smallint NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment      text,
    status       text NOT NULL DEFAULT 'visible' CHECK (status IN ('visible', 'hidden')),
    created_at   timestamptz NOT NULL DEFAULT now(),
    UNIQUE (offer_id, reviewer_id),
    CHECK (reviewer_id <> reviewee_id)
);

CREATE TABLE reports (
    id                 bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reporter_id        bigint NOT NULL REFERENCES users(id),
    target_user_id     bigint REFERENCES users(id),
    target_listing_id  bigint REFERENCES listings(id),
    reason             text NOT NULL,
    details            text,
    status             text NOT NULL DEFAULT 'open'
                       CHECK (status IN ('open', 'investigating', 'resolved', 'dismissed')),
    assigned_admin_id  bigint REFERENCES users(id),
    created_at         timestamptz NOT NULL DEFAULT now(),
    resolved_at        timestamptz,
    CHECK (target_user_id IS NOT NULL OR target_listing_id IS NOT NULL)
);
CREATE INDEX idx_reports_status ON reports (status, created_at DESC);

CREATE TABLE report_notes (               -- « Dossier d'enquête »
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    report_id   bigint NOT NULL REFERENCES reports(id) ON DELETE CASCADE,
    admin_id    bigint NOT NULL REFERENCES users(id),
    note        text NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------
-- 8. Administration            (écrans administrateur : déduit des noms)
-- ---------------------------------------------------------------------

CREATE TABLE audit_logs (                 -- « Journal d'administration »
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_id     bigint NOT NULL REFERENCES users(id),
    action       text NOT NULL,             -- « suspend_user », « hide_review »...
    target_type  text,
    target_id    bigint,
    details      jsonb,
    created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_audit_date ON audit_logs (created_at DESC);

CREATE TABLE app_settings (               -- « Paramètres administrateur »
    key          text PRIMARY KEY,
    value        text NOT NULL,
    description  text,
    updated_by   bigint REFERENCES users(id),
    updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE daily_stats (                -- « Trafic et statistiques »
    stat_date          date PRIMARY KEY,
    new_users          integer NOT NULL DEFAULT 0,
    active_users       integer NOT NULL DEFAULT 0,
    new_listings       integer NOT NULL DEFAULT 0,
    offers_sent        integer NOT NULL DEFAULT 0,
    exchanges_success  integer NOT NULL DEFAULT 0,
    exchanges_failed   integer NOT NULL DEFAULT 0
);

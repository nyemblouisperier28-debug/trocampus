-- =====================================================================
-- trocampus : données de référence (à exécuter après schema.sql)
-- Aucun utilisateur n'est créé ici : les mots de passe doivent être
-- hachés par l'API (inscription normale), puis un compte devient
-- administrateur avec la commande indiquée à la fin.
-- =====================================================================

INSERT INTO campuses (name, city) VALUES ('Campus Lyon', 'Lyon');

INSERT INTO zones (campus_id, name, latitude, longitude)
SELECT c.id, z.name, z.lat, z.lon
FROM campuses c,
     (VALUES ('La Doua',     45.7833, 4.8750),
             ('Charpennes',  45.7700, 4.8630)) AS z(name, lat, lon)
WHERE c.name = 'Campus Lyon';

INSERT INTO categories (name) VALUES ('Manuels'), ('Meubles'), ('Appareils'), ('Autres');

-- Récompenses d'invitation et rayon de recherche par défaut
INSERT INTO app_settings (key, value, description) VALUES
    ('points_per_invitation', '10',   'Points crédités pour une invitation confirmée'),
    ('default_radius_m',      '2000', 'Rayon de recherche par défaut, en mètres'),
    ('offer_expiry_days',     '7',    'Durée avant expiration d''une offre en attente');

-- Après avoir créé votre compte via l'application, le passer administrateur :
-- UPDATE users SET role = 'admin' WHERE email = 'vous@exemple.fr';

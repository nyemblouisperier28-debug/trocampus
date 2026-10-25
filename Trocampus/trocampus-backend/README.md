# Trocampus backend

Backend de developpement compose de l'API ASP.NET Core, PostgreSQL et Redis. Les ecrans MAUI gardent leur mise en page, avec les formulaires de connexion et d'inscription relies a l'API.

## Démarrage sur Windows

Consulter [le guide de démarrage Windows](../docs/demarrage_windows.md) pour lancer Docker Desktop, l'API, PostgreSQL, Redis et l'application MAUI sur la cible **Local Machine**. Les ports actuellement configurés sont `8081` pour l'API, `5433` pour PostgreSQL et `6380` pour Redis.

PostgreSQL initialise le schéma et les données de référence lors de la première création de son volume. Après une modification de `db/schema.sql`, une migration versionnée est nécessaire ; ne supprimez pas le volume de données partagé pour appliquer un changement de schéma.

## Lancer l'API hors Docker

Demarrer d'abord PostgreSQL et Redis avec Compose, puis lancer `dotnet run --project Trocampus.Api` depuis ce dossier. Les connexions locales sont configurees dans `Trocampus.Api/appsettings.Development.json`. Ces identifiants sont reserves a une base locale et ne doivent pas etre reutilises en production.

## Routes disponibles

- `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/logout`
- `GET /api/listings?q=&categoryId=&zoneId=&type=&take=&skip=`
- `POST /api/listings` (Bearer requis), `GET /api/categories`
- `GET /api/home/summary` (Bearer requis)
- `GET /health/live`, `GET /health/ready`

Les jetons bearer expirent apres 30 jours. Seule leur empreinte est enregistree dans PostgreSQL ; la deconnexion revoque la session. Les annonces publiques sont mises en cache 45 secondes dans Redis et les categories une heure. Les ecritures financieres et les messages ne sont pas mis en cache.

L'inscription demande un prenom, une adresse e-mail et un mot de passe d'au moins 10 caracteres. Un doublon d'adresse est refuse. La connexion ne cree pas de compte et renvoie une erreur si les identifiants ne correspondent pas a un compte actif. Apres authentification, le jeton est conserve dans le stockage securise MAUI ; l'accueil affiche le prenom et le profil charge les donnees du compte depuis `GET /api/auth/me`.

## Connexion de l'application MAUI

Le client MAUI utilise `http://localhost:8081` sur Windows et `http://10.0.2.2:8081` sur l'émulateur Android standard. Sur un téléphone physique, `10.0.2.2` ne fonctionne pas : remplacer l'adresse par l'IP locale du PC, autoriser le port 8081 dans le pare-feu du réseau privé et utiliser HTTPS hors développement local.

## Limites de cette premiere version

Les flux d'offres, rendez-vous, conversations/messages, reinitialisation du mot de passe, televersement de photos, e-mails et endpoints d'administration restent a implementer. Utiliser des migrations SQL versionnees pour les prochaines evolutions du schema au lieu de modifier un volume deja initialise.

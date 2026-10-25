# Lancer Trocampus sur Windows

Sur Windows, l'application MAUI tourne directement sur le PC avec la cible **Local Machine**. Il n'est pas nécessaire de créer un émulateur Windows. Docker Desktop sert à lancer l'API, PostgreSQL et Redis.

## 1. Démarrer les services backend

1. Ouvrir Docker Desktop et attendre que le moteur indique qu'il fonctionne.
2. Ouvrir un terminal PowerShell dans VS Code.
3. Se placer dans le dossier backend et démarrer les composants :

```powershell
cd .\trocampus-backend
docker compose up -d --build
docker compose ps
```

Le fichier `.env` local fournit les ports et les mots de passe. Il est ignoré par Git. Ne mets pas de vrais mots de passe dans `.env.example` et ne supprime pas `.env` si tu veux garder les mêmes paramètres.

Dans la configuration actuelle, les ports publiés sur le PC sont :

| Composant | Adresse locale |
| --- | --- |
| Application API | `http://localhost:8081` |
| Documentation OpenAPI | `http://localhost:8081/openapi/v1.json` |
| Vérification API, PostgreSQL et Redis | `http://localhost:8081/health/ready` |
| PostgreSQL | `localhost:5433` |
| Redis | `localhost:6380` |

La réponse attendue de `/health/ready` contient `"status":"ready"`, `"database":"ok"` et `"cache":"ok"`. PostgreSQL et Redis utilisent des volumes Docker : `docker compose down` arrête les services sans effacer les données. **Ne lance pas `docker compose down -v`**, car l'option `-v` supprime les volumes et les données de la base.

Pour consulter les journaux ou arrêter les services :

```powershell
docker compose logs -f api
docker compose logs -f db
docker compose logs -f redis
docker compose down
```

Laisse les services tourner pendant que tu utilises l'application.

## 2. Lancer l'application MAUI

1. Ouvrir le dossier racine `Trocampus` dans VS Code.
2. Avec `Ctrl+Shift+P`, exécuter **.NET MAUI: Select Startup Project** et choisir `Trocampus.csproj`.
3. Choisir la cible **Local Machine** / **Windows Machine** dans les cibles de débogage .NET MAUI.
4. Lancer le projet avec la commande de débogage MAUI ou le bouton de lancement MAUI.

Le projet cible `net10.0-windows10.0.19041.0`. L'application Windows appelle l'API à `http://localhost:8081`.

Dans `.vscode/launch.json`, le profil **Launch Integrated Browser against localhost** est une configuration de navigateur, pas le lancement MAUI. Ne choisis pas ce profil pour démarrer l'application Windows : il ouvre une page web au lieu de lancer Trocampus.

## 3. Vérifier le fonctionnement

Avant d'essayer l'inscription ou la connexion, vérifie que l'API répond :

```powershell
Invoke-RestMethod http://localhost:8081/health/ready
```

Dans l'application, crée un compte avec une adresse e-mail valide et un mot de passe d'au moins 10 caractères. Une adresse déjà utilisée doit être connectée plutôt que réinscrite. Les comptes neufs commencent avec zéro point, zéro offre, zéro message non lu et aucun échange dans leur historique.

## 4. Arrêter et relancer

Pour arrêter l'application, ferme sa fenêtre ou arrête la session de débogage MAUI. Pour arrêter les composants backend, depuis `trocampus-backend` :

```powershell
docker compose down
```

Au prochain démarrage, relance `docker compose up -d`. Les données PostgreSQL sont conservées dans le volume Docker.

## Notes de connexion

- **Application Windows sur le même PC** : l'adresse API est `http://localhost:8081`.
- **Émulateur Android standard** : l'adresse API est `http://10.0.2.2:8081`.
- **Téléphone Android physique** : utilise l'adresse IP locale du PC et configure le pare-feu. Pour tout usage hors réseau de développement, configure HTTPS.
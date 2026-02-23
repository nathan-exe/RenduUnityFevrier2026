## Arbres procéduraux 

<img width="753" height="623" alt="image" src="https://github.com/user-attachments/assets/d39f45a5-175b-4a20-836b-ffd802d49596" />

J'ai implémenté un POC du <a href="https://docs.google.com/document/d/1pZPgNVconWkuDLs-SOfueWEL2C0AaA0Uoo-jqUDj5-E/edit?usp=sharing">sujet que j'avais recherché pour le concours du master de l'enjmin</a>.

voici la liste des features implémentées : 
- génération procédurale de la structure d'arbres en 3D (graphs) avec des l systems
  - réécriture récursive de chaines de symboles + turtle pour générer un graph en 3D  
  - un alphabet de symboles qui permettent de gérer la rotation, la longueur ainsi que l'épaisseur des branches
  - interpolation possible entre différents niveaux de pousse de l'abre
  - réduction du nombre de segments lorsqu'ils se suivent sans se diviser en d'autres branches
  - variables aléatoires, avec possibilité de changer la seed de l'arbre
- dessin des arbres en wireframe avec les gizmos
- dessin des arbres en 3D avec du raymarching dans le fragment shader de leur bounding box
  - raymarching sur une liste de SDFs de cylindres représentant chaque branche de l'arbre
  - smoothing des intersections entre les branches
  - phong shading
  - depth write pour intégrer correctement les objets à la scène
  - shadow pass (avec niveau de détail réduit)
- dessin de feuilles au bout des branches avec des meshes texturés


Ce que j'aimerais ajouter :
- binary space partitionning ou octree pour améliorer drastiquement les performances du fragment shader. (actuellent, le jeu lag quand un arbre a trop de branches et prend beaucoup de place à l'écran)
- texture mapping sur le bois
- système de pousse de l'arbre plus polished
- bounding mesh plus serré au lieu d'une bounding box pour réduire le coût du fragment shader
- animation du vent dans les branches
- experimentation avec d'autres espèces / structures d'arbres
  
## Multijoueur en ligne et chat écrit : 

Ce projet contient deux systèmes différents : 
- Un personnage dont les déplacements sont répliqués sur les différents clients grâce à NetcodeForGameobjects et à un component NetworkTransform.
  J'ai codé un petit script _NetworkComponentSort_ qui permet de n'activer certains components d'un objet que si le client est l'owner dudit objet.
  Par exemple, la physique du joueur, les inputs, la camera et l'audio listener ne sont pas actifs sur tous les personnages du jeu mais que sur celui que le client contrôle.
- Un chat écrit, codé avec Unity Transport. Il y'a un script _LowLevelNetworkManager_ qui gère les connections entre le server et les différents clients sur un second port
  en se basant sur les events du NetworkManager principal de NetcodeForGameObjects. Ce script permet également de broadcast des events avec de la data arbitraire,
  et est utilisé par le script _GameChat_ pour partager les messages textuels des joueurs. Enfin, un script _ChatMessageBox_ permet d'afficher les messages dans l'UI de la scène Unity.
  La serialization en binaire et la deserialization des paquets est gérée dans le script _GameChat_.

  Voici la structure d'un packet de message générique du LowLevelNetworkManager :
  - Message Type -> 1 byte, enum **Unknown** ou **ChatMessage**
  - Data Length -> 4 bytes Uint
  - Data -> byte[]

  Voici la structure d'un packet de message du chat :
  - _Message Type -> 1 byte, enum **Unknown** ou **ChatMessage** (ici **ChatMessage**)_
  - _Data Length -> 4 bytes Uint_
  - _Data -> byte[] :_
    - MessageType -> 1 byte, enum **System** ou **Player**
    - MessageLength -> 4 bytes Uint
    - PlayerNameLength -> 4 bytes Uint
    - Message -> byte[], string convertie en array de bytes
    - PlayerName -> byte[], string convertie en array de bytes

## Pour tester : 

Il y'a une scène "scene_characterController".
Il faut la lancer sur deux instances de jeu différentes (utiliser la fenetre multiplayer playmode de Unity) puis cliquer sur le bouton Host sur la première instance puis Client sur la seconde instance. On peut constater que les mouvements sont biens répliqués en se déplaçant avec **ZQSD**, et on peut ouvrir le chat avec **T** et envoyer des messages avec **Entrer**. **Echap** ou **Clic souris** pour sortir du chat.

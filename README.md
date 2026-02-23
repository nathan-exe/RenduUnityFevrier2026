## Explication du Projet : 

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

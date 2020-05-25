# Szakdoga

Külön elérhető ARFoundation, illetve AREngine-t használó frontend alkalmazás. Mindkettő .apk formátumban fent van a repoban. Utóbbi csak Huawei eszközön működik. 
Mindkét esetben külön backend áll rendelkezésre. Ezen felül elérhető egy vizualizációs alkalmazás, amely egyelőre csak Unity Editorban működik (vagy Oculus Quest szemüvegre kell lebuildelni). 2018+ Unity ajánlott hozzá, illetve az Oculus Integration Kit letöltése Asset Store-ból.

A prjoekt működése:
- Megfelelő backend indítása
- Az ehhez tartozó AR alkalamzás indítása
  - Koordináta-tengelyek felvétele
  - Környezet felvétele
  - Kommunikáció létrehozása beállítások menüben (backend-del azonos IP, 1883-as port) -> MQTT
  - Mentés
- Drón indtítása: docker környezetben vagy valóságban
- Drónhoz tartozó backend indítása:
```sh
$ cd
$ export ROS_MASTER_URI=http://172.18.0.100:11311
$ export ROS_IP={saját hálózat}
$ python drone_control.py
```
- VR alkalmazás indítása
  - Csatlakozás MQTT hálózatra
  - A felvett pontfelhő visszatöltése, megfelelő aggregációval
- Vizualizáció indítása, kiadható utasítások:
   - W, S: fel, le
   - A, D: forgás Z-tengely körül balra, vagy jobbra
   - felfelé, lefelé nyíl: előre, hátra mozgás
   - jobbra, balra nyíl: jobbra, balra mozgás
   

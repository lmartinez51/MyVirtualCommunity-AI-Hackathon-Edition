Carpeta oficial de anuncios y videos para la pantalla LED del Arco de Avenida Benito Juárez.

Cómo publicar un nuevo video (patrocinador, evento, campaña cultural):
1. Copia el archivo `.mp4` (recomendado H.264, 1280x720 o 1920x1080, 10–45 segundos) directamente aquí.
   El backend lo detecta automáticamente en `/api/world/arch-playlist` y lo sirve en `/api/media/arch/<archivo>`.
   Los clientes Unity lo pre-descargan en segundo plano sin recompilar ni reiniciar el juego.
2. Opcionalmente puedes crear un `playlist.json` en esta misma carpeta para definir título, patrocinador, orden o fechas de vigencia (`startAt` / `endAt`):
   {
     "items": [
       { "fileName": "camila_loop.mp4", "title": "Camila en Ciudad Juárez", "sponsor": "Cartelera", "order": 1 },
       { "fileName": "patrocinador_40s.mp4", "title": "Oferta", "sponsor": "Comercio Local", "order": 2, "startAt": "2026-11-01T00:00:00Z", "endAt": "2026-12-01T00:00:00Z" }
     ]
   }

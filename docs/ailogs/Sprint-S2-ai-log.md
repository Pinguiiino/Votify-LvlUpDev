# AI Usage Log — Sprint S2

## 1) Herramientas usadas

- Herramienta/modelo: Claude (Anthropic) 
- Para qué se usó:
  - Diseñar el flujo de subida de imagen (frontend Blazor + endpoint API + almacenamiento estático). 
  
## 2) Prompts clave (copiar/pegar)

- **Prompt 1:** “Quiero que al crear un Evento o Proyecto se pueda subir una imagen de portada y que aparezca como miniatura en la página de eventos. Propón primero el flujo (dónde se guarda, qué endpoint hace falta, qué cambia en la entidad) y después genera el endpoint de subida con tamaño máximo de 5 MB, y los formularios con selector de archivo, vista previa y botón para quitarla. Misma lógica para Evento y Proyecto."  

## 3) Salidas relevantes (resumen corto)

**Prompt 1:** Propuso separar la subida en dos pasos: un endpoint upload-image que guarda el archivo físicamente y devuelve la URL, y luego el POST normal de creación que solo guarda esa URL en ImageUrl. Sugirió almacenar los archivos en wwwroot/uploads/events y wwwroot/uploads/projects, validar extensión y tamaño, y generar el componente Blazor con InputFile, vista previa y botón "Quitar".  

## 4) Qué aceptamos y qué rechazamos (mínimo 3 ejemplos)

- Aceptado:
  - Separar el upload del POST de creación: mantiene los DTOs como JSON simples y permite mostrar la vista previa antes de enviar el formulario. 
  - Guardar con Guid.NewGuid() como nombre de archivo: evita colisiones y problemas con nombres con caracteres raros. 

- Rechazado/corregido:
  - Guardar la imagen como Base64 en la base de datos: infla la BD y ralentiza las consultas. Guardamos solo la ruta y servimos el archivo como estático. 
  - Validar el tipo por ContentType: el navegador puede falsificarlo. Lo cambiamos a validar por extensión del archivo. 

## 5) Cómo lo verificamos

- Experimento / medición: probamos manualmente subir una imagen al crear un evento y un proyecto, comprobando que aparece la vista previa, se guarda y se muestra la miniatura en MainEvent y MyProjects. 
- Casos límite: archivo > 5 MB (rechazado en cliente), archivo.txt (rechazado por el endpoint), botón "Quitar" (limpia ImageUrl), evento sin imagen (muestra el icono por defecto). 

## 6) Resultado final / decisión humana

El equipo aceptó la solución con los ajustes anteriores. Quedó reflejado en el repo en: 

src/Votify.Api/Controllers/EventsController.cs y ProjectController.cs -> endpoints upload-image. 

src/Votify.Web/Components/Pages/CreateEvent.razor y CreateProject.razor -> formulario con InputFile, vista previa y botón "Quitar". 

MainEvent.razor y MyProjects.razor -> muestran la miniatura correctamente. 
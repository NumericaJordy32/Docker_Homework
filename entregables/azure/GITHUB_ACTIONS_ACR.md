# Publicación sin Docker local ni ACR Tasks

El flujo `.github/workflows/publish-acr.yml` construye y publica las cuatro imágenes desde un runner hospedado por GitHub. No usa `az acr build`, por lo que evita el bloqueo de ACR Tasks y la red local.

Antes de ejecutarlo, cree en el repositorio de GitHub estos secretos:

- `ACR_LOGIN_SERVER`: `acrclinicajordy202609.azurecr.io`.
- `AZURE_CREDENTIALS`: JSON de una identidad con permiso `AcrPush` en el registro. No lo agregue al repositorio ni lo muestre en logs.

Luego haga push a `main` o ejecute **Actions > Publish images to ACR > Run workflow**. Se publicarán:

- `clinica/oauth-jwt-api:latest`
- `clinica/pacientes-api:latest`
- `clinica/historial-clinico-api:latest`
- `clinica/api-gateway:latest`

Cada publicación también conserva una etiqueta corta del commit para desplegar una versión inmutable.

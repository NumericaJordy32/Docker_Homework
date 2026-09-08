# Memoria de comandos de Azure

Registro de pasos ejecutados, sin secretos.

1. Se verificó la suscripción con `az account show` desde Azure Cloud Shell.
2. Se mantuvieron el Resource Group `rg-clinica-aa-jordy-202609`, el ACR `acrclinicajordy202609.azurecr.io`, las dos bases Azure SQL y las cinco Container Apps ya creadas.
3. Se configuraron referencias a secretos de Container Apps para cadenas SQL, RabbitMQ, usuarios OAuth y la misma `Jwt__Key` en OAuthJWT, Pacientes e Historial. No se registran valores en este archivo.
4. Se configuró RabbitMQ con FQDN interno y puerto TCP 5672 en Pacientes e Historial.
5. Se cambió el mínimo de réplicas a 1 en OAuthJWT, Pacientes, Historial y Gateway para eliminar el escalado a cero y el arranque en frío.
6. Se actualizó el Gateway para usar los FQDN HTTPS públicos de Pacientes, Historial y OAuthJWT como destinos.
7. Validación pública: `/health` del Gateway devolvió 200; `/pacientes` y `/historiales` devolvieron 401 sin token; `/oauth/token` llegó a OAuth y devolvió 400 al enviar un cuerpo vacío.
8. Se intentó `az acr build` para publicar las imágenes con reintentos RabbitMQ, pero Azure devolvió `TasksOperationsNotAllowed` para esta suscripción/ACR. No se publicó ninguna imagen ni se cambió una imagen desplegada.
9. Tras la revisión: `az group delete --name <NOMBRE_RESOURCE_GROUP> --yes --no-wait`.
